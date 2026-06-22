# US-4: Produkt-endpoints (top-100, paginering, billigste)

## Brugerhistorie
Som frontend-udvikler vil jeg kunne hente de top-rangerede produkter, et pagineret katalog sorteret efter pris, og de 10 billigste produkter, så jeg kan vise produkter i webshoppen.

## Kontekst
Første brugervendte værdi. Refererer til planens Trin 4/6, B1 (ranking) og plan-AC1–AC4. Ranking = `stars` faldende, tie-break `id` stigende. Datasættet har kun 89 femstjernede, så top-100 = 89 femstjernede + 11 firestjernede med lavest id.

## Afhængigheder
US-3.

## Scope
- `IProductQueryService`: top-100, paginering (pris asc + metadata), 10 billigste.
- `RankingService` (eller -metode): isoleret ranking-logik så nøglen let kan ændres.
- DTO'er: `ProductDto`, `PagedResult<T>`, `PaginationQuery` (validering).
- `ProductsController`: `GET /api/products/top`, `GET /api/products?page=&pageSize=`, `GET /api/products/cheapest`.

## Acceptkriterier
- AC4-1 (plan-AC1): `/api/products/top` → præcis 100, sorteret `stars` desc, tie-break `id` asc.
- AC4-2 (plan-AC2): `/api/products` → `price` asc, korrekt pagineret, med metadata (page, pageSize, totalCount, totalPages).
- AC4-3 (plan-AC3): `pageSize` > 1000 → 400; `pageSize` ≤ 0 → 400; `page` ≤ 0 → 400.
- AC4-4 (plan-AC4): `/api/products/cheapest` → de 10 laveste `price` (stigende).

## Opgaver (TDD)
1. Fejlende unit-tests: ranking (stars desc/id asc, præcis 100, <100-tilfælde); paginering (sortering, side-udsnit, metadata, sidste delvise side, page out of range); pageSize/page-validering; cheapest-10.
2. Fejlende e2e-tests for de tre endpoints (incl. pageSize=1001→400).
3. Implementér `ProductQueryService` + `RankingService` + DTO'er/validering.
4. Implementér `ProductsController`.
5. Gør tests grønne.

## Tests
- Unit: håndlavede datasæt med kontrolleret stars/price-fordeling.
- E2E: `WebApplicationFactory` med fake-klient (fixture-katalog, jf. `doc/GetAllProducts.json`).

## Definition of Done
- [ ] Tre endpoints virker iht. acceptkriterier.
- [ ] Ranking isoleret og testet.
- [ ] pageSize-grænse håndhævet.
- [ ] Unit- + e2e-tests grønne.
