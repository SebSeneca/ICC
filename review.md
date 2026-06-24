# Review-rapport: BasketSystem (IMPACT Code Challenge) — Helhedsreview

Gennemgående, ærligt review af hele kodebasen efter aflevering. Formålet er refleksion: lever løsningen op til produktionskvalitet for behovet (en webshop-kurv-API), er strukturen ren, er kravene opfyldt, og hvad burde være gjort anderledes.

Reviewet er udført af fire dedikerede perspektiver — **arkitektur**, **.NET-kodekvalitet**, **test** og **product owner (kravopfyldelse)** — og konsolideret her. Review only: ingen kodeændringer er foretaget.

> De to punkter fra det tidligere fokuserede review er allerede adresseret og indgår derfor ikke som åbne findings: kurv-trådsikkerhed (`Lock` i `Basket.cs`) og fjernelse af den ubrugte `doc/GetAllProducts.json`.

## Summary
En lagdelt .NET 10 Basket API oven på Code Challenge API'et, bygget i 9 user stories med 67 tests (64 passed, 3 opt-in integrationstests skipped). Alle funktionelle krav er opfyldt, og løsningen er klart over gennemsnittet for en kodeudfordring: ren Clean Architecture, gennemtænkt concurrency og robust upstream-integration. De reelle svagheder er få, isolerede og ikke-blokerende — men der er nogle subtile korrekthedsdetaljer i tal-/trådhåndteringen og lidt løs konfiguration, der trækker den et stykke fra "helt færdig til produktion".

---

## 1. Arkitektur & struktur

### Styrker
- **Ren afhængighedsretning (Clean/Onion).** Projektreferencer håndhæver det: Domain er afhængighedsfrit, Application → kun Domain, Infrastructure → Application+Domain, Api → alle. Application definerer porte (`ICodeChallengeApiClient`, `IProductCatalog`, `IBasketRepository`), Infrastructure implementerer. Lærebogseksempel, gjort rigtigt.
- **De to klassiske DI-livstidsfælder er bevidst undgået.** `CachedProductCatalog` er singleton (`InfrastructureServiceCollectionExtensions.cs:42`), men bruger `IServiceScopeFactory` og åbner et scope pr. fetch (`CachedProductCatalog.cs:43-45`) frem for at fange den scoped/transient typed `HttpClient` i en singleton. `TokenProvider` (singleton, `:25`) bruger `IHttpClientFactory`, ikke en captured client. Det er ikke trivielt, og det er korrekt.
- **Interface-segregation.** Adskillelsen af `IProductCatalog` (fuld cache + refresh) og `IBuyableProductCatalog` (kun `FindBuyableAsync`) giver `BasketService`/`OrderService` netop den smalle evne de skal bruge — pænt ISP-snit.
- **Domæne-invarianter ligger i domænet.** `BasketItem` håndhæver `quantity >= 1` i ctor og mutatorer (`BasketItem.cs:7-8,31-32,39-40`); `Basket` indkapsler sin `Dictionary` og udstiller kun adfærd. Ingen anæmisk model.
- **Centraliseret, lag-ren fejlstrategi.** Application kaster semantiske exceptions uden HTTP-kendskab; `GlobalExceptionHandler.cs:28-39` mapper til RFC 9457 ProblemDetails. Controllers er tynde og kender ingen statuskoder.

