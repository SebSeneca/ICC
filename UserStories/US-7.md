# US-7: Submit kurv som ordre

## Brugerhistorie
Som frontend-udvikler vil jeg kunne submitte en kurv, så der oprettes en ordre via upstream `CreateOrder`, og brugeren får sin ordrebekræftelse.

## Kontekst
Afslutter kerneflowet. Refererer til planens B8 (pris/data slås op ved submit), B9 (`size`→`productSize` som streng), Trin 4/6 og plan-AC8. `userEmail` sættes fra config (ikke fra klienten).

## Afhængigheder
US-6 (kurv-indhold), US-2 (upstream auth).

## Scope
- `IOrderService`: byg `CreateOrderRequest` og kald `ICodeChallengeApiClient.CreateOrder`.
- Mapping: `userEmail` fra options; `orderLines` fra kurv (productId, productName, productUnitPrice, `productSize` = `size.ToString()`, quantity, totalPrice = pris×antal); `totalAmount` = Σ linje-totaler.
- Revalidér at alle linjer stadig er købbare og brug aktuel katalog-pris (kilden er sandhed).
- DTO'er: `OrderDto`/`OrderLineDto`.
- `BasketsController`: `POST /api/baskets/{id}/submit`.

## Acceptkriterier
- AC7-1 (plan-AC8): Submit af ikke-tom, gyldig kurv kalder upstream `CreateOrder` med korrekt `userEmail`, `totalAmount`, og `orderLines`, og returnerer ordren.
- AC7-2: Tom kurv → 400.
- AC7-3: Kurv med ikke-købbart produkt → 422 (ProblemDetails).
- AC7-4: Pris/produktnavn til ordren tages fra kataloget, ikke fra klient-input.

## Opgaver (TDD)
1. Fejlende unit-tests: korrekt mapping (totaler, productSize-streng, email fra config); tom kurv → fejl; ikke-købbart → fejl; upstream-klient kaldt én gang.
2. Fejlende e2e-test: fuldt flow opret→add→submit → 200 med ordre; tom kurv → 400.
3. Implementér `OrderService` + mapping + revalidering.
4. Implementér submit-endpoint.
5. Gør tests grønne.

## Tests
- Unit med fake `ICodeChallengeApiClient`.
- E2E via `WebApplicationFactory` (fake upstream der returnerer en ordre).
- Reel-API order-roundtrip dækkes i US-8.

## Definition of Done
- [ ] Submit opretter ordre via upstream med korrekt payload.
- [ ] Tom kurv → 400; ikke-købbart → 422.
- [ ] Pris/data fra katalog, ikke klient.
- [ ] Unit- + e2e-tests grønne.
