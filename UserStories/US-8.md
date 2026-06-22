# US-8: Integrationstests mod det reelle API

## Brugerhistorie
Som team vil vi have integrationstests, der kører mod det reelle Code Challenge API, så vi verificerer den faktiske kontrakt (login, katalog, ordre) ud over de hurtige mockede tests.

## Kontekst
Opgaven kræver at vi også tester mod det reelle API. Disse tests adskilles fra den almindelige (netværksfri) test-suite, så `dotnet test` som standard forbliver hurtigt og uafhængigt af netværk (plan-AC11), mens integrationstests kan køres on-demand. Den gemte `doc/GetAllProducts.json` bruges fortsat som hurtig fixture i de mockede tests — integrationstests rammer det levende API.

## Afhængigheder
US-2 (auth), US-3 (katalog), US-7 (ordre). Kan udvikles parallelt efterhånden som de features lander.

## Scope
- Integrationstests markeret med kategori/trait (fx `[Trait("Category","Integration")]`) så de kan filtreres fra/til.
- Verificér mod reelt upstream:
  - Login returnerer et token med template-email.
  - `GetAllProducts` returnerer ~10.000 produkter med forventede felter (`id, name, price, size, stars`).
  - (Hvis forsvarligt) en order-roundtrip via vores submit-flow mod reelt `CreateOrder`.
- Dokumentér hvordan integrationstests køres separat (fx `dotnet test --filter Category=Integration`) og at default-runnet ekskluderer dem.

## Acceptkriterier
- AC8-1: Integrationstests kører grønt mod det reelle API.
- AC8-2: Default `dotnet test` (uden filter) ekskluderer integrationstests og kræver hverken netværk eller hemmeligheder.
- AC8-3: Integrationstest bekræfter katalog-størrelse og felt-shape mod live-data.
- AC8-4: Kørselsmåde for integrationstests er dokumenteret.

## Opgaver
1. Opret integrationstest-klasse(r) med kategori-trait.
2. Implementér login- og katalog-kontrakt-tests mod reelt API.
3. Vurdér og (hvis forsvarligt) implementér en order-roundtrip-test.
4. Konfigurér test-filtrering så default-run er netværksfrit.
5. Dokumentér kørsel i README (kobles til US-9).

## Definition of Done
- [ ] Integrationstests verificerer reel upstream-kontrakt.
- [ ] Default test-run er netværksfrit og grønt.
- [ ] Kørsel af integrationstests dokumenteret.