### Forbedringspunkter
- **`ProductCacheTtl` er dead config.** Deklareret (`CodeChallengeApiOptions.cs:11`), dokumenteret i README, sat i `appsettings.json` — men aldrig læst i kode. Caching er reelt *refresh-interval-drevet* (`CatalogWarmupHostedService.cs:16`), ikke TTL-drevet. Cachen udløber aldrig af sig selv; den overskrives bare periodisk. Enten implementér en faktisk TTL eller fjern feltet — som det står, lover dokumentationen en stale-while-revalidate-TTL der ikke findes. *(Bekræftet på tværs af arkitektur- og kodekvalitets-review.)*
- **Warm-up er ikke så "eager" som dokumenteret.** Warm-up sker i `BackgroundService.ExecuteAsync` (`CatalogWarmupHostedService.cs:14-22`), der kører *efter* appen begynder at tage trafik. En request der rammer før den ~30s upstream-fetch er færdig, udløser selv `LoadAsync` og betaler ventetiden. README's "first request does not pay that cost" (README:26-27) er derfor kun sandt hvis første request kommer efter warm-up. En `StartAsync`-baseret blokering eller en readiness-gate ville matche påstanden.
- **`OrderId` modelleres nullable hele vejen, men antages non-null udadtil.** `Order.OrderId` er `string?` (`Order.cs:3`), og `OrderService.Map` oversætter `null → string.Empty` (`OrderService.cs:39`). En upstream-respons uden `orderId` giver dermed lydløst en `OrderDto` med tomt id + HTTP 200 — klienten tror ordren lykkedes med id `""`. Hvis upstream skal garantere et id, bør manglende id være en upstream-fejl (502), ikke en tom streng.

### Overvejelser
- **Ingen resilience-politik ud over 401-retry.** Transiente 5xx/netværksfejl retries ikke (bevidst out-of-scope). Mod en kendt-langsom/-ustabil upstream ville `Microsoft.Extensions.Http.Resilience` være det naturlige næste skridt mod produktionsmodenhed.
- **`Order`-domæne-record bærer et upstream-genereret `OrderId`** (sættes via `with` i `CodeChallengeApiClient.cs:36`). Fungerer, men blander "domæne-ordre" og "upstream-kvittering" i én type; en separat resultat-/kvitteringstype ville være renere.
- Upstream-timeout (2 min, `InfrastructureServiceCollectionExtensions.cs:38`) mappes til 502 — semantisk er 504 Gateway Timeout mere præcist. Kosmetisk.

**Arkitektur-dom:** Solid, korrekt lagdelt løsning der rammer Clean Architecture og SOLID præcist, og som håndterer de svære DI-livstidsfælder bevidst. Produktionsmoden nok for skalaen; svaghederne er små og isolerede. Ingen blocking.

---

## 2. Kodekvalitet (.NET / C#)

### Styrker
- Konsekvent, idiomatisk moderne C# (primary constructors, records, collection expressions, fil-scoped namespaces, `is { }`-mønstre). Nullable aktiveret i alle projekter og respekteret.
- God cancellation-disciplin: `CancellationToken` tages og videregives næsten hele vejen til HTTP-kaldene.
- Korrekt request-cloning i 401-retry (`AuthenticationDelegatingHandler.cs:33-49`) — den almindelige fælde (forbrugt request-body kan ikke gensendes) er undgået.
- `BackgroundService` er robust: skelner korrekt mellem cancellation (break) og fejl (log + fortsæt), vælter ikke hosten (`CatalogWarmupHostedService.cs:25-32`).
- NuGet pinnet til konkrete versioner i alle projekter. Stort set ingen kommentarer — i tråd med konventionen, og koden er selvbeskrivende.

