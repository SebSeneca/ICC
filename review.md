# Review-rapport: BasketSystem (IMPACT Code Challenge)

Fokuseret review af solution-struktur og kravopfyldelse mod opgaven (`doc/IMPACT_CodeChallenge_Challenge_Simple.pdf`). Diff: `main` (commit `fbb356d`) → `HEAD` (`ca99b65`). Tid var en faktor — dette er en målrettet gennemgang, ikke en udtømmende audit.

## Summary
En lagdelt .NET 10 Basket API oven på Code Challenge API'et, bygget i 9 user stories med 67 tests. Alle funktionelle krav i opgaven er opfyldt og verificeret end-to-end mod det reelle API.

## Changes Overview (projekt-niveau)
| Område | Type | Indhold |
|--------|------|---------|
| `BasketSystem.Domain` | Added | `Product`, `Basket`, `BasketItem`, `Order` + invarianter |
| `BasketSystem.Application` | Added | Ports, DTO'er, services (produkter, kurve, ordre), exceptions |
| `BasketSystem.Infrastructure` | Added | Upstream-klient, token-handler, katalog-cache + warm-up, in-memory repo |
| `BasketSystem.Api` | Modified | Controllers, DI, ProblemDetails-fejlhåndtering |
| `BasketSystem.Tests` | Added | 64 netværksfri + 3 opt-in integrationstests |
| `plans/`, `UserStories/`, `doc/` | Added | Plan, 9 stories, opgave-PDF + datadump |

## Kravopfyldelse
| Krav (PDF) | Status | Note |
|------------|--------|------|
| Add/remove/øg/sænk antal/submit | ✅ | `BasketsController` + `BasketService`/`OrderService` |
| Endpoints designet (ikke 1:1 med operationer) | ✅ | Sæt-antal dækker øg/sænk idempotent |
| Top-ranked 100 | ✅ | `stars` desc, tie-break `id` (`ProductRanking`) |
| Pagineret katalog, pris asc, pageSize ≤ 1000 | ✅ | `ProductQueryService.GetByPriceAsync` |
| Hent kurv via GUID | ✅ | `GET /api/baskets/{id:guid}` |
| 10 billigste | ✅ | `GET /api/products/cheapest` |
| Kun top-100 må købes | ✅ | Håndhævet ved add (422) + revalideret ved submit |
| Submit → CreateOrder | ✅ | Verificeret med ægte ordre i upstream |
| Ingen klient-auth/email | ✅ | Auth rent internt; `userEmail` fra config |
| In-memory storage | ✅ | `ConcurrentDictionary` |
| SOLID | ✅ | Lagdelt, ports i Application, afhængigheder indad |
| Unit + e2e tests | ✅ | + opt-in integration mod reelt API |
| Kør lokalt + tests uden ændringer | ✅ | Default `dotnet test` er netværksfri |
| Public repo, flere commits, første commit ved start | ✅ | Plan-commit først, derefter per-story |
| README med design-beslutninger + AI-note | ✅ | `README.md` |

## What's Good
- **Ren lagdeling efter SOLID.** Afhængigheder peger indad; Domain er afhængighedsfrit; Application definerer ports som Infrastructure implementerer. Forretningslogik er fri for HTTP-detaljer.
- **Robust upstream-integration.** Token caches og fornyes ved 401 via `AuthenticationDelegatingHandler` (med korrekt request-cloning til retry). Katalog-cache med eager warm-up, stale-while-revalidate og single-flight — adresserer den reelle ~30s-latens.
- **Korrekt "source of truth" ved submit.** Pris/navn hentes fra kataloget, ikke fra klient-input, og købbarhed revalideres (`OrderService.SubmitAsync`).
- **Konsekvent fejlhåndtering.** Én `GlobalExceptionHandler` mapper validering→400, ikke-fundet→404, forretningsregel→422, upstream→502 som `ProblemDetails`. Ingen stacktraces lækkes.
- **Meningsfuld testdækning.** 67 tests på tværs af lag; e2e er netværksfri via fakes; integrationstests er opt-in så default-runnet forbliver hurtigt og uafhængigt. Ranking/paginering/købbarhed testet præcist.
- **Sporbar proces.** Plan + 9 nummererede stories + per-story commits gør beslutninger og forløb læsbare.

## Areas for Improvement
- **Kurv-mutation er ikke trådsikker på instans-niveau** (`BasketSystem.Domain/Basket.cs`). `ConcurrentDictionary` beskytter selve store'et, men `Basket` bruger en almindelig `Dictionary`, og `AddItem`/`SetItemQuantity`/`RemoveItem` muteres uden lås. To samtidige requests mod *samme* kurv kan i princippet race/korruptere. For en webshop-kurv er samtidige operationer plausible. *Lav-til-middel* — bør i det mindste nævnes som kendt begrænsning; en lås pr. kurv eller en immutabel kurv ville lukke det.
- **`doc/GetAllProducts.json` (~1,2 MB, 70k linjer) er committet men bruges ikke.** Planen lagde op til at bruge den som test-fixture, men e2e-tests bruger et syntetisk datasæt i stedet. Den er reelt dødvægt i repo'et. *Lavt* — overvej at fjerne den eller faktisk bruge den.
- **`BuyableProductCatalog` afhænger af `ProductQueryService.TopRankedCount`** (`BasketSystem.Application/Products/BuyableProductCatalog.cs`). Konstanten "top-100" bor på query-servicen, men er en delt forretningsregel. *Lavt* — flyt den til ét neutralt sted (fx en `ProductCatalogPolicy`/konstant) for at undgå kobling mellem to services.

## Suggestions
- Integrationstesten `Full_flow...` opretter en **rigtig ordre** i upstream ved hver kørsel — fint til formålet, men værd at nævne i README at det har sideeffekter.
- Overvej en kort note i README om den bevidste in-memory/ikke-trådsikre kurv-afvejning, så det fremstår som et valg frem for en forglemmelse.
- `OrderService` returnerer tom `orderId`-streng hvis upstream svarer uden id; en lille guard kunne gøre det eksplicit (lav prioritet).

## Verdict
**Minor changes needed** — alle krav er opfyldt og løsningen er produktionsnær og velstruktureret. De nævnte punkter er små og mest af typen "kendt begrænsning / oprydning"; ingen af dem blokerer aflevering. Kurv-trådsikkerheden er det eneste reelle robusthedshul og bør som minimum dokumenteres.
