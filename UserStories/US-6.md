# US-6: Håndtér kurv-linjer (tilføj, sæt antal, fjern)

## Brugerhistorie
Som frontend-udvikler vil jeg kunne tilføje produkter til kurven, ændre antal (øg/sænk) og fjerne produkter, hvor kun de top-100 købbare produkter må lægges i kurven, så brugeren kan sammensætte sin ordre korrekt.

## Kontekst
Refererer til planens B4 (købsregel), B6 (sæt-antal dækker øg/sænk), Trin 4/6 og plan-AC6–AC7. Købbar = blandt top-100 (`stars` desc).

## Afhængigheder
US-5 (kurv), US-4 (ranking/købbarhed), US-3 (katalog).

## Scope
- `IBasketService`: tilføj produkt, sæt antal, fjern produkt — med validering mod katalog/købbarhed.
- Genbrug ranking/købbarheds-logik fra US-4.
- DTO'er: `AddItemRequest` (`productId`, `quantity`), `SetQuantityRequest` (`quantity`).
- `BasketsController`: `POST /api/baskets/{id}/items`, `PUT /api/baskets/{id}/items/{productId}`, `DELETE /api/baskets/{id}/items/{productId}`.

## Acceptkriterier
- AC6-1 (plan-AC6): Tilføj et top-100-produkt → linje oprettes; gentag → antal øges.
- AC6-2: Tilføj et ikke-købbart produkt → afvist med 400/422 + forklarende ProblemDetails.
- AC6-3 (plan-AC7): Sæt antal opdaterer linjen; antal < 1 er ugyldigt (brug fjern) → 400.
- AC6-4: Fjern fjerner linjen; fjern/ændr ukendt produkt i kurv → 404; operationer på ukendt kurv → 404.

## Opgaver (TDD)
1. Fejlende unit-tests: add købbart (opret + øg), add ikke-købbart (afvist), sæt antal, antal<1 fejl, fjern, ukendt produkt/kurv.
2. Fejlende e2e-tests: add ikke-købbart → 400/422; add+sæt+fjern roundtrip.
3. Implementér service-operationer + validering (genbrug købbarheds-tjek).
4. Implementér controller-endpoints + request-DTO'er.
5. Gør tests grønne.

## Definition of Done
- [ ] Add/sæt-antal/fjern virker iht. acceptkriterier.
- [ ] Kun top-100 kan tilføjes; ikke-købbare afvises forklarende.
- [ ] Korrekt 404/400-håndtering.
- [ ] Unit- + e2e-tests grønne.
