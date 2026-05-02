# RunwayScheduling

Full-stack application for simulating and comparing runway scheduling algorithms. Implements a greedy solver and a hybrid Genetic Algorithm + CP-SAT solver, with an interactive UI for scenario configuration and result visualization.

## Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core Minimal API (.NET 10), MediatR (CQRS), EF Core, PostgreSQL |
| Frontend | React 19, TypeScript, Vite |
| Auth | JWT Bearer, BCrypt |
| Solver | Greedy + Genetic Algorithm + CP-SAT (Google OR-Tools) |
| Infra | Docker Compose, GitHub Actions CI/CD |
| Tests | xUnit, Coverlet |

## Project Structure

```
src/
  Api/                   Composition root — DI, HTTP pipeline, auth, EF migrations
  Modules.Aircrafts/     Aircraft generation and listing (wake turbulence category)
  Modules.Airports/      Airports and runways (type, active state)
  Modules.Login/         Authentication, JWT issuance
  Modules.Scenarios/     Scenario configs, flights, weather, random events
  Modules.Solver/        Solvers (Greedy, Genetic+CP-SAT), compare, benchmark
  frontend/              React SPA (Vite)

tests/
  RunwayScheduling.Tests/  Unit + Integration (xUnit, EF InMemory)

scripts/
  start-dev.bat            Start backend + frontend in parallel
  coverage.bat             Run tests with HTML coverage report
  generate-scenario.mjs    Generate scenario-500.json

docs/
  DB.png                   Database schema diagram
  SYSTEM.md                Full technical documentation
```

## Running Locally

### Quick start (Windows)

```bat
scripts\start-dev.bat
```

### Manual

**Backend** — requires PostgreSQL on `localhost:5433`:
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

### Docker Compose (recommended)

```bash
cd src
cp .env.example .env    # fill in values
docker compose up --build
# API: :5186  |  Frontend: :3000  |  DB: :5433
```

## Authentication

All endpoints require a JWT token except `POST /login`:

```
Authorization: Bearer <token>
```

`POST /login` is rate-limited to 5 requests/minute.

## API Endpoints

### Auth
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/login` | Obtain JWT token |

### Airports & Runways
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/airport` | Create airport |
| GET | `/airports` | List airports |
| DELETE | `/airports/{id}` | Delete airport |
| POST | `/airports/{id}/runways` | Add runway |
| GET | `/airports/{id}/runways` | List runways |
| PUT | `/runways/{id}` | Update runway |
| DELETE | `/runways/{id}` | Delete runway |

### Scenarios
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/scenarios/configs` | Create scenario config |
| GET | `/scenarios/configs` | List configs |
| GET | `/scenarios/configs/{id}` | Full scenario data |
| DELETE | `/scenarios/configs/{id}` | Delete scenario |
| POST | `/flights/generate/{id}` | Generate flights |
| GET | `/flights/{id}` | List flights |
| POST | `/weatherintervals/generate/{id}` | Generate weather intervals |
| GET | `/weatherintervals/{id}` | List weather intervals |
| POST | `/aircrafts/generate/{id}` | Generate aircraft |
| GET | `/aircrafts/{id}` | List aircraft |
| POST | `/scenarios/{id}/random-events` | Add random event |
| GET | `/random-events/{id}` | List random events |
| PUT | `/random-events/{id}` | Update random event |
| DELETE | `/random-events/{id}` | Delete random event |

### Solver
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/greedy/{id}` | Greedy scheduling |
| GET | `/genetic/{id}` | Genetic + CP-SAT scheduling |
| GET | `/compare/{id}` | Compare Greedy vs Genetic |
| POST | `/solver/solve-from-payload` | Solve directly from JSON body (no account needed) |
| POST | `/solver/benchmark` | Benchmark GA across multiple parameter configs |
| GET | `/solver/benchmarks` | List saved benchmark results |

## Solve from Payload

Run the solver without an account or database. Use the **Import JSON** button on the Solver page in the UI.

**Minimal structure:**
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

**Enum values:**

| Field | Values |
|-------|--------|
| `runwayType` | `0` Landing, `1` Takeoff, `2` Both |
| `flight.type` | `0` Arrival, `1` Departure, `2` OnGround |
| `weatherSeverity` | `0` Clear, `1` Light, `2` Moderate, `3` Heavy, `4` Severe, `5` Storm |

A 500-flight scenario is available at `scripts/scenario-500.json`.

## Tests

```bash
dotnet test tests\RunwayScheduling.Tests\RunwayScheduling.Tests.csproj
```

With HTML coverage report:
```bash
scripts\coverage.bat
```

## CI/CD

- **CI** (`ci.yml`): triggered on push/PR to `main` or `develop` — build + test backend, lint + build frontend, Docker build
- **CD** (`cd.yml`): manual trigger — publishes Docker images to GitHub Container Registry

## Notes

- EF migrations are applied automatically on startup
- The genetic solver seeds half the initial population from the greedy solution
- CP-SAT refines time windows of elite chromosomes every generation
- Fitness = penalty (lower is better): cancellation → 180×, delay → minutes×, early → minutes×0.5, all weighted by `1.2^(priority-1)`
