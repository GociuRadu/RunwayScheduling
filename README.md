#  RunwayScheduling

RunwayScheduling is a **modular-monolith backend system** for simulating **airport runway operations, aircraft generation, and flight scheduling**.

The project is built with:
- Clean Architecture
- CQRS + MediatR
- Entity Framework Core
- PostgreSQL (Docker)
- .NET 10

This project is intended for **simulation, research, and academic use**, with realistic constraints inspired by real ATC concepts.

---

## Architecture

The solution follows a **Modular Monolith** approach:
- One deployment
- Strict module boundaries
- No cross-module data leaks

Project structure:

src/
├── Api
│ ├── Minimal API (composition root)
│ ├── EF Core DbContext
│ ├── Infrastructure stores (EF implementations)
│ └── Endpoints
│
├── Modules.Airports
│ ├── Domain
│ │ ├── Airport
│ │ └── Runway
│ └── Application
│ ├── CreateAirport
│ ├── CreateRunway
│ ├── UpdateRunway
│ ├── DeleteRunway
│ └── Queries
│
├── Modules.Aircrafts
│ ├── Domain
│ │ ├── Aircraft
│ │ └── WakeTurbulenceCategory
│ └── Application
│ ├── Generators
│ └── Queries
│
├── Modules.Scenarios
│ ├── Domain
│ │ ├── ScenarioConfig
│ │ ├── Flight
│ │ └── WeatherInterval
│ └── Application
│ ├── CreateScenarioConfig
│ ├── CreateFlights
│ ├── DeleteScenario
│ └── Queries
│
└── frontend
└── Vite + React (fully decoupled)


---

## 🔁 High-Level Flow

1. Create an **Airport**
2. Create **Runways** for the airport
3. Create a **ScenarioConfig**
   - difficulty
   - time window
   - aircraft count
4. Generate **Aircrafts**
5. Generate **Flights**
   - callsign
   - priority
   - delay / early tolerance
   - wake turbulence realism
6. Persist everything in PostgreSQL
7. Query data by scenario or airport

---

## ⚙️ Tech Stack

### Backend
- .NET 10
- ASP.NET Core Minimal API
- MediatR (CQRS)
- Entity Framework Core
- PostgreSQL
- Docker

### Frontend
- Vite
- React

### Tooling
- DBeaver / pgAdmin (database inspection)
- dotnet format
- EditorConfig

---

## 🐳 Database (Docker)

PostgreSQL runs inside a Docker container.

Example running container:

0.0.0.0:5433 -> 5432 runway_db


Connection from host:
- Host: `localhost`
- Port: `5433`
- Schema: `public`

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

## 🎯 Design Goals

- Realistic ATC-inspired constraints
- Clear domain boundaries
- Deterministic + reproducible simulations (seeded)
- Easy extensibility (weather, conflicts, optimization algorithms)
- Academic-grade codebase

---

## 🚧 Current Status

- Core domains implemented
- Flight generation logic in progress
- Weather & conflict resolution planned
- Frontend integration pending

---

## 📌 Notes

This is a **backend-first system**.  
The frontend is optional and completely decoupled from backend logic.