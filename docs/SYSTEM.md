# RunwayScheduling — Documentație tehnică

## Cuprins

1. [Arhitectură](#arhitectură)
2. [Module backend](#module-backend)
3. [Modele de date](#modele-de-date)
4. [Algoritmi solver](#algoritmi-solver)
5. [API complet](#api-complet)
6. [Frontend](#frontend)
7. [Baza de date](#baza-de-date)
8. [Autentificare](#autentificare)
9. [Configurare și mediu](#configurare-și-mediu)

---

## Arhitectură

**Monolith modular** cu Clean Architecture per modul. Toate modulele compilează într-un singur proces; nu există comunicare prin rețea între ele.

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

**Dependențe între module** (doar acestea sunt permise):
- `Scenarios` → `Aircrafts`, `Airports`
- `Solver` → `Scenarios`, `Airports`, `Aircrafts`
- Orice alt cross-module reference este interzis

**Pattern CQRS**: fiecare use-case are `<Name>Command/Query.cs` + `<Name>Handler.cs`. Handlere înregistrate în `Program.cs` din assembly-ul fiecărui modul.

---

## Module backend

### Modules.Airports

Gestionează aeroporturi și piste.

**Domain:**
- `Airport` — id, name
- `Runway` — id, name, runwayType, isActive, airportId
- `RunwayType` enum: `Landing=0`, `Takeoff=1`, `Both=2`

**Use cases:**
- `CreateAirport` — creare aeroport (validat: name nenul)
- `GetAirports` — listare toate
- `DeleteAirport` — ștergere cu cascade (piste + zboruri)
- `CreateRunway` — adăugare pistă la aeroport
- `GetRunwaysByAirportId` — listare piste per aeroport
- `UpdateRunway` — modificare tip/stare pistă
- `DeleteRunway` — ștergere pistă

### Modules.Aircrafts

Generează aeronave cu categorie de turbulență de sillaj.

**Domain:**
- `Aircraft` — id, tailNumber, wakeCategory, scenarioConfigId
- `WakeTurbulenceCategory` enum: `Light`, `Medium`, `Heavy`, `Super`

**Use cases:**
- `GenerateRandomAircraft` — generează N aeronave cu tail number și categorie random
- `GetAircrafts` — listare per scenariu

### Modules.Login

**Domain:**
- `User` — id, username, passwordHash

**Use cases:**
- `Login` — validează credențiale (BCrypt), emite JWT access token

### Modules.Scenarios

Gestionează tot ce definește un scenariu de simulare.

**Domain:**
- `ScenarioConfig` — id, name, startTime, endTime, baseSeparationSeconds, airportId
- `Flight` — id, callsign, type, scheduledTime, maxDelayMinutes, maxEarlyMinutes, priority, aircraftId, scenarioConfigId
- `FlightType` enum: `Arrival=0`, `Departure=1`, `OnGround=2`
- `WeatherInterval` — id, startTime, endTime, severity, scenarioConfigId
- `WeatherCondition` enum: `Clear=0`, `Light=1`, `Moderate=2`, `Heavy=3`, `Severe=4`, `Storm=5`
- `RandomEvent` — id, startTime, endTime, affectedRunwayId, description, scenarioConfigId

**Use cases:**
- `CreateScenarioConfig` — creare configurație
- `GetScenarioConfigs` — listare
- `GetAllDataScenarioConfig` — tot scenariul într-un singur response (config + zboruri + meteo + events + aeronave)
- `DeleteScenario` — ștergere cu tot ce ține de el
- `CreateFlights` — generare zboruri random în fereastra scenariului
- `GetFlights` — listare zboruri per scenariu
- `CreateWeatherIntervals` — generare intervale meteo random
- `GetWeatherIntervals` — listare
- `CreateRandomEvent` / `UpdateRandomEvent` / `DeleteRandomEvent` / `GetRandomEventsByScenarioConfigId`

**FlightScheduler** (service intern): distribuie zborurile uniform în fereastra scenariului, respectând separarea minimă și proporțiile arrival/departure.

### Modules.Solver

Conține logica de optimizare.

**Componente cheie:**
- `IScenarioSnapshotFactory` → `ScenarioSnapshotFactory` — asamblează toate datele scenariului într-un `ScenarioSnapshot` imutabil
- `ScenarioSnapshot` — toate datele: config, airport, runways, flights, weatherIntervals, randomEvents
- `PreparedScenario` — snapshot procesat: zboruri sortate, piste indexate pe tip, weather/events sortate
- `ISchedulingEngine` → `SchedulingEngine` — evaluează o permutare de zboruri și produce `SchedulingEvaluation`
- `SchedulingEvaluation` — lista de `SolvedFlight` + fitness total
- `SolverResult` — rezultat final: fitness, counts, algorithmName, solveTimeMs, lista SolvedFlight

**Use cases:**
- `SolveGreedy` — solver greedy
- `SolveGenetic` — solver hibrid GA + CP-SAT
- `Compare` — rulează ambii solveri și returnează rezultatele side-by-side
- `SolveFromPayload` — acceptă tot scenariul ca JSON body, rulează ambii solveri
- `GaBenchmark` — rulează GA cu mai multe configurații de parametri, salvează rezultatele
- `GetBenchmarkEntries` — listare benchmark-uri salvate

---

## Modele de date

### SolvedFlight

```
FlightId         Guid
ScenarioConfigId Guid
Callsign         string
Type             FlightType
Priority         int
ScheduledTime    DateTime
AssignedTime     DateTime?
DelayMinutes     double
EarlyMinutes     double
Status           FlightStatus
CancellationReason CancellationReason
```

**FlightStatus**: `Scheduled`, `Early`, `Delayed`, `Canceled`, `Rescheduled`

**CancellationReason**: `None`, `NoCompatibleRunway`, `OutsideScenarioWindow`, `ExceedsMaxDelay`

### SolverResult

```
AlgorithmName    string
Fitness          double        (penalitate totală, mai mic = mai bun)
TotalFlights     int
TotalScheduled   int
TotalCanceled    int
TotalDelayed     int
SolveTimeMs      double
Flights          IReadOnlyList<SolvedFlight>
```

### BenchmarkEntry

Înregistrează un rulaj GA cu parametrii și rezultatele sale. Persiste în DB.

---

## Algoritmi solver

### Greedy

1. Sortează zborurile după `ScheduledTime`
2. Pentru fiecare zbor, găsește prima pistă compatibilă:
   - Tip pistă compatibil cu tipul zborului (Arrival→Landing/Both, Departure→Takeoff/Both)
   - Pistă activă și fără eveniment aleatoriu activ în momentul respectiv
   - Respectă separarea minimă față de ultimul zbor pe pistă (`BaseSeparationSeconds`)
   - Respectă constrângerile meteo (severitate mare = separare suplimentară)
3. Dacă nicio pistă nu e disponibilă în `[scheduledTime - maxEarly, scheduledTime + maxDelay]`: zborul e anulat
4. Fitness = suma penalităților (vezi formula mai jos)

### Genetic Algorithm + CP-SAT

**Codificare**: permutare de indici — `chromosome[i]` = indexul zborului care se procesează pe poziția `i`. SchedulingEngine evaluează permutarea = soluție candidat.

**Inițializare** (PopulationSize cromozomi):
- 50%: ordine naturală (sortată după scheduledTime) cu ~10% swap-uri adiacente
- 50%: shuffle complet Fisher-Yates

**Selecție**: Tournament selection (TournamentSize candidați aleatori, câștigă cel cu fitness minim)

**Crossover OX1** (Order Crossover): se aplică cu probabilitate CrossoverRate
- Copiază un segment din parent1, completează din parent2 în ordinea apariției

**Mutație tip A — Local Window-Aware Swap**:
- Pentru fiecare poziție: cu probabilitate `MutationRateLocal`, swap cu o poziție din aceeași fereastră de timp `[scheduledTime - maxEarly, scheduledTime + maxDelay]`

**Mutație tip B — Memetic Destroy-and-Repair**:
- Cu probabilitate `MutationRateMemetic`
- Selectează fereastra cu cea mai mare penalitate (roulette wheel)
- Identifică zborurile cu cel mai mare cost (anulate + întârziate)
- Reintroduce aceste zboruri mai devreme în permutare (să fie procesate primele)
- Aplică mutația doar dacă îmbunătățește fitness-ul

**CP-SAT Window Refiner**:
- La fiecare generație, elitele trec prin `CpSatWindowRefiner`
- Împarte scenariul în ferestre de timp (`TimeWindowSize`)
- Pentru fiecare fereastră, optimizează local cu Google OR-Tools CP-SAT
- Limitat de `CpSatTimeLimitMsMicro` (per fereastră mică) și `CpSatTimeLimitMsMacro` (per fereastră mare)
- Rafinamentul modifică ordinea relativă a zborurilor în fereastră

**Elitism**: `EliteCount` cromozomi cu fitness minim supraviețuiesc nemodificați

**Oprire**: după `MaxGenerations` generații sau `NoImprovementGenerations` generații fără îmbunătățire

### Formula fitness (penalitate)

```
priority_multiplier = 1.2 ^ (priority - 1)

penalty(flight) =
  dacă Canceled:   180 × priority_multiplier
  dacă Delayed:    delayMinutes × priority_multiplier
  dacă Early:      earlyMinutes × 0.5 × priority_multiplier

Fitness = Σ penalty(flight) pentru toate zborurile
```

Lower = better. O anulare echivalează cu ~180 minute de întârziere.

### Parametri GA (GaConfig)

| Parametru | Default | Descriere |
|-----------|---------|-----------|
| `PopulationSize` | 80 | Număr cromozomi per generație |
| `MaxGenerations` | 200 | Generații maxime |
| `CrossoverRate` | 0.85 | Probabilitate crossover OX |
| `MutationRateLocal` | 0.15 | Probabilitate mutație swap per genă |
| `MutationRateMemetic` | 0.20 | Probabilitate mutație destroy-and-repair per cromozom |
| `TournamentSize` | 3 | Candidați per selecție tournament |
| `EliteCount` | 2 | Cromozomi elită păstrați nemodificați |
| `NoImprovementGenerations` | 40 | Oprire early dacă nu există progres |
| `CpSatTimeLimitMsMicro` | 60 | Time limit CP-SAT per fereastră mică (ms) |
| `CpSatTimeLimitMsMacro` | 150 | Time limit CP-SAT per fereastră mare (ms) |
| `CpSatNeighborhoodSize` | 8 | Zboruri considerate per fereastră CP-SAT |
| `RandomSeed` | 42 | Seed pentru reproducibilitate |

---

## API complet

Toate endpoint-urile necesită `Authorization: Bearer <token>`, cu excepția `/login`.

### POST /login
```json
Request:  { "username": "string", "password": "string" }
Response: { "token": "string" }
```

### POST /airport
```json
Request:  { "name": "Henri Coandă" }
Response: { "id": "guid", "name": "string" }
```

### GET /airports
```json
Response: [{ "id": "guid", "name": "string" }]
```

### DELETE /airports/{id}
`204 No Content` sau `404`

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
`204 No Content` sau `404`

### POST /scenarios/configs
```json
Request: {
  "name": "Scenariu test",
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
Returnează toate datele scenariului: config + flights + weatherIntervals + randomEvents + aircrafts.

### DELETE /scenarios/configs/{id}
`204 No Content` sau `404`

### POST /flights/generate/{scenarioConfigId}
Generează zboruri random în fereastra scenariului. Nu are body.

### GET /flights/{scenarioConfigId}
```json
Response: [{ "id": "guid", "callsign": "string", "type": 0, "scheduledTime": "...", ... }]
```

### POST /weatherintervals/generate/{scenarioConfigId}
Generează intervale meteo random. Nu are body.

### GET /weatherintervals/{scenarioConfigId}

### POST /aircrafts/generate/{scenarioConfigId}
```json
Request:  { "count": 10 }
```

### GET /aircrafts/{scenarioConfigId}

### POST /scenarios/{scenarioConfigId}/random-events
```json
Request: {
  "description": "Incident pistă",
  "startTime": "2026-06-15T08:00:00Z",
  "endTime": "2026-06-15T09:00:00Z",
  "affectedRunwayId": "guid"
}
```

### GET /random-events/{scenarioConfigId}

### PUT /random-events/{id}

### DELETE /random-events/{id}

### GET /greedy/{scenarioConfigId}
Returnează `SolverResult` cu `algorithmName: "Greedy"`.

### GET /genetic/{scenarioConfigId}
Returnează `SolverResult` cu `algorithmName: "Genetic Algorithm"`.

### GET /compare/{scenarioConfigId}
```json
Response: {
  "greedy": { ...SolverResult },
  "genetic": { ...SolverResult }
}
```

### POST /solver/solve-from-payload
Rulează ambii solveri pe un scenariu definit complet în body. Nu necesită date în DB.
```json
Request: {
  "scenarioConfig": { "name": "...", "startTime": "...", "endTime": "...", "baseSeparationSeconds": 45 },
  "runways": [{ "name": "08L", "runwayType": 0, "isActive": true }],
  "flights": [{ "callsign": "ROT001", "type": 0, "scheduledTime": "...", "maxDelayMinutes": 20, "maxEarlyMinutes": 0, "priority": 1 }],
  "weatherIntervals": [],
  "randomEvents": []
}
Response: {
  "greedy": { ...SolverResult },
  "genetic": { ...SolverResult }
}
```

### POST /solver/benchmark
Rulează GA cu mai multe seturi de parametri pe un scenariu și salvează rezultatele.
```json
Request: {
  "scenarioConfigId": "guid",
  "configs": [
    { "populationSize": 80, "maxGenerations": 200, ... }
  ]
}
```

### GET /solver/benchmarks
Returnează toate rulajele de benchmark salvate în DB.

---

## Frontend

SPA React 19 + TypeScript + Vite. Proxy Vite în dev → `/api` la `localhost:5000`.

### Pagini
- **HomePage** — dashboard principal, buton Compare
- **AirportsPage** — CRUD aeroporturi și piste
- **ScenarioConfigPage** — creare/ștergere scenarii, generare date (zboruri, meteo, aeronave, events)
- **SolverPage** — rulare solveri, Import JSON, vizualizare rezultate comparative
- **ContactPage** — informații contact

### Hooks
- `useAirports` — CRUD airports + runways cu invalidare cache
- `useScenarios` — CRUD scenarios + toate sub-resursele
- `useSolver` — greedy, genetic, compare, solve-from-payload
- `useAuthSession` — login, logout, token storage

### Componente refolosibile
`Modal`, `ConfirmDialog`, `Toast`, `Skeleton`, `NumberInput`, `SearchBar`

---

## Baza de date

PostgreSQL. Schema vizuală: `docs/DB.png`.

**Tabele principale:**
- `airports`, `runways`
- `scenario_configs`, `flights`, `weather_intervals`, `random_events`
- `aircrafts`
- `users`
- `benchmark_entries`

Migrări EF Core în `src/Api/Data/Migrations/` (set curent) și `src/Api/DataBase/Migrations/` (legacy, păstrate pentru compatibilitate).

Migrările se aplică **automat la startup** în `Program.cs`.

**Reset migrații (dev):**
```bash
cd src/Api
dotnet ef database drop --force
dotnet ef migrations remove  # repetă până golești
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Autentificare

- JWT Bearer tokens, emise de `JwtTokenService`
- Chei de configurare: `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`
- Userii sunt seed-uiți la prima migrare
- Parolele sunt hash-uite BCrypt

---

## Configurare și mediu

### Variabile de mediu (Docker)

Copiat din `src/.env.example`:

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=...
POSTGRES_DB=RunwayScheduling
JWT__KEY=...
JWT__ISSUER=RunwayScheduling
JWT__AUDIENCE=RunwayScheduling
```

### Dev local

`src/Api/appsettings.Development.json` — suprascrie connection string și JWT pentru dev.

Port-uri implicite:
- Backend dev: `http://localhost:5000`
- Frontend dev: `http://localhost:5173`
- Docker API: `:5186`, Frontend: `:3000`, DB: `:5433`

### CI/CD

- **ci.yml** — declanșat la push/PR pe `main`/`develop`:
  1. `dotnet build` + `dotnet test`
  2. `npm run lint` + `npm run build`
  3. `docker build` (smoke test)

- **cd.yml** — trigger manual:
  1. Build imagini Docker
  2. Push pe GitHub Container Registry (`ghcr.io`)
