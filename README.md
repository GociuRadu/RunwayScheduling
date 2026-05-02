# RunwayScheduling

Aplicație full-stack pentru simularea și compararea algoritmilor de planificare a zborurilor pe pistele unui aeroport. Implementează un solver greedy și un solver hibrid Genetic Algorithm + CP-SAT, cu UI interactiv pentru configurare și vizualizare rezultate.

## Stack

| Strat | Tehnologie |
|---|---|
| Backend | ASP.NET Core Minimal API (.NET 10), MediatR (CQRS), EF Core, PostgreSQL |
| Frontend | React 19, TypeScript, Vite |
| Auth | JWT Bearer, BCrypt |
| Solver | Greedy + Genetic Algorithm + CP-SAT (Google OR-Tools) |
| Infra | Docker Compose, GitHub Actions CI/CD |
| Teste | xUnit, Coverlet |

## Structura proiectului

```
src/
  Api/                   Composition root — DI, HTTP pipeline, auth, EF migrations
  Modules.Aircrafts/     Generare și listare aeronave (wake turbulence category)
  Modules.Airports/      Aeroporturi și piste (tip pistă, stare activă)
  Modules.Login/         Autentificare, emitere JWT
  Modules.Scenarios/     Configurații scenarii, zboruri, meteo, evenimente aleatorii
  Modules.Solver/        Solvere (Greedy, Genetic+CP-SAT), comparare, benchmark
  frontend/              SPA React (Vite)

tests/
  RunwayScheduling.Tests/  Unit + Integration (xUnit, EF InMemory)

scripts/
  start-dev.bat            Pornește backend + frontend în paralel
  coverage.bat             Teste cu raport HTML coverage
  generate-scenario.mjs    Generează scenario-500.json

docs/
  DB.png                   Schema baza de date
```

## Cum rulezi local

### Rapid (Windows)

```bat
scripts\start-dev.bat
```

### Manual

**Backend** — necesită PostgreSQL pe `localhost:5433`:
```bash
dotnet restore RunwayScheduling.slnx
dotnet run --project src\Api\Api.csproj
# http://localhost:5000
```

**Frontend:**
```bash
cd src\frontend
npm install
npm run dev
# http://localhost:5173
```

### Docker Compose (recomandat)

```bash
cd src
cp .env.example .env    # completează valorile
docker compose up --build
# API: :5186  |  Frontend: :3000  |  DB: :5433
```

## Autentificare

Toate endpoint-urile necesită token JWT, cu excepția `POST /login`:

```
Authorization: Bearer <token>
```

`POST /login` este rate-limited la 5 cereri/minut.

## Endpoint-uri API

### Auth
| Metodă | Rută | Descriere |
|--------|------|-----------|
| POST | `/login` | Obține token JWT |

### Aeroporturi & Piste
| Metodă | Rută | Descriere |
|--------|------|-----------|
| POST | `/airport` | Creare aeroport |
| GET | `/airports` | Listare aeroporturi |
| DELETE | `/airports/{id}` | Ștergere aeroport |
| POST | `/airports/{id}/runways` | Adăugare pistă |
| GET | `/airports/{id}/runways` | Listare piste |
| PUT | `/runways/{id}` | Actualizare pistă |
| DELETE | `/runways/{id}` | Ștergere pistă |

### Scenarii
| Metodă | Rută | Descriere |
|--------|------|-----------|
| POST | `/scenarios/configs` | Creare configurație scenariu |
| GET | `/scenarios/configs` | Listare configurații |
| GET | `/scenarios/configs/{id}` | Date complete scenariu |
| DELETE | `/scenarios/configs/{id}` | Ștergere scenariu |
| POST | `/flights/generate/{id}` | Generare zboruri |
| GET | `/flights/{id}` | Listare zboruri |
| POST | `/weatherintervals/generate/{id}` | Generare intervale meteo |
| GET | `/weatherintervals/{id}` | Listare intervale meteo |
| POST | `/aircrafts/generate/{id}` | Generare aeronave |
| GET | `/aircrafts/{id}` | Listare aeronave |
| POST | `/scenarios/{id}/random-events` | Adăugare eveniment aleatoriu |
| GET | `/random-events/{id}` | Listare evenimente |
| PUT | `/random-events/{id}` | Actualizare eveniment |
| DELETE | `/random-events/{id}` | Ștergere eveniment |

### Solver
| Metodă | Rută | Descriere |
|--------|------|-----------|
| GET | `/greedy/{id}` | Planificare greedy |
| GET | `/genetic/{id}` | Planificare Genetic + CP-SAT |
| GET | `/compare/{id}` | Comparație Greedy vs Genetic |
| POST | `/solver/solve-from-payload` | Solver direct din JSON (fără cont) |
| POST | `/solver/benchmark` | Benchmark parametri GA pe mai multe configurații |
| GET | `/solver/benchmarks` | Listare rezultate benchmark salvate |

## Solve from Payload

Poți rula solverul fără cont și fără bază de date. În UI folosești butonul **Import JSON** din pagina Solver.

**Structură minimă:**
```json
{
  "scenarioConfig": {
    "name": "Demo",
    "startTime": "2026-06-15T06:00:00.000Z",
    "endTime": "2026-06-15T14:00:00.000Z",
    "baseSeparationSeconds": 45
  },
  "runways": [
    { "name": "08L", "runwayType": 0, "isActive": true },
    { "name": "08R", "runwayType": 1, "isActive": true }
  ],
  "flights": [
    { "callsign": "ROT001", "type": 0, "scheduledTime": "2026-06-15T06:30:00.000Z", "maxDelayMinutes": 20, "maxEarlyMinutes": 0, "priority": 1 },
    { "callsign": "WIZ100", "type": 1, "scheduledTime": "2026-06-15T07:30:00.000Z", "maxDelayMinutes": 30, "maxEarlyMinutes": 10, "priority": 1 }
  ],
  "weatherIntervals": [],
  "randomEvents": []
}
```

**Valori enum:**

| Câmp | Valori |
|------|--------|
| `runwayType` | `0` Landing, `1` Takeoff, `2` Both |
| `flight.type` | `0` Arrival, `1` Departure, `2` OnGround |
| `weatherSeverity` | `0` Clear, `1` Light, `2` Moderate, `3` Heavy, `4` Severe, `5` Storm |

Scenariu cu 500 de zboruri: `scripts/scenario-500.json`.

## Teste

```bash
dotnet test tests\RunwayScheduling.Tests\RunwayScheduling.Tests.csproj
```

Cu raport coverage HTML:
```bash
scripts\coverage.bat
```

## CI/CD

- **CI** (`ci.yml`): push/PR pe `main` sau `develop` → build + test backend, lint + build frontend, build Docker
- **CD** (`cd.yml`): trigger manual → publică imaginile pe GitHub Container Registry

## Note

- Migrarile EF se aplică automat la startup
- Solverul genetic inițializează jumătate din populație din soluția greedy
- CP-SAT rafinează ferestrele de timp ale elitelor în fiecare generație
- Fitness = penalitate (mai mic = mai bun): anulare → 180×, întârziere → minute×, devans → minute×0.5, totul ponderat cu `1.2^(priority-1)`
