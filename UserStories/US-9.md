# US-9: README og dokumentation

## Brugerhistorie
Som reviewer vil jeg have en README, der forklarer hvordan løsningen køres og testes, samt de centrale design-beslutninger, så jeg kan køre og forstå projektet uden yderligere hjælp.

## Kontekst
Opgavekrav: kør lokalt uden ændringer, kør tests, kort README med design-beslutninger, og en note om AI-brug (efterspørges i interviewet). Refererer til planens Trin 9. README skrives på engelsk (repo-/kode-dokumentation).

## Afhængigheder
Alle øvrige stories (dokumenterer den færdige løsning).

## Scope
- Opdatér `README.md` med:
  - Kørsel: `dotnet run` (Api) og `dotnet test`.
  - Konfiguration: `CodeChallengeApi`-sektion (BaseUrl, Email-template, TTL/refresh).
  - Arkitektur/lagdeling og endpoint-oversigt.
  - Design-beslutninger: ranking (stars desc + tie-break), caching/warm-up, in-memory kurve, anonymitet (ingen klient-auth).
  - Kørsel af integrationstests (fra US-8) vs. default netværksfrit run.
  - Kort AI-brug-note.

## Acceptkriterier
- AC9-1: En udefrakommende kan køre løsning + tests udelukkende ud fra README.
- AC9-2: README dokumenterer de centrale design-beslutninger.
- AC9-3: README beskriver hvordan både mockede tests og integrationstests køres.
- AC9-4: AI-brug-note inkluderet.

## Opgaver
1. Skriv README-sektioner (kørsel, config, arkitektur, endpoints, beslutninger, test, AI-note).
2. Verificér instruktionerne mod en ren clone.

## Definition of Done
- [ ] README dækker kørsel, config, arkitektur, beslutninger og test.
- [ ] AI-brug-note med.
- [ ] Instruktioner verificeret.