### Forbedringspunkter (potentielle bugs markeret)
- **⚠️ Præcisionstab: `double → decimal`-cast på pris (`CodeChallengeApiClient.cs:40`).** Upstream leverer `Price` som `double` (`:42`), castet direkte til `decimal`. I et pris-/betalingsdomæne er det en reel korrekthedsrisiko: en `double` kan ikke repræsentere fx `19.99` eksakt, og artefakter kan propagere til `Total` og videre til ordren upstream. Overvej `Math.Round((decimal)p.Price, 2)` ved indlæsning, så al efterfølgende `decimal`-aritmetik er ren. Castet underminerer ellers den (rigtige) beslutning om at bruge `decimal` i domænet.
- **⚠️ Kulturafhængig `Size.ToString()` mod eksternt API (`CodeChallengeApiClient.cs:29`).** `Size` er `int`, så harmløst i praksis nu, men det er en kulturafhængig serialisering på en upstream-grænseflade. Brug `ToString(CultureInfo.InvariantCulture)`.
- **⚠️ Trådsikkerheden i `Basket` dækker ikke `BasketItem`'s mutable state.** `Basket`'s `Lock` (`Basket.cs:6`) er korrekt anvendt på collection-niveau, og `Items` returnerer et snapshot via `.ToList()` (`:15`). Men snapshotet kopierer *referencer*, og `BasketItem` er mutérbar (`Quantity` med `private set`, `BasketItem.cs:25-43`). Itererer én tråd over `basket.Items` mens en anden kalder `AddItem`/`SetItemQuantity` på samme `productId`, muteres et item den første tråd holder uden for låsen — `LineTotal` (`:27`) kan læses midt i en `Quantity`-opdatering. Realistiske triggere: `BasketService.Map` (`:60-65`) og især `OrderService.SubmitAsync` (`:23-30`), der `await`'er per item i et langt vindue. Sandsynligheden er lav (samme basket-id samtidigt), men låsningen er ikke fuldt dækkende, sådan som commit-beskeden antyder. Reneste fix: gør `BasketItem` immutabel og erstat hele item'et i dictionariet under låsen.
- **`OrderService.SubmitAsync` læser `basket.Items` to gange** (`OrderService.cs:19` og `:23`) — to separate låste snapshots, hvor basket'en kan ændres imellem. Læs `var items = basket.Items;` én gang og brug det (både konsistens og mikro-effektivitet).
- **Buyability re-rangerer hele kataloget pr. opslag.** `BuyableProductCatalog.FindBuyableAsync` (`:11-13`) kører `OrderByDescending` over 10.000 produkter ved *hver* add og *hver* submit-linje (i loop, `OrderService.cs:25`). En kurv med N linjer = N fulde sorteringer. Funktionelt korrekt; et cached top-100-`HashSet<int>` (invalideret ved refresh) ville være den naturlige løsning. Bevidst acceptabelt under "clean code over performance", men det er det sted skaleringen knækker.

### Mindre / stilistisk
- `AuthenticationDelegatingHandler` kloner altid requesten upfront (`:11`), også når der ikke kommer 401; lazy klon (kun ved 401) ville være renere. Desuden videregives `cancellationToken` ikke til `ReadAsByteArrayAsync` (`:42`).
- Intet `ConfigureAwait(false)` i kodebasen. Korrekt og ufarligt i ASP.NET Core 10 (ingen `SynchronizationContext`), men for genbrugelig handler-/provider-infrastruktur er det stadig best practice.
- `CodeChallengeApiClient.Timeout = 2 min` er hardcoded i DI (`InfrastructureServiceCollectionExtensions.cs:38`); kunne flyttes til options for konsistens.
- Navngivning er gennemført og intentions-afslørende (`GetBasketOrThrow`, `FindBuyableAsync`). Ingen indvendinger.

**Kodekvalitets-dom:** Stærk, professionel, idiomatisk .NET 10. De svære concurrency-stykker (single-flight, token-double-checked-locking, 401-retry med cloning) er grundlæggende korrekte. De vægtigste reservationer er de to subtile tal-korrekthedsrisici og at `Basket`-trådsikkerheden ikke fuldt dækker `BasketItem`. Ingen showstoppere ved opgavens skala; disse tre er de rigtige steder at sætte ind før man kalder tråd-/talhåndteringen "færdig".

---

## 3. Test & dækning

**Testresultat (kørt):** `dotnet test BasketSystem.slnx --nologo` → **64 passed, 3 skipped, 0 failed** (~1s, netværksfri). De 3 skippede er integrationstestene, korrekt gated af `RUN_INTEGRATION_TESTS`.

### Styrker
- Ægte assertions overalt — ingen "kører-bare"-tests. Der asserteres på værdier, rækkefølger og bivirkninger (`Received(1)`).
- God fejlsti-dækning på service- OG API-niveau: 400/404/422 testet både som exceptions og som HTTP-status via `WebApplicationFactory` (`BasketItemEndpointsTests.cs:28,56,69,83`, `OrderEndpointsTests.cs:31,41`).
- De svære ting er reelt testet: source-of-truth ved submit med bevidst "stale" data (`OrderServiceTests.cs:15`), cache single-flight med 10 samtidige kolde requests (`CachedProductCatalogTests.cs:31`), 401-retry med frisk token (`AuthenticationDelegatingHandlerTests.cs:25`).

