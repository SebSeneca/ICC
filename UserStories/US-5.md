# US-5: Opret og hent kurv

## Brugerhistorie
Som frontend-udvikler vil jeg kunne oprette en kurv og hente den igen via dens GUID, så en bruger har en (in-memory) indkøbssession uden at logge ind.

## Kontekst
Refererer til planens B7 (eksplicit oprettelse), B13 (trådsikker in-memory store) og plan-AC5. Kurve gemmes kun in-memory — ingen DB.

## Afhængigheder
US-1.

## Scope
- Domain: `Basket` (Id som GUID, linjer) + `BasketItem`.
- `IBasketRepository` + `InMemoryBasketRepository` (`ConcurrentDictionary<Guid, Basket>`).
- `IBasketService`: opret kurv, hent kurv.
- DTO: `BasketDto`.
- `BasketsController`: `POST /api/baskets` (returnerer ny GUID + kurv), `GET /api/baskets/{id}`.

## Acceptkriterier
- AC5-1 (plan-AC5): `POST /api/baskets` opretter en tom kurv og returnerer dens GUID.
- AC5-2: `GET /api/baskets/{id}` returnerer kurven for en kendt GUID.
- AC5-3: `GET /api/baskets/{ukendt-guid}` → 404 (ProblemDetails).

## Opgaver (TDD)
1. Fejlende unit-tests: opret → ny GUID + tom kurv; hent kendt; hent ukendt → not found.
2. Fejlende e2e-test: opret → hent roundtrip; ukendt → 404.
3. Implementér Domain-`Basket`/`BasketItem`, repository, service, DTO.
4. Implementér controller-endpoints.
5. Gør tests grønne.

## Definition of Done
- [ ] Kurv kan oprettes og hentes via GUID.
- [ ] Ukendt GUID → 404.
- [ ] In-memory, trådsikker store.
- [ ] Unit- + e2e-tests grønne.
