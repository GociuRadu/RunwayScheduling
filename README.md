# RunwayScheduling

Aplicație full-stack pentru simularea și optimizarea planificării zborurilor pe piste de aeroport. Compară un algoritm greedy cu un solver hibrid Genetic + CP-SAT pe scenarii configurabile.

## Stack

| Strat | Tehnologie |
|---|---|
| Backend | ASP.NET Core Minimal API (.NET 10), MediatR, EF Core, PostgreSQL |
| Frontend | React 19, TypeScript, Vite |
| Auth | JWT Bearer, BCrypt |
| Solver | Greedy + Genetic Algorithm + CP-SAT (Google OR-Tools) |
| Infra | Docker Compose, GitHub Actions |
| Teste | xUnit, Coverlet |

## Structura proiectului

```
src/
  Api/                   Composition root, pipeline HTTP, auth, migrari EF
  Modules.Aircrafts/     Generare si listare aeronave
  Modules.Airports/      Aeroporturi si piste
  Modules.Login/         Autentificare, emitere JWT
  Modules.Scenarios/     Configuratii, zboruri, vreme, evenimente aleatorii
  Modules.Solver/        Solvere greedy, genetic si compare
  frontend/              SPA React

tests/
  RunwayScheduling.Tests/

scripts/
  start-dev.bat          Porneste backend + frontend in paralel
  coverage.bat           Teste cu raport HTML de coverage
  generate-scenario.mjs  Genereaza scenario-500.json
```

## Cum rulezi local

### Rapid

```bash
scripts\start-dev.bat
```

### Manual

**Backend:**
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

### Docker Compose

```bash
cd src
docker compose up --build
```

Variabilele de mediu se configureaza in `src/.env` (copiat din `src/.env.example`).

## Autentificare

Toate endpoint-urile, cu exceptia `POST /login`, necesita token JWT:

```
Authorization: Bearer <token>
```

`POST /login` este rate-limited la 5 cereri/minut.

## Endpoint-uri

### Auth

| Metoda | Ruta | Descriere |
|--------|------|-----------|
| POST | `/login` | Obtine token JWT |

### Aeroporturi & Piste

| Metoda | Ruta | Descriere |
|--------|------|-----------|
| POST | `/airport` | Creare aeroport |
| GET | `/airports` | Listare aeroporturi |
| DELETE | `/airports/{id}` | Stergere aeroport |
| POST | `/airports/{id}/runways` | Adaugare pista |
| GET | `/airports/{id}/runways` | Listare piste |
| PUT | `/runways/{id}` | Actualizare pista |
| DELETE | `/runways/{id}` | Stergere pista |

### Scenarii

| Metoda | Ruta | Descriere |
|--------|------|-----------|
| POST | `/scenarios/configs` | Creare configuratie |
| GET | `/scenarios/configs` | Listare configuratii |
| GET | `/scenarios/configs/{id}` | Date complete scenariu |
| DELETE | `/scenarios/configs/{id}` | Stergere scenariu |
| POST | `/flights/generate/{id}` | Generare zboruri |
| GET | `/flights/{id}` | Listare zboruri |
| POST | `/weatherintervals/generate/{id}` | Generare intervale meteo |
| GET | `/weatherintervals/{id}` | Listare intervale meteo |
| POST | `/aircrafts/generate/{id}` | Generare aeronave |
| GET | `/aircrafts/{id}` | Listare aeronave |
| POST | `/scenarios/{id}/random-events` | Adaugare eveniment aleatoriu |
| GET | `/random-events/{id}` | Listare evenimente |
| PUT | `/random-events/{id}` | Actualizare eveniment |
| DELETE | `/random-events/{id}` | Stergere eveniment |

### Solver

| Metoda | Ruta | Descriere |
|--------|------|-----------|
| GET | `/greedy/{id}` | Planificare greedy |
| GET | `/genetic/{id}` | Planificare genetic + CP-SAT |
| GET | `/compare/{id}` | Comparatie greedy vs genetic |
| POST | `/solver/solve-from-payload` | Solver direct din JSON |
| POST | `/solver/benchmark` | Benchmark parametri GA |

## Solve from Payload

Poti rula solverul fara cont si fara baza de date. In UI gasesti butonul **Import JSON** in pagina Solver.

**Structura minima:**
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

| Camp | Valori |
|------|--------|
| `runwayType` | `0` = Landing, `1` = Takeoff, `2` = Both |
| `flight.type` | `0` = Arrival, `1` = Departure, `2` = OnGround |
| `weatherSeverity` | `0` = Clear, `1` = Light, `2` = Moderate, `3` = Heavy, `4` = Severe, `5` = Storm |

Pentru un scenariu cu 500 de zboruri vezi `scripts/scenario-500.json`.

## Teste

```bash
dotnet test tests\RunwayScheduling.Tests\RunwayScheduling.Tests.csproj
```

Cu raport de coverage:
```bash
scripts\coverage.bat
```

## CI/CD

- **CI** (`ci.yml`): la orice push/PR pe `main` sau `develop` — build + test backend, lint + build frontend, build Docker
- **CD** (`cd.yml`): trigger manual — publica imaginile pe GitHub Container Registry

## Note

- Migratiile EF sunt aplicate automat la startup
- Solverul genetic porneste populatia initiala din solutia greedy
- CP-SAT rafineaza doar cei mai buni candidati din generatie, nu toata populatia
