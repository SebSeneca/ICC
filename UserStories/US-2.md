# US-2: Upstream-autentificering (token-håndtering)

## Brugerhistorie
Som Basket API vil jeg automatisk autentificere mig mod Code Challenge API'et med et cachet token, der fornyes ved udløb/401, så klienter aldrig skal håndtere auth eller email.

## Kontekst
Enabler for al upstream-kommunikation. Refererer til planens Trin 2/5, B3 (token-cache), B5 (email fra config) og T-AC10. Login kræver ikke en reel email — template `seb@challenge.dk` bruges.

## Afhængigheder
US-1.

## Scope
- `CodeChallengeApiOptions` (`BaseUrl`, `Email`, `ProductCacheTtl`, `RefreshInterval`) bundet fra konfiguration; `Email` default `seb@challenge.dk` i `appsettings.json`.
- Abstraktioner: `ITokenProvider`, `ICodeChallengeApiClient` (typed `HttpClient`).
- `TokenProvider`: `Login` med email fra options; cache token; forny ved udløb/401. Token logges aldrig.
- `CodeChallengeApiClient`: sætter Bearer fra `ITokenProvider`; ved 401 → forny token og retry én gang.

## Acceptkriterier
- AC2-1 (plan-AC10): Første upstream-kald logger ind og cacher token; efterfølgende kald genbruger tokenet.
- AC2-2: Ved 401 fornyes token og det fejlede kald retries præcis én gang.
- AC2-3: Email/token optræder ikke i logs.
- AC2-4: Ingen af vores egne endpoints kræver eller accepterer email/auth (plan-AC9) — verificeres løbende.

## Opgaver (TDD)
1. Skriv fejlende unit-tests: login-once-then-reuse; 401→refresh+retry-once; email tages fra options.
2. Definér interfaces og options.
3. Implementér `TokenProvider` (cache + lås).
4. Implementér `CodeChallengeApiClient` med Bearer + 401-retry.
5. Wire DI/`IHttpClientFactory`; gør tests grønne.

## Tests
- Unit (NSubstitute fake af HTTP/login): token genbrug, 401-retry, email fra config.
- Reel-API-verifikation af login sker i US-8.

## Definition of Done
- [ ] Token hentes, caches og genbruges.
- [ ] 401 udløser én fornyelse + retry.
- [ ] Options bundet fra config; ingen secret nødvendig.
- [ ] Unit-tests grønne.
