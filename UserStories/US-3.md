# US-3: Produktkatalog med caching og warm-up

## Brugerhistorie
Som Basket API vil jeg hente hele produktkataloget og holde det i en cache med eager warm-up ved app-start og baggrundsfornyelse, så de tunge ~30-sekunders upstream-kald ikke rammer slutbrugere.

## Kontekst
Enabler for alle produkt- og kurv-features. Refererer til planens B3 og Trin 5 (`CachedProductCatalog`, `CatalogWarmupHostedService`). Upstream returnerer 10.000 produkter uden paginering og er bekræftet langsomt (~30 sek).

## Afhængigheder
US-1, US-2.

## Scope
- `ICodeChallengeApiClient.GetAllProducts` (henter rå katalog) + mapping til `Product` (Domain).
- `IProductCatalog`-abstraktion der eksponerer det cachede katalog.
- `CachedProductCatalog`: in-memory katalog; single-flight-semafor så kun ét upstream-kald kører ad gangen; TTL/refresh fra options; stale-while-revalidate (serverer eksisterende katalog mens fornyelse kører).
- `CatalogWarmupHostedService` (`BackgroundService`): henter kataloget ved start og forny periodisk før TTL udløber.

## Acceptkriterier
- AC3-1: Kataloget hentes automatisk ved app-start (warm-up).
- AC3-2: Forespørgsler efter warm-up serveres fra cache uden nyt upstream-kald.
- AC3-3: Samtidige cold-cache-forespørgsler udløser kun ét upstream-kald (single-flight).
- AC3-4: Ved fornyelse serveres det eksisterende katalog indtil nyt er hentet.
- AC3-5: Hvis warm-up fejler (upstream nede), fejler katalog-afhængige endpoints kontrolleret (502/503) indtil næste vellykkede fornyelse.

## Opgaver (TDD)
1. Skriv fejlende unit-tests: cache-hit kalder ikke upstream igen; single-flight (N samtidige → ét kald); fornyelse erstatter katalog.
2. Definér `IProductCatalog` + Domain-`Product` + mapping.
3. Implementér `CachedProductCatalog` (semafor, TTL).
4. Implementér `CatalogWarmupHostedService`.
5. Wire DI; gør tests grønne.

## Tests
- Unit med fake `ICodeChallengeApiClient` (kald-tæller).
- Reel-API-verifikation af katalog-shape (10.000, felter) sker i US-8.

## Definition of Done
- [ ] Warm-up henter kataloget ved start.
- [ ] Cache-hit rammer ikke upstream.
- [ ] Single-flight verificeret.
- [ ] Baggrundsfornyelse implementeret.
- [ ] Unit-tests grønne.
