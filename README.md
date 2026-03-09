# RunwayScheduling

RunwayScheduling is a **modular monolith backend system** for simulating **airport runway operations, aircraft generation, and flight scheduling**.

The project is designed for **simulation, research, and academic use**, with realistic constraints inspired by real-world **Air Traffic Control (ATC)** concepts.

---

# Tech Stack

## Backend
- .NET 10
- ASP.NET Core Minimal API
- Clean Architecture
- CQRS + MediatR
- Entity Framework Core
- PostgreSQL
- Docker

## Frontend
- React
- Vite

## Tooling
- Docker Compose
- DBeaver / pgAdmin
- dotnet format
- EditorConfig
- GitHub Actions (CI)

---

# Architecture

The project follows a **Modular Monolith architecture**.

Characteristics:

- Single deployable unit
- Strict module boundaries
- Domain isolation between modules
- Infrastructure shared only when necessary

This allows the system to stay **simple to deploy** while still keeping **clean domain separation**.

---

# Project Structure

```
src/
├── Api
│   ├── Minimal API
│   ├── Composition Root
│   ├── EF Core DbContext
│   ├── Infrastructure Stores
│   └── Endpoints
│
├── Modules.Airports
│   ├── Domain
│   │   ├── Airport
│   │   └── Runway
│   └── Application
│       ├── CreateAirport
│       ├── CreateRunway
│       ├── UpdateRunway
│       ├── DeleteRunway
│       └── Queries
│
├── Modules.Aircrafts
│   ├── Domain
│   │   ├── Aircraft
│   │   └── WakeTurbulenceCategory
│   └── Application
│       ├── Generators
│       └── Queries
│
├── Modules.Scenarios
│   ├── Domain
│   │   ├── ScenarioConfig
│   │   ├── Flight
│   │   └── WeatherInterval
│   └── Application
│       ├── CreateScenarioConfig
│       ├── CreateFlights
│       ├── DeleteScenario
│       └── Queries
│
└── frontend
    └── Vite + React
```

The **frontend is completely decoupled** from backend domain logic.

---

# High-Level Flow

Typical usage flow:

1. Create an **Airport**
2. Create **Runways** for that airport
3. Create a **ScenarioConfig**
   - difficulty
   - time window
   - aircraft count
   - simulation seed
4. Generate **Aircraft**
5. Generate **Flights**
   - callsign
   - priority
   - early / delay tolerance
   - wake turbulence realism
6. Persist data to **PostgreSQL**
7. Query scenario data

---

# Database (Docker)

PostgreSQL runs inside a Docker container.

Example container:

```
runway_db
0.0.0.0:5433 -> 5432
```

Host connection:

```
Host: localhost
Port: 5433
Database: RunwayScheduling
Schema: public
```

Core tables:

- airports
- runways
- scenario_configs
- aircrafts
- flights
- weather_intervals

Relationships:

- Airport → Runways (CASCADE)
- ScenarioConfig → Aircrafts (CASCADE)
- ScenarioConfig → Flights (CASCADE)
- ScenarioConfig → WeatherIntervals (CASCADE)

---

# Authentication (Planned)

Authentication will be implemented using **JWT + Refresh Tokens**.

Planned features:

- User registration
- User login
- JWT access token generation
- Refresh token rotation
- Secure logout
- Protected endpoints
- Future role support

## Planned Auth Flow

1. User sends credentials to:

```
POST /auth/login
```

2. Backend validates credentials.

3. Backend returns:

```
access_token
refresh_token
```

4. Frontend uses the access token for authenticated requests.

5. When the access token expires:

```
POST /auth/refresh
```

is used to obtain a new one.

## Planned Auth Endpoints

```
POST /auth/register
POST /auth/login
POST /auth/refresh
POST /auth/logout
```

Authentication data will be stored in a **Users table**, with **hashed passwords and refresh token management**.

---

# CI / CD

The project currently includes **Continuous Integration (CI)**.

CI pipeline checks:

- backend build
- frontend build
- frontend lint
- Docker build validation

Future **Continuous Deployment (CD)** will:

- deploy containers to cloud / VPS
- rebuild Docker services automatically
- manage environment variables
- restart services safely

---

# Design Goals

- Realistic ATC-inspired constraints
- Clear domain boundaries
- Deterministic and reproducible simulations
- Modular and extensible architecture
- Clean academic-grade codebase
- Future support for optimization algorithms

---

# Current Status

Current progress:

- Core domain modules implemented
- Scenario configuration system implemented
- Dockerized PostgreSQL environment
- Frontend connected to backend
- CI pipeline integrated
- Authentication system planned
- Flight generation logic in progress
- Weather and conflict resolution planned

---

# Notes

This is primarily a **backend-focused system**.

The frontend is intentionally **decoupled** so that:

- multiple frontends could exist
- simulation logic remains backend-driven

The project is intended for **academic use, research experiments, and simulation environments**.