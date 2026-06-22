# Implementeringsplan: Basket API

## Titel & Opgave
**Opgave:** IMPACT Code Challenge — byg et Basket API oven på IMPACT's Code Challenge API.
**Et-linjes resume:** Et anonymt Basket API (ASP.NET Core, .NET 10) der lader en webshop-frontend håndtere en indkøbskurv og afgive ordrer, ved at konsumere det autentificerede upstream Code Challenge API på vegne af brugeren.

---

## Kontekst & Mål
IMPACT leverer et eksisterende Code Challenge API (Azure Function) med et produktkatalog på 10.000 rangerede produkter, samt ordre-endpoints. Vi skal bygge vores eget Basket API ovenpå, som en typisk webshop-frontend kan kalde.

**Centralt princip:** Vores API er anonymt udadtil — klienter angiver aldrig email og skal ikke autentificere. Vores API autentificerer derimod *sig selv* mod upstream via et Bearer-token hentet med `Login`.

**Upstream-kontrakt (fra Swagger, `https://azfun-impact-code-challenge-api.azurewebsites.net`):**
- `POST /api/Login` — body `{ email }` → `{ token }` (JWT). Auth for vores API alene.
- `GET /api/GetAllProducts` — kræver Bearer. Returnerer alle 10.000 produkter uden paginering.
  - `ProductResponse`: `id (int)`, `name (string)`, `price (double)`, `size (int)`, `stars (int)`.
- `POST /api/CreateOrder` — kræver Bearer. Body `CreateOrderRequest { userEmail, totalAmount, orderLines[] }` → `OrderResponse`.
  - `OrderLine`: `productId (int)`, `productName (string)`, `productUnitPrice (double)`, `productSize (string)`, `quantity (int)`, `totalPrice (double)`.
- `GET /api/GetOrder/{orderId}` — kræver Bearer → `OrderResponse`.

**Mål:** Et produktionsnært, SOLID-struktureret, testdækket (unit + e2e) Basket API der kan køres og testes lokalt uden kodeændringer.

---

## Scope

### In scope
- Lagdelt solution: `Domain`, `Application`, `Infrastructure`, `Api` (eksisterende projekt) + ét xUnit-testprojekt.
- Produkt-endpoints:
  - Top-100 rangerede produkter (rangering = `stars` faldende).
  - Pagineret katalog sorteret efter `price` stigende; `pageSize` bestemmes af kalder, men afvises hvis > 1000.
  - De 10 billigste produkter ud af alle 10.000.
- Kurv-endpoints (in-memory, GUID-Id):
  - Opret kurv, hent kurv via GUID.
  - Tilføj produkt, sæt antal (dækker øg/sænk), fjern produkt.
  - Submit kurv → upstream `CreateOrder`.
- Købsregel: kun top-100 produkter (stars desc) må lægges i kurv; afvises ved add og revalideres ved submit.
- Upstream-integration: token-håndtering (cache + forny ved 401/udløb) og produktkatalog-cache med eager warm-up ved app-start + baggrundsfornyelse (pga. ~30 sek. upstream-latens).
- Upstream-email leveres via konfiguration (`appsettings.json`); en template-email (`seb@challenge.dk`) er tilstrækkelig — login kræver ikke en reel adresse.
- README med kørselsvejledning og design-beslutninger.

### Out of scope
- Persistens af kurve (DB/fil) — kun in-memory.
- End-user / frontend-authentication og email på vores egne endpoints.
- Brug af upstream `GetOrder/{orderId}` til et eget endpoint (ikke krævet). Klienten implementeres, men eksponeres ikke nødvendigvis. (Kan tilføjes hvis tid.)
- Rate limiting, betalingsflow, lager-/stock-styring.
- Frontend.

---

## Acceptkriterier
Hvert kriterium er testbart og mapper til Test Plan.

1. **AC1 — Top-100:** `GET /api/products/top` returnerer præcis 100 produkter sorteret efter `stars` faldende (tie-break: `id` stigende).
2. **AC2 — Paginering:** `GET /api/products?page=&pageSize=` returnerer produkter sorteret efter `price` stigende, korrekt pagineret, med metadata (page, pageSize, totalCount, totalPages).
3. **AC3 — pageSize-grænse:** `pageSize` > 1000 afvises med HTTP 400. `pageSize` ≤ 0 og `page` ≤ 0 afvises med 400.
4. **AC4 — Billigste 10:** `GET /api/products/cheapest` returnerer de 10 produkter med lavest `price` ud af alle 10.000 (stigende).
5. **AC5 — Opret/hent kurv:** Man kan oprette en kurv og hente den via dens GUID. Ukendt GUID → 404.
6. **AC6 — Tilføj produkt:** Tilføjelse af et top-100-produkt lægger det i kurven; gentag øger antallet. Tilføjelse af et ikke-købbart produkt afvises med 400/422 og forklarende fejl.
7. **AC7 — Sæt antal / fjern:** Antal kan sættes (øg/sænk); antal < 1 er ugyldigt (brug fjern). Fjern fjerner linjen. Operationer på ukendt kurv → 404, på produkt der ikke er i kurven → 404.
8. **AC8 — Submit:** Submit af en ikke-tom, gyldig kurv kalder upstream `CreateOrder` med korrekt `userEmail` (fra config), korrekt beregnet `totalAmount` og `orderLines` (inkl. `productSize` som streng, `totalPrice` = pris × antal), og returnerer ordren. Tom kurv → 400. Kurv med ikke-købbart produkt → 422.
9. **AC9 — Anonymitet:** Ingen af vores endpoints kræver eller accepterer email/auth fra klienten.
10. **AC10 — Token:** Vores API henter og genbruger et upstream-token og fornyer det ved 401/udløb, uden at klienten involveres.
11. **AC11 — Kør lokalt:** `dotnet test` kører grønt uden kodeændringer og uden netværk/secrets (upstream er mocket i tests).

---

## Antagelser & Beslutninger
- **B1 — Rangering = `stars` faldende.** "Top-ranked 100" tolkes som de 100 produkter med flest `stars`. Tie-break på `id` stigende for deterministisk output. Bekræftet med bruger. Hele datasættet (10.000) er analyseret: listen er ikke pré-rangeret, og `stars`-fordelingen er 1:2026, 2:2008, 3:2025, 4:3852, **5:89**. Konkret betyder top-100 = alle 89 femstjernede + de 11 firestjernede med lavest `id`. Grænsen afgøres altså af tie-break blandt 3852 firestjernede — deterministisk, men arbitrær; isoleret i `RankingService` så den let kan ændres (fx "alle femstjernede" eller anden nøgle).
- **B2 — Lagdelt arkitektur** med separate projekter for at understøtte SOLID og testbarhed. (Bekræftet.)
- **B3 — Caching:** token caches og fornyes ved 401/udløb. Produktkataloget caches in-memory og hentes **eager ved app-start** (background hosted service) med **periodisk fornyelse** før TTL udløber (stale-while-revalidate), pga. ~30 sek. upstream-latens. Et single-flight-lås (semafor) sikrer at kun ét upstream-kald kører ad gangen. Katalog-TTL/refresh-interval er konfigurerbart. (Bekræftet.)
- **B4 — Købsregel håndhæves ved add** (afvis ikke-købbare) og revalideres ved submit. (Bekræftet.)
- **B5 — Upstream-email** leveres via konfiguration (`CodeChallengeApi:Email` i `appsettings.json`), default template `seb@challenge.dk`. Login kræver ikke en reel adresse, så ingen følsomhed/secret-håndtering er nødvendig. Samme email bruges som `userEmail` ved `CreateOrder`. (Bekræftet med bruger.)
- **B6 — Øg/sænk antal** modelleres som ét idempotent "sæt antal"-endpoint (`PUT .../items/{productId}` med `{ quantity }`), frem for separate inc/dec-endpoints. Renere REST og dækker begge operationer.
- **B7 — Kurv oprettes eksplicit** via `POST /api/baskets` (returnerer GUID). Add kræver eksisterende kurv. (Alternativ auto-create fravælges for klar livscyklus.)
- **B8 — Pris og produktdata til ordren slås op i kataloget ved submit-tidspunkt** (kilden er sandhed), så klienten ikke kan diktere pris.
- **B9 — `productSize`:** upstream-produkt har `size` som `int`, men `OrderLine.productSize` er `string`. Vi konverterer `size.ToString()` ved ordreoprettelse.
- **B10 — Projektnavngivning:** eksisterende web-projekt hedder `ImpactCodeChallence` (bevidst bevaret stavemåde fra repo) og bliver Api-laget. Nye projekter navngives `ImpactCodeChallence.Domain/.Application/.Infrastructure/.Tests` for konsistens med solution-navnet.
- **B11 — HTTP-klient** implementeres med `HttpClient` via `IHttpClientFactory` (typed client). Genforsøg ved 401 håndteres eksplicit i token-laget; evt. transient-retry kan tilføjes senere (out of scope nu).
- **B12 — Fejlformat:** standard `ProblemDetails`. Validering → 400, ikke-fundet → 404, forretningsregel-brud (ikke-købbart ved submit) → 422, upstream-fejl → 502.
- **B13 — Trådsikkerhed:** in-memory kurv-store bruger `ConcurrentDictionary<Guid, Basket>`.