### Dækningshuller (sorteret efter alvor)
1. **Upstream-fejl → 502 er ikke testet.** `GlobalExceptionHandler.cs:34` mapper `HttpRequestException`/`TimeoutException` til 502, men ingen test rammer stien gennem API-laget. Kerneadfærd (resiliens mod upstream-nedbrud) udækket. Billigt at lukke med en fake-klient der kaster.
2. **`CodeChallengeApiClient` har ingen unit-tests.** Den eneste klasse der laver faktisk HTTP-serialisering dækkes kun af de *skippede* integrationstests. Utestet: `double→decimal`-mapping, `Name ?? ""`, og CreateOrder-payloadens form (især `Size.ToString()` → `ProductSize` som string). `StubHttpMessageHandler` findes allerede og kunne verificere request-body + `OrderId`-parsing.
3. **Concurrency på `Basket` er ikke testet — selvom trådsikkerhed netop blev tilføjet.** Seneste commit indførte `Lock`, men ingen test udøver samtidige mutationer. Adfærden er udokumenteret af tests; en fjernelse af låsen ville ikke fejle noget. (Hænger sammen med kodekvalitets-finding om `BasketItem`.)
4. **Pagineringsgrænser kun delvist dækket.** `pageSize == MaxPageSize` (1000, accepteret grænse) testes ikke — kun 1001 (afvist), så off-by-one på `>` vs `>=` ville ikke fanges. Sidste/delvise side og side-ud-over-grænsen (tom) testes heller ikke.
5. **Ranking/buyability-kant ikke udfordret.** Tie-break testes (`ProductRankingTests.cs:9`), men ikke kanten "produkt nr. 100 vs. 101" — altså at en vare lige under stregen *ikke* er buyable. `BuyableProductCatalog` afhænger kritisk af et stabilt snit.
6. **Token single-flight (samtidige kolde `GetTokenAsync`) ikke testet** — modsat catalog, hvor den findes.
7. Tom-kurv-submit tjekker 400 (`OrderServiceTests.cs:51`), men ikke at upstream *ikke* rammes (`DidNotReceive()`).

### Forslag
- Luk hul 1–3 før produktion (502-sti, `CodeChallengeApiClient`-serialisering, `Basket`-concurrency).
- Integrationstesten `Full_flow_creates_an_order_via_real_upstream` (`CodeChallengeApiIntegrationTests.cs:46`) opretter en **rigtig ordre** upstream ved hver kørsel. Forsvarligt, da den er opt-in og default-skippet — men dokumentér bivirkningen på testen, så ingen pr. vane sætter `RUN_INTEGRATION_TESTS=1` i en delt pipeline.

**Test-dom:** Solid og ærlig testpakke; meningsfulde assertions og de svære ting reelt testet. Væsentlige huller: 502-stien, utestet `CodeChallengeApiClient`-serialisering og manglende `Basket`-concurrency-test. God kvalitet, men ikke "færdig".

---

## 4. Kravopfyldelse (Product Owner)

Krav udledt direkte fra `doc/IMPACT_CodeChallenge_Challenge_Simple.pdf` og verificeret mod koden + upstream-schema.

| Krav (PDF) | Status | Hvor | Note |
|---|---|---|---|
| Add / remove / inc-dec quantity / submit | ✅ | `BasketsController.cs:21-37`, `BasketService.cs`, `OrderService.cs` | PUT sætter absolut mængde (dækker inc/dec). |
| Submit → upstream `CreateOrder` | ✅ | `CodeChallengeApiClient.cs:22-37` | Payload matcher upstream-schema. |
| Endpoints ≠ 1:1 med operationer | ✅ | `BasketsController.cs` | REST-ressourcer. |
| Kun top-100 må købes | ✅ | `BuyableProductCatalog.cs`, håndhævet ved add (`BasketService.cs:27`) OG submit (`OrderService.cs:25`) | Revalideres ved submit. |
| Ingen klient-auth / ingen email på egne endpoints | ✅ | Controllers; email kun internt (`TokenProvider.cs:40`) | Verificeret: ingen email-param på noget endpoint. |
| Top-ranked 100 endpoint | ✅ | `ProductsController.cs:10-12` | |
| Pagineret katalog, pris asc, pageSize ≤ 1000 | ✅ | `ProductQueryService.cs:11,19-53` | Validerer ≤0 og >1000. |
| Get basket via GUID | ✅ | `BasketsController.cs:18-19` | Route `{id:guid}`. |
| 10 billigste af hele kataloget | ✅ | `ProductQueryService.cs:36-40` | |
| Korrekt rangordning af "10.000 ranked products" | ⚠️ | `ProductRanking.cs:7-12` | Fortolkning — se nedenfor. |
| In-memory storage | ✅ | `InMemoryBasketRepository.cs` | ConcurrentDictionary. |
| SOLID / production-ready / unit + e2e | ✅ | Lagdeling + 67 tests | |
| Kør lokalt + tests uden ændringer | ✅ | README; default-tests netværksfri | |
| Public repo, flere commits, README m. designvalg | ✅ | git-historik, README | |

