# RunwayScheduling

**RunwayScheduling** is a modular monolith system for simulating airport runway operations and optimizing flight scheduling using configurable algorithms.

Designed for **simulation, research, and academic use**, with realistic constraints inspired by real-world Air Traffic Control (ATC) concepts.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 10 · ASP.NET Core Minimal API |
| Architecture | Modular Monolith · Clean Architecture · CQRS + MediatR |
| Database | PostgreSQL 17 · Entity Framework Core |
| Frontend | React 19 · Vite · TypeScript |
| Auth | JWT Bearer tokens · BCrypt |
| Infra | Docker · Docker Compose |
| CI/CD | GitHub Actions → GHCR |
| Testing | xUnit · NSubstitute · Coverlet (~59% coverage) |

---

## Repository Structure

```
RunwayScheduling/
├── .github/
│   └── workflows/
│       ├── ci.yml          # Build, test, lint on push/PR
│       └── cd.yml          # Build & push Docker images to GHCR on main
│
├── docs/
│   └── IMPROVEMENTS.md     # Architecture notes & roadmap
│
├── scripts/
│   ├── coverage.bat        # Run tests + generate HTML coverage report
│   └── start-dev.bat       # Start local dev environment
│
├── src/
│   ├── Api/                # Composition root, endpoints, EF DbContext, auth
│   ├── Modules.Airports/   # Airport & runway domain
│   ├── Modules.Aircrafts/  # Aircraft domain + random generation
│   ├── Modules.Scenarios/  # Scenario config, flights, weather, random events
│   ├── Modules.Solver/     # Solver engine (Greedy; GA planned)
│   └── frontend/           # React SPA
│
├── tests/
│   └── RunwayScheduling.Tests/   # xUnit integration & unit tests
│
├── global.json             # Pins .NET SDK version
└── RunwayScheduling.slnx   # Solution file
```

---

## Module Overview

### `Modules.Airports`
Manages airports and their runways. Runways have a type (`Landing`, `Takeoff`, `Both`) and an active flag used by the solver.

### `Modules.Aircrafts`
Aircraft domain with wake turbulence categories. Supports random generation seeded for reproducibility.

### `Modules.Scenarios`
- **ScenarioConfig** — time window, difficulty, weather %, separation seconds, wake %, seed
- **Flights** — callsign, priority, type (Arrival / Departure / OnGround), delay tolerance
- **WeatherIntervals** — time-bounded weather conditions affecting separation
- **RandomEvents** — time-bounded disruptions with an impact multiplier

### `Modules.Solver`
Pluggable solver engine via `IScenarioSolver`. Current implementation: **Greedy** (priority + earliest-available-runway). Planned: **Genetic Algorithm**.

Separation formula:
```
separation = BaseSeparationSeconds × (WakePercent / 100)
           × weatherMultiplier
           × (1 + eventImpactPercent / 100)
```

---

## Usage Flow

```
1. Create Airport + Runways
2. Create ScenarioConfig (time window, difficulty, seed)
3. Generate Aircraft (seeded, random)
4. Generate Flights (callsign, priority, type, tolerance)
5. (Optional) Add Weather Intervals
6. (Optional) Add Random Events
7. Run Solver → get SolverResult with stats & per-flight detail
```

---

## Database

PostgreSQL runs in Docker on `localhost:5433`.

```
Host:     localhost
Port:     5433
Database: RunwayScheduling
```

**Tables:**

| Table | Description |
|-------|-------------|
| `airports` | Airport records |
| `runways` | Runways per airport (CASCADE on delete) |
| `scenario_configs` | Scenario parameters |
| `aircrafts` | Generated aircraft per scenario |
| `flights` | Generated flights per scenario |
| `weather_intervals` | Time-bounded weather per scenario |
| `random_events` | Time-bounded disruptions per scenario |
| `users` | Auth accounts (hashed passwords) |

**Cascade rules:** deleting a ScenarioConfig removes all aircrafts, flights, weather intervals, and random events.

---

## Authentication

JWT Bearer authentication is fully implemented.

| Endpoint | Description |
|----------|-------------|
| `POST /auth/register` | Create account |
| `POST /auth/login` | Returns JWT token |

Protected endpoints require `Authorization: Bearer <token>`.

---

## CI / CD

| Pipeline | Trigger | Jobs |
|----------|---------|------|
| CI | push / PR on `main`, `develop` | Backend build + test · Frontend lint + build · Docker compose build |
| CD | push to `main` | Build & push `runway-api` and `runway-frontend` images to GHCR |

Docker images are tagged with `latest` and `sha-<commit>`.

To generate a local coverage report:
```
scripts\coverage.bat
```

---

## Algorithms

| Algorithm | Status | Description |
|-----------|--------|-------------|
| Greedy | ✅ Implemented | Assigns flights in priority order to the earliest available compatible runway |
| Genetic Algorithm | 🔜 Planned | Population-based optimization for minimizing total delay and cancellations |

The solver is abstracted behind `IScenarioSolver` — new algorithms are plug-in additions with no changes to existing code.

---

## Design Goals

- Realistic ATC-inspired scheduling constraints
- Deterministic and reproducible simulations via seeded RNG
- Pluggable solver architecture for algorithm comparison
- Clean domain boundaries, no cross-module DB joins
- Academic-grade codebase suitable for algorithm research