---

## Test Plan (skrives først — TDD)
Testene skrives før produktionskoden og forventes at fejle initielt (funktionalitet findes endnu ikke), hvorefter de bliver grønne efterhånden som koden lander. Ét testprojekt `ImpactCodeChallence.Tests` (xUnit). Mocking via NSubstitute (eller Moq). E2E via `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) hvor `ICodeChallengeApiClient` erstattes af en fake med fast testkatalog.

**Testdata:** den gemte upstream-respons (`doc/GetAllProducts.json.json`, 10.000 produkter — bør omdøbes til `GetAllProducts.json`) kan kopieres ind som embedded fixture i testprojektet og bruges af fake-klienten, så e2e-tests kører mod realistiske data uden netværk. Mindre, håndlavede datasæt bruges til præcise unit-tests (fx kontrolleret stars/price-fordeling).

### Unit-tests (Application/Domain)
- **T-AC1 RankingService:** top-100 = stars desc, tie-break id asc; returnerer præcis 100 ved ≥100 produkter; håndterer < 100 produkter (returnerer alle).
- **T-AC2/AC4 Pagination/Catalog:** sortering price asc; korrekt side-udsnit; metadata (totalCount/totalPages); cheapest = 10 laveste price; sidste side delvist fyldt; page ud over range → tom side.
- **T-AC3 Validering:** pageSize > 1000 → valideringsfejl; pageSize ≤ 0 → fejl; page ≤ 0 → fejl.
- **T-AC6/AC7 BasketService:** add købbart produkt → linje oprettet; gentag add → quantity øges; add ikke-købbart → forretningsfejl; set quantity → opdaterer; quantity < 1 → fejl; remove → linje væk; remove ukendt produkt → not found; operationer på ukendt kurv → not found.
- **T-AC8 OrderMapping/Submit:** mapper kurv → `CreateOrderRequest` korrekt (userEmail fra config, totalAmount = Σ linje-totaler, productSize som streng, totalPrice = pris×antal); tom kurv → fejl; ikke-købbart i kurv → forretningsfejl; kalder upstream-klient én gang.
- **T-AC10 TokenProvider:** første kald logger ind og cacher token; efterfølgende kald genbruger; ved simuleret 401 fornyes token og kaldet retries én gang; email tages fra config.
- **Produktcache:** anden forespørgsel inden TTL rammer ikke upstream igen (klient kaldt én gang); samtidige cold-cache-kald udløser kun ét upstream-kald (single-flight); efter warm-up er kataloget tilgængeligt uden upstream-kald pr. request.

### E2E-tests (Api via WebApplicationFactory, fake upstream)
- **E-Flow:** opret kurv → add 2 produkter → get kurv (verificér indhold/totaler) → submit → 200 med ordre.
- **E-AC1/AC4:** `GET /api/products/top` → 100 i stars-desc; `GET /api/products/cheapest` → 10 i price-asc.
- **E-AC2:** `GET /api/products?page=2&pageSize=10` → korrekt side + metadata, price asc.
- **E-AC3:** `pageSize=1001` → 400; `pageSize=0` → 400.
- **E-AC5:** `GET /api/baskets/{ukendt-guid}` → 404.
- **E-AC6:** add ikke-købbart produkt → 400/422 med ProblemDetails.
- **E-AC8:** submit tom kurv → 400.
- **E-AC9:** verificér at endpoints hverken kræver eller læser email/Authorization fra klient.

---

## Implementeringstrin
Udføres i rækkefølge, TDD-først (fejlende test → implementering → grøn). Hvert trin er lille og verificerbart.

### Trin 0 — Solution-opsætning
- Opret projekter og referencer:
  - `ImpactCodeChallence.Domain` (classlib, ingen deps).
  - `ImpactCodeChallence.Application` (classlib → Domain).
  - `ImpactCodeChallence.Infrastructure` (classlib → Application, Domain).
  - `ImpactCodeChallence` (eksisterende web → Application, Infrastructure, Domain).
  - `ImpactCodeChallence.Tests` (xUnit → alle ovenstående).
- Tilføj alle projekter til `ImpactCodeChallence.slnx`.
- Fjern scaffold-rester: `WeatherForecast.cs`, `Controllers/WeatherForecastController.cs`.
- NuGet: `Microsoft.Extensions.Http`, `Microsoft.Extensions.Caching.Memory` (Infrastructure); `Microsoft.AspNetCore.Mvc.Testing`, `NSubstitute`, `FluentAssertions` (Tests). OpenAPI/Swagger til Api (`Swashbuckle.AspNetCore` eller bevar `AddOpenApi`).
- *Verificér:* `dotnet build` lykkes.

### Trin 1 — Domain-model
- `Domain`: `Product (Id, Name, Price, Size, Stars)`, `Basket (Id, IReadOnlyCollection<BasketItem>)`, `BasketItem (ProductId, ProductName, UnitPrice, Size, Quantity)`, `Order`/`OrderLine` (eller genbrug Application-DTO). Kurv-adfærd (add/set/remove med invarianter, quantity ≥ 1) placeres på `Basket` eller i `BasketService` — vælg ét og hold det konsistent.
- *Verificér:* kompilerer; ingen eksterne deps.

### Trin 2 — Application: abstraktioner & DTO'er
- Interfaces: `ICodeChallengeApiClient` (GetAllProducts, CreateOrder), `IBasketRepository`, `IProductCatalog` (cache-abstraktion), `ITokenProvider`.
- Services/use cases: `IProductQueryService` (top/paginate/cheapest), `IBasketService` (CRUD på kurv), `IOrderService` (submit).
- DTO'er: `ProductDto`, `PagedResult<T>`, `PaginationQuery` (med validering), `AddItemRequest`, `SetQuantityRequest`, `CreateOrderRequest`/`OrderLineDto`, `OrderDto`.
- Options: `CodeChallengeApiOptions { BaseUrl, Email, ProductCacheTtl }`.

### Trin 3 — Skriv fejlende unit-tests
- Implementér alle unit-tests fra Test Plan mod interfaces/services (T-AC1, T-AC2/4, T-AC3, T-AC6/7, T-AC8, T-AC10, cache).
- *Verificér:* tests kompilerer og **fejler** (rød baseline).

### Trin 4 — Application-implementering
- `ProductQueryService`: ranking (stars desc, id asc), price-asc paginering + metadata, cheapest-10, pageSize-validering (>1000, ≤0).
- `BasketService`: add (kræver købbart = i top-100), set quantity, remove, get; not-found/valideringsfejl som domæne-resultater/exceptions.
- `OrderService`: byg `CreateOrderRequest` (email fra options, totaler, productSize→streng), revalidér købbarhed, kald `ICodeChallengeApiClient.CreateOrder`.
- *Verificér:* unit-tests fra Trin 3 bliver grønne.

### Trin 5 — Infrastructure-implementering
- `CodeChallengeApiClient` (typed `HttpClient`): kald `GetAllProducts`/`CreateOrder` med Bearer fra `ITokenProvider`; ved 401 → bed token-provider forny og retry én gang.
- `TokenProvider`: `Login` med email fra options; cache token (`IMemoryCache`/felt + lås); forny ved udløb/401. Log aldrig token.
- `InMemoryBasketRepository`: `ConcurrentDictionary<Guid, Basket>`.
- `CachedProductCatalog`: holder kataloget in-memory; single-flight-semafor om `GetAllProducts` så kun ét ~30 sek.-kald kører ad gangen; TTL/refresh fra options; serverer eksisterende katalog mens fornyelse kører (stale-while-revalidate).
- `CatalogWarmupHostedService` (`IHostedService`/`BackgroundService`): henter kataloget ved app-start og forny periodisk før TTL udløber.
- *Verificér:* infra-relaterede unit-tests grønne (token-retry, cache-hit, single-flight).

### Trin 6 — Api-lag (controllers + wiring)
- `ProductsController`: `GET /api/products/top`, `GET /api/products`, `GET /api/products/cheapest`.
- `BasketsController`: `POST /api/baskets`, `GET /api/baskets/{id}`, `POST /api/baskets/{id}/items`, `PUT /api/baskets/{id}/items/{productId}`, `DELETE /api/baskets/{id}/items/{productId}`, `POST /api/baskets/{id}/submit`.
- `Program.cs`: DI for alle services/klienter/options; `IHttpClientFactory`; `IMemoryCache`; registrér `CatalogWarmupHostedService`; ProblemDetails-fejlhåndtering (exception-middleware/handler mapper til 400/404/422/502); Swagger i Development.
- Bind options fra konfiguration (`CodeChallengeApi`-sektion: `BaseUrl`, `Email`, `ProductCacheTtl`, `RefreshInterval`).
- *Verificér:* `dotnet run` starter, warm-up logges; Swagger viser endpoints.

### Trin 7 — Skriv fejlende e2e-tests, derefter grøn
- Implementér `WebApplicationFactory`-baserede e2e-tests med fake `ICodeChallengeApiClient` (fast katalog, in-memory ordre).
- *Verificér:* e2e-tests grønne; hele `dotnet test` grøn uden netværk/secrets.

### Trin 8 — Konfiguration
- `appsettings.json`: `CodeChallengeApi: { BaseUrl, Email: "seb@challenge.dk", ProductCacheTtl, RefreshInterval }`.
- Ingen secret-håndtering nødvendig (login kræver ikke reel email).
- *Verificér:* app kan hente token og katalog lokalt med default-config.

### Trin 9 — README & dokumentation
- Opdatér `README.md`: arkitektur/lagdeling, endpoint-oversigt, kørsel (`dotnet run`/`dotnet test`), opsætning af email-secret, design-beslutninger (ranking, caching, in-memory, anonymitet), og kort AI-brug-note (krævet af opgaven).
- *Verificér:* en udefrakommende kan køre løsning + tests ud fra README alene.

### Trin 10 — Oprydning & commits
- Sikr SOLID-snit, ingen ubrugt scaffold, konsistent navngivning.
- Flere commits undervejs (opgavekrav). Commits foretages kun efter eksplicit godkendelse.
- *Verificér:* fuld `dotnet build` + `dotnet test` grøn fra ren clone (efter secret-opsætning til evt. manuel upstream-kørsel).

---

## Risici & Åbne spørgsmål
- **R1 — Rangering:** Antagelsen "stars desc" er bekræftet, men upstream-datas faktiske stars-fordeling er ikke inspiceret. Mitigering: deterministisk tie-break på id; let at justere ét sted (`RankingService`) hvis fortolkningen ændres.
- **R2 — Token-udløb:** JWT-levetid er ukendt. Vi forlader os på 401-drevet fornyelse + valgfri proaktiv udløbstjek hvis `exp` kan parses. Robust nok uden at kende levetiden.
- **R3 — Upstream-latens (~30 sek.) og -stabilitet:** Bekræftet langsom GetAllProducts; mitigeres af eager warm-up + baggrundsfornyelse + single-flight, så slutbrugere normalt ikke venter. Hvis warm-up fejler ved start (upstream nede), serverer katalog-endpoints 502/503 indtil næste vellykkede fornyelse. Tests afhænger ikke af upstream (mocket/fixture).
- **R4 — `size`/`productSize`-mismatch:** håndteret via `ToString()` (B9); hvis upstream forventer et bestemt format, kan det kræve justering — lav risiko.
- **Åbent:** Skal vi eksponere et "hent ordre"-endpoint (via upstream `GetOrder`)? Pt. out of scope; tilføjes hvis tid.

---

## Definition of Done
- [ ] Alle acceptkriterier (AC1–AC11) opfyldt og dækket af tests.
- [ ] `dotnet build` og `dotnet test` kører grønt fra en ren clone uden kodeændringer og uden netværk/secrets (upstream mocket).
- [ ] Lagdelt solution efter SOLID; ingen scaffold-rester (WeatherForecast væk).
- [ ] Unit- + e2e-tests til stede og grønne.
- [ ] Ingen klient-vendt email/auth på vores endpoints; upstream-email kun i config (template, ikke følsom).
- [ ] Token hentes/fornyes automatisk mod upstream; katalog warm-up'es ved start og fornyes i baggrunden.
- [ ] Kurve gemmes in-memory; hentbare via GUID.
- [ ] README beskriver kørsel, email-secret-opsætning og design-beslutninger (inkl. AI-brug-note).
- [ ] Flere commits foretaget (efter godkendelse).