### Vigtigste kravnote
- **⚠️ Rangordnings-fortolkningen er en antagelse, ikke en verificeret kontrakt.** PDF'en taler om "ranked" og "top-ranked 100". Upstream-schemaet eksponerer KUN `id, name, price, size, stars` — der er **intet eksplicit rank-felt**. Koden udleder rang fra `stars` desc + `id` asc (`ProductRanking.cs:9-10`). Det er forsvarligt, men en lige så plausibel læsning er, at API'et returnerer produkterne *allerede rangordnet*, og at "top-100" er de første 100 i listen. Hvis sidstnævnte var intentionen, er et forkert sæt på 100 produkter "købbare". Det er den eneste reelle kravrisiko — vær klar til at begrunde valget (og tie-break "laveste id") mundtligt i interviewet.
- Decrease quantity er ikke et selvstændigt verbum (dækkes af PUT med absolut mængde); acceptabelt under "endpoints ≠ operationer", men frontend skal selv beregne ny mængde.

### Scope
Generelt disciplineret afgrænset. Det tungeste ekstra-stykke ift. et 2-timers scope er baggrunds-refresh + single-flight-caching — men velbegrundet givet det ~30s langsomme upstream-kald, og dokumenteret i README. Ingen unødvendige endpoints, ingen DB-/frontend-scope-creep.

**PO-dom:** Afleveringen lever op til opgaven. Alle krævede endpoints og operationer er til stede, korrekte og fornuftigt designet, og de tre "subtile" krav (anonymitet, top-100-begrænsning, page-size-loft) er ramt præcist og håndhævet flere steder. Den eneste reelle kravusikkerhed er fortolkningen af "ranked".

---

## Samlet vurdering & prioriteret handlingsspringbræt

Hvis dette skulle hærdes mod produktion, i rækkefølge:

1. **`double → decimal` på pris** (`CodeChallengeApiClient.cs:40`) — afrund ved indlæsning. Betalingsdomæne; reel korrekthed.
2. **`BasketItem`-trådsikkerhed** — gør item'et immutabelt så `Basket`-låsen reelt dækker (lukker også gabet ift. commit-beskeden), + test for samtidige mutationer.
3. **502-stien + `CodeChallengeApiClient`-serialisering testes** — de to største testhuller; billige at lukke.
4. **Beslut `ProductCacheTtl`** — implementér en faktisk TTL eller fjern feltet; ret README så warm-up-påstanden matcher adfærden.
5. **Begrund ranking-fortolkningen** (eller bekræft mod IMPACT) — den eneste åbne kravrisiko.
6. Mindre: `Size.ToString(InvariantCulture)`, læs `basket.Items` én gang i submit, cache top-100-sættet, overvej resilience-pipeline.

## Verdict
**Minor changes needed.** Alle krav er opfyldt, og løsningen er velstruktureret, idiomatisk og klart over niveau for en kodeudfordring — ren Clean Architecture, gennemtænkt concurrency og robust upstream-integration. De åbne punkter er få, isolerede og ikke-blokerende; de vigtigste er to subtile korrekthedsdetaljer (pris-cast, `BasketItem`-race), tre konkrete testhuller, og én kravfortolkning der bør kunne forsvares. Ingen af dem forhindrer aflevering eller drift på opgavens skala.
