# US-1: Lagdelt solution-fundament

## Brugerhistorie
Som udvikler vil jeg have en lagdelt solution med separat testprojekt og fælles fejlhåndtering, så vi kan bygge resten af Basket API'et testdrevet og efter SOLID.

## Kontekst
Fundament for alt videre arbejde. Refererer til planens Trin 0 samt B2 (lagdeling) og B12 (fejlformat). Det eksisterende web-projekt `ImpactCodeChallence` bliver Api-laget; scaffold (WeatherForecast) fjernes.

## Afhængigheder
Ingen (første story).

## Scope
- Opret projekter: `ImpactCodeChallence.Domain`, `.Application`, `.Infrastructure`, og testprojekt `.Tests` (xUnit).
- Projektreferencer: Application→Domain; Infrastructure→Application,Domain; Api→Application,Infrastructure,Domain; Tests→alle.
- Tilføj alle projekter til `ImpactCodeChallence.slnx`.
- Fjern `WeatherForecast.cs` og `Controllers/WeatherForecastController.cs`.
- NuGet: test-pakker (`Microsoft.AspNetCore.Mvc.Testing`, `NSubstitute`, `FluentAssertions`), `Microsoft.Extensions.Http` og `Microsoft.Extensions.Caching.Memory` i Infrastructure.
- Global ProblemDetails-fejlhåndtering (exception-handler/middleware) der mapper til 400/404/422/502.

## Acceptkriterier
- AC1-1: `dotnet build` på solution lykkes.
- AC1-2: `dotnet test` kører grønt (mindst én triviel bestået test som baseline).
- AC1-3: Ingen scaffold-rester tilbage (WeatherForecast væk).
- AC1-4: Ukendt rute / ufanget exception returnerer `ProblemDetails` med korrekt statuskode.

## Opgaver (TDD)
1. Skriv en fejlende test der rammer en bevidst fejl-rute og forventer `ProblemDetails`-struktur.
2. Opret projekter, referencer og solution-registrering.
3. Fjern scaffold-filer.
4. Implementér ProblemDetails-fejlhåndtering i Api.
5. Bekræft build + test grøn.

## Definition of Done
- [ ] Lagdelt solution bygger.
- [ ] Testprojekt kører grønt.
- [ ] Scaffold fjernet.
- [ ] ProblemDetails-fejlhåndtering på plads og dækket af en test.
