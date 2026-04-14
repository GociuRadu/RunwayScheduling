# RunwayScheduling

RunwayScheduling este o aplicație full-stack pentru simularea operațiunilor pe piste și compararea algoritmilor de planificare a zborurilor în scenarii aeroportuare încărcate.

Backend-ul este construit ca modular monolith în .NET 10, iar frontend-ul este un SPA React + Vite. Proiectul include două implementări de solver:

- `Greedy`, pentru o planificare rapidă și predictibilă
- `Genetic Algorithm + CP-SAT`, pentru optimizare și experimente comparative

## Stack

- Backend: ASP.NET Core Minimal API, MediatR, Entity Framework Core, PostgreSQL
- Frontend: React, TypeScript, Vite
- Auth: JWT bearer
- Tooling: Docker Compose, xUnit, Coverlet, GitHub Actions

## Structură

```text
src/
  Api/                        Composition root, pipeline HTTP, EF, auth
  Modules.Aircrafts/          Generare și listare aeronave
  Modules.Airports/           Aeroporturi și piste
  Modules.Login/              Login și token issuing
  Modules.Scenarios/          Configuri, zboruri, vreme, evenimente
  Modules.Solver/             Solverele și încărcarea snapshot-ului
  Modules.Solver.Benchmarks/  Benchmark pentru tuning GA
  frontend/                   Aplicația React
tests/
  RunwayScheduling.Tests/     Teste unitare și de integrare ușoară
```

## Fluxul aplicației

1. Creezi un aeroport și pistele active.
2. Creezi un `ScenarioConfig`.
3. Generezi aeronave și zboruri pentru scenariu.
4. Adaugi opțional intervale meteo și evenimente aleatorii.
5. Rulezi solverul `Greedy`, `Genetic` sau endpoint-ul de comparație.

## Endpoint-uri importante

- `POST /login`
- `POST /airport`
- `GET /airports`
- `POST /airports/{airportId}/runways`
- `POST /scenarios/configs`
- `POST /flights/generate/{scenarioConfigId}`
- `POST /weatherintervals/generate/{scenarioConfigId}`
- `POST /scenarios/{scenarioConfigId}/random-events`
- `GET /greedy/{scenarioConfigId}`
- `GET /genetic/{scenarioConfigId}`
- `GET /compare/{scenarioConfigId}`

Toate endpoint-urile, în afară de `POST /login`, cer token JWT.

## Rulare locală

### Variantă rapidă

```powershell
scripts\start-dev.bat
```

### Backend manual

```powershell
dotnet restore RunwayScheduling.slnx
dotnet build RunwayScheduling.slnx
dotnet run --project src\Api\Api.csproj
```

### Frontend manual

```powershell
cd src\frontend
npm install
npm run dev
```

## Benchmark și tuning

Pentru benchmark-ul solverului genetic:

```powershell
run-benchmark.bat
```

Sau direct:

```powershell
dotnet run --project src\Modules.Solver.Benchmarks --configuration Release -- --output benchmark_results.csv
```

Detaliile de tuning sunt documentate în `PARAMETER_TUNING.md`.

## Testare

```powershell
dotnet test tests\RunwayScheduling.Tests\RunwayScheduling.Tests.csproj
```

Pentru coverage:

```powershell
scripts\coverage.bat
```

## Observații

- Migrațiile EF sunt aplicate la startup din proiectul API.
- Solverul genetic folosește aceleași reguli de scheduling ca fallback-ul greedy, pentru a păstra consistența rezultatelor.
- Proiectul de benchmark este inclus în soluție pentru experimente și reglaj de parametri.
