# RunwayScheduling — Technical Documentation

## Table of Contents

1. [Architecture](#architecture)
2. [Backend Modules](#backend-modules)
3. [Data Models](#data-models)
4. [Solver Algorithms](#solver-algorithms)
5. [Full API Reference](#full-api-reference)
6. [Frontend](#frontend)
7. [Database](#database)
8. [Authentication](#authentication)
9. [Configuration & Environment](#configuration--environment)

---

## Architecture

**Modular monolith** with Clean Architecture per module. All modules compile into a single process — no network communication between them.

```
┌─────────────────────────────────────────────────────┐
│  src/Api  (composition root)                        │
│  ┌──────────────────────────────────────────────┐   │
│  │  HTTP Pipeline (ASP.NET Core Minimal API)    │   │
│  │  → JWT Auth → Rate Limiting → Validation     │   │
│  │  → MediatR dispatch → Handler                │   │
│  └──────────────────────────────────────────────┘   │
│  ┌──────────┐ ┌──────────┐ ┌──────────────────┐    │
│  │Aircrafts │ │ Airports │ │    Scenarios      │    │
│  └──────────┘ └──────────┘ └──────────────────┘    │
│  ┌──────────┐ ┌──────────────────────────────────┐  │
│  │  Login   │ │           Solver                 │  │
│  └──────────┘ └──────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────┐   │
│  │  EF Core + PostgreSQL (AppDbContext)          │   │
│  └──────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

**Allowed cross-module dependencies** (all others are forbidden):
- `Scenarios` → `Aircrafts`, `Airports`
- `Solver` → `Scenarios`, `Airports`, `Aircrafts`

**CQRS pattern**: every use-case has a `<Name>Command/Query.cs` + `<Name>Handler.cs`. Handlers are registered in `Program.cs` from each module's assembly.

---

## Backend Modules

### Modules.Airports

Manages airports and runways.

**Domain:**
- `Airport` — id, name
- `Runway` — id, name, runwayType, isActive, airportId
- `RunwayType` enum: `Landing=0`, `Takeoff=1`, `Both=2`

**Use cases:**
- `CreateAirport` — create airport (validated: name required)
- `GetAirports` — list all
- `DeleteAirport` — delete with cascade (runways + flights)
- `CreateRunway` — add runway to airport
- `GetRunwaysByAirportId` — list runways per airport
- `UpdateRunway` — modify runway type/active state
- `DeleteRunway` — delete runway

### Modules.Aircrafts

Generates aircraft with wake turbulence category.

**Domain:**
- `Aircraft` — id, tailNumber, wakeCategory, scenarioConfigId
- `WakeTurbulenceCategory` enum: `Light`, `Medium`, `Heavy`, `Super`

**Use cases:**
- `GenerateRandomAircraft` — generate N aircraft with random tail numbers and categories
- `GetAircrafts` — list by scenario

### Modules.Login

**Domain:**
- `User` — id, username, passwordHash

**Use cases:**
- `Login` — validates credentials (BCrypt), issues JWT access token

### Modules.Scenarios

Manages everything that defines a simulation scenario.

**Domain:**
- `ScenarioConfig` — id, name, startTime, endTime, baseSeparationSeconds, airportId
- `Flight` — id, callsign, type, scheduledTime, maxDelayMinutes, maxEarlyMinutes, priority, aircraftId, scenarioConfigId
- `FlightType` enum: `Arrival=0`, `Departure=1`, `OnGround=2`
- `WeatherInterval` — id, startTime, endTime, severity, scenarioConfigId
- `WeatherCondition` enum: `Clear=0`, `Light=1`, `Moderate=2`, `Heavy=3`, `Severe=4`, `Storm=5`
- `RandomEvent` — id, startTime, endTime, affectedRunwayId, description, scenarioConfigId

**Use cases:**
- `CreateScenarioConfig` — create configuration
- `GetScenarioConfigs` — list all
- `GetAllDataScenarioConfig` — returns full scenario in one response (config + flights + weather + events + aircraft)
- `DeleteScenario` — delete with all associated data
- `CreateFlights` — generate random flights within the scenario time window
- `GetFlights` — list flights per scenario
- `CreateWeatherIntervals` — generate random weather intervals
- `GetWeatherIntervals` — list
- `CreateRandomEvent` / `UpdateRandomEvent` / `DeleteRandomEvent` / `GetRandomEventsByScenarioConfigId`

**FlightScheduler** (internal service): distributes flights evenly across the scenario window, respecting minimum separation and arrival/departure ratios.

### Modules.Solver

Contains all optimization logic.

**Key components:**
- `IScenarioSnapshotFactory` → `ScenarioSnapshotFactory` — assembles all scenario data into an immutable `ScenarioSnapshot`
- `ScenarioSnapshot` — complete data: config, airport, runways, flights, weatherIntervals, randomEvents
- `PreparedScenario` — processed snapshot: sorted flights, runways indexed by type, sorted weather/events
- `ISchedulingEngine` → `SchedulingEngine` — evaluates a flight permutation and produces a `SchedulingEvaluation`
- `SchedulingEvaluation` — list of `SolvedFlight` + total fitness
- `SolverResult` — final result: fitness, counts, algorithmName, solveTimeMs, SolvedFlight list

**Use cases:**
- `SolveGreedy` — greedy solver
- `SolveGenetic` — hybrid GA + CP-SAT solver
- `Compare` — runs both solvers and returns results side-by-side
- `SolveFromPayload` — accepts full scenario as JSON body, runs both solvers
- `GaBenchmark` — runs GA with multiple parameter configurations, saves results
- `GetBenchmarkEntries` — list saved benchmark results

---

## Data Models

### SolvedFlight

```
FlightId           Guid
ScenarioConfigId   Guid
Callsign           string
Type               FlightType
Priority           int
ScheduledTime      DateTime
AssignedTime       DateTime?
DelayMinutes       double
EarlyMinutes       double
Status             FlightStatus
CancellationReason CancellationReason
```

**FlightStatus**: `Scheduled`, `Early`, `Delayed`, `Canceled`, `Rescheduled`

**CancellationReason**: `None`, `NoCompatibleRunway`, `OutsideScenarioWindow`, `ExceedsMaxDelay`

### SolverResult

```
AlgorithmName   string
Fitness         double         (total penalty — lower is better)
TotalFlights    int
TotalScheduled  int
TotalCanceled   int
TotalDelayed    int
SolveTimeMs     double
Flights         IReadOnlyList<SolvedFlight>
```

### BenchmarkEntry

Records a GA run with its parameters and results. Persisted to DB.

---

## Solver Algorithms

### Greedy

1. Sort flights by `ScheduledTime`
2. For each flight, find the first compatible runway:
   - Runway type matches flight type (Arrival → Landing/Both, Departure → Takeoff/Both)
   - Runway is active and has no active random event at the assigned time
   - Respects minimum separation from the last flight on that runway (`BaseSeparationSeconds`)
   - Respects weather constraints (higher severity = additional separation)
3. If no runway is available within `[scheduledTime - maxEarly, scheduledTime + maxDelay]`: flight is canceled
4. Fitness = sum of all penalties (see formula below)

### Genetic Algorithm + CP-SAT

**Encoding**: permutation of indices — `chromosome[i]` = index of the flight processed at position `i`. The SchedulingEngine evaluates the permutation as a candidate solution.

**Initialization** (PopulationSize chromosomes):
- 50%: natural order (sorted by scheduledTime) with ~10% adjacent swaps
- 50%: full Fisher-Yates shuffle

**Selection**: Tournament selection (TournamentSize random candidates, winner has lowest fitness)

**OX1 Crossover** (Order Crossover): applied with probability `CrossoverRate`
- Copies a segment from parent1, fills remaining slots in order from parent2

**Mutation type A — Local Window-Aware Swap**:
- For each position: with probability `MutationRateLocal`, swap with another position in the same time window `[scheduledTime - maxEarly, scheduledTime + maxDelay]`

**Mutation type B — Memetic Destroy-and-Repair**:
- Applied with probability `MutationRateMemetic`
- Selects the time window with the highest penalty (roulette wheel)
- Identifies flights with the highest cost (canceled + delayed)
- Reinserts those flights earlier in the permutation (processed first by greedy, more likely to be scheduled)
- Only applies the mutation if it improves fitness

**CP-SAT Window Refiner**:
- Each generation, elite chromosomes pass through `CpSatWindowRefiner`
- Splits the scenario into time windows (`TimeWindowSize`)
- Optimizes each window locally using Google OR-Tools CP-SAT
- Bounded by `CpSatTimeLimitMsMicro` (per small window) and `CpSatTimeLimitMsMacro` (per large window)
- Refinement adjusts the relative order of flights within the window

**Elitism**: `EliteCount` chromosomes with lowest fitness survive unmodified

**Stopping criteria**: after `MaxGenerations` generations or `NoImprovementGenerations` generations without improvement

### Fitness Formula (penalty)

```
priority_multiplier = 1.2 ^ (priority - 1)

penalty(flight) =
  if Canceled:  180 × priority_multiplier
  if Delayed:   delayMinutes × priority_multiplier
  if Early:     earlyMinutes × 0.5 × priority_multiplier

Fitness = Σ penalty(flight) for all flights
```

Lower is better. A cancellation is treated as ~180 minutes of delay at the same priority.

### GA Parameters (GaConfig)

| Parameter | Default | Description |
|-----------|---------|-------------|
| `PopulationSize` | 80 | Chromosomes per generation |
| `MaxGenerations` | 200 | Maximum generations |
| `CrossoverRate` | 0.85 | OX crossover probability |
| `MutationRateLocal` | 0.15 | Per-gene swap mutation probability |
| `MutationRateMemetic` | 0.20 | Destroy-and-repair mutation probability per chromosome |
| `TournamentSize` | 3 | Candidates per tournament selection |
| `EliteCount` | 2 | Elite chromosomes preserved unchanged |
| `NoImprovementGenerations` | 40 | Early stop if no progress |
| `CpSatTimeLimitMsMicro` | 60 | CP-SAT time limit per small window (ms) |
| `CpSatTimeLimitMsMacro` | 150 | CP-SAT time limit per large window (ms) |
| `CpSatNeighborhoodSize` | 8 | Flights considered per CP-SAT window |
| `RandomSeed` | 42 | Seed for reproducibility |

---

## Full API Reference

All endpoints require `Authorization: Bearer <token>` except `/login`.

### POST /login
```json
Request:  { "username": "string", "password": "string" }
Response: { "token": "string" }
```

### POST /airport
```json
Request:  { "name": "Henri Coanda" }
Response: { "id": "guid", "name": "string" }
```

### GET /airports
```json
Response: [{ "id": "guid", "name": "string" }]
```

### DELETE /airports/{id}
`204 No Content` or `404`

### POST /airports/{id}/runways
```json
Request:  { "name": "08L", "runwayType": 0, "isActive": true }
Response: { "id": "guid", "name": "string", "runwayType": 0, "isActive": true }
```

### GET /airports/{id}/runways
```json
Response: [{ "id": "guid", "name": "string", "runwayType": 0, "isActive": true }]
```

### PUT /runways/{id}
```json
Request:  { "name": "08L", "runwayType": 2, "isActive": false }
Response: 204 No Content
```

### DELETE /runways/{id}
`204 No Content` or `404`

### POST /scenarios/configs
```json
Request: {
  "name": "Test Scenario",
  "startTime": "2026-06-15T06:00:00Z",
  "endTime": "2026-06-15T14:00:00Z",
  "baseSeparationSeconds": 45,
  "airportId": "guid"
}
```

### GET /scenarios/configs
```json
Response: [{ "id": "guid", "name": "string", "startTime": "...", "endTime": "...", "airportId": "guid" }]
```

### GET /scenarios/configs/{id}
Returns complete scenario data: config + flights + weatherIntervals + randomEvents + aircraft.

### DELETE /scenarios/configs/{id}
`204 No Content` or `404`

### POST /flights/generate/{scenarioConfigId}
Generates random flights within the scenario window. No request body.

### GET /flights/{scenarioConfigId}
```json
Response: [{ "id": "guid", "callsign": "string", "type": 0, "scheduledTime": "...", "maxDelayMinutes": 20, "maxEarlyMinutes": 0, "priority": 1 }]
```

### POST /weatherintervals/generate/{scenarioConfigId}
Generates random weather intervals. No request body.

### GET /weatherintervals/{scenarioConfigId}

### POST /aircrafts/generate/{scenarioConfigId}
```json
Request:  { "count": 10 }
```

### GET /aircrafts/{scenarioConfigId}

### POST /scenarios/{scenarioConfigId}/random-events
```json
Request: {
  "description": "Runway incident",
  "startTime": "2026-06-15T08:00:00Z",
  "endTime": "2026-06-15T09:00:00Z",
  "affectedRunwayId": "guid"
}
```

### GET /random-events/{scenarioConfigId}

### PUT /random-events/{id}

### DELETE /random-events/{id}

### GET /greedy/{scenarioConfigId}
Returns `SolverResult` with `algorithmName: "Greedy"`.

### GET /genetic/{scenarioConfigId}
Returns `SolverResult` with `algorithmName: "Genetic Algorithm"`.

### GET /compare/{scenarioConfigId}
```json
Response: {
  "greedy":  { ...SolverResult },
  "genetic": { ...SolverResult }
}
```

### POST /solver/solve-from-payload
Runs both solvers on a scenario defined entirely in the request body. No DB required.
```json
Request: {
  "scenarioConfig": { "name": "...", "startTime": "...", "endTime": "...", "baseSeparationSeconds": 45 },
  "runways": [{ "name": "08L", "runwayType": 0, "isActive": true }],
  "flights": [{ "callsign": "ROT001", "type": 0, "scheduledTime": "...", "maxDelayMinutes": 20, "maxEarlyMinutes": 0, "priority": 1 }],
  "weatherIntervals": [],
  "randomEvents": []
}
Response: {
  "greedy":  { ...SolverResult },
  "genetic": { ...SolverResult }
}
```

### POST /solver/benchmark
Runs GA with multiple parameter sets on a scenario and saves results.
```json
Request: {
  "scenarioConfigId": "guid",
  "configs": [
    { "populationSize": 80, "maxGenerations": 200, "crossoverRate": 0.85, ... }
  ]
}
```

### GET /solver/benchmarks
Returns all saved benchmark runs from the DB.

---

## Frontend

React 19 + TypeScript + Vite SPA. Vite proxy in dev routes `/api` to `localhost:5000`.

### Pages
- **HomePage** — main dashboard, Compare button
- **AirportsPage** — CRUD airports and runways
- **ScenarioConfigPage** — create/delete scenarios, generate data (flights, weather, aircraft, events)
- **SolverPage** — run solvers, Import JSON, visualize comparative results
- **ContactPage** — contact information

### Hooks
- `useAirports` — airports + runways CRUD with cache invalidation
- `useScenarios` — scenarios + all sub-resources CRUD
- `useSolver` — greedy, genetic, compare, solve-from-payload
- `useAuthSession` — login, logout, token storage

### Reusable Components
`Modal`, `ConfirmDialog`, `Toast`, `Skeleton`, `NumberInput`, `SearchBar`

---

## Database

PostgreSQL. Visual schema: `docs/DB.png`.

**Main tables:**
- `airports`, `runways`
- `scenario_configs`, `flights`, `weather_intervals`, `random_events`
- `aircrafts`
- `users`
- `benchmark_entries`

EF Core migrations are in `src/Api/Data/Migrations/` (current) and `src/Api/DataBase/Migrations/` (legacy, kept for compatibility).

Migrations are **applied automatically on startup** in `Program.cs`.

**Reset migrations (dev):**
```bash
cd src/Api
dotnet ef database drop --force
dotnet ef migrations remove   # repeat until empty
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Authentication

- JWT Bearer tokens issued by `JwtTokenService`
- Config keys: `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`
- Users are seeded on first migration
- Passwords are BCrypt hashed

---

## Configuration & Environment

### Environment Variables (Docker)

Copy from `src/.env.example`:

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=...
POSTGRES_DB=RunwayScheduling
JWT__KEY=...
JWT__ISSUER=RunwayScheduling
JWT__AUDIENCE=RunwayScheduling
```

### Local Development

`src/Api/appsettings.Development.json` — overrides connection string and JWT for local dev.

Default ports:
- Backend dev: `http://localhost:5000`
- Frontend dev: `http://localhost:5173`
- Docker API: `:5186`, Frontend: `:3000`, DB: `:5433`

### CI/CD

- **ci.yml** — triggered on push/PR to `main`/`develop`:
  1. `dotnet build` + `dotnet test`
  2. `npm run lint` + `npm run build`
  3. `docker build` smoke test

- **cd.yml** — manual trigger:
  1. Build Docker images
  2. Push to GitHub Container Registry (`ghcr.io`)
