# Runway Scheduling — Status & Next Steps

## Ce s-a făcut (Modules.Solver rebuild)

- [x] `SolveGreedyQuery.cs` + `SolveGreedyHandler.cs` create in `src/Modules.Solver/Application/UseCases/SolveGreedy/`
- [x] DI registration în `ServiceCollectionExtensions.cs`:
  - `IScenarioSnapshotFactory` → `ScenarioSnapshotFactory` (Scoped)
  - `ISchedulingEngine` → `SchedulingEngine` (Singleton)
  - `SolveGreedyHandler` assembly adăugat în MediatR
- [x] Endpoint `GET /greedy/{scenarioConfigId:guid}` adăugat în `Endpoints.cs`
- [x] Fix 404: URL era `/solver/greedy/...` → corectat la `/greedy/...`
- [x] Fix 405: endpoint era `MapPost` → corectat la `MapGet`

## Ce mai trebuie făcut

### 1. Endpoint `/compare/{scenarioId}` (PRIORITAR)
- **Unde e apelat**: `SolverPage.tsx` linia 325 — `apiFetch(\`/api/compare/${scenarioId}\`)`
- **Ce întoarce**: `{ greedy: SolverResultDto, genetic: SolverResultDto }` (vezi `SolverPage.tsx` linia 580, 656)
- **Ce trebuie creat**:
  - `CompareQuery.cs` + `CompareHandler.cs` în `src/Modules.Solver/Application/UseCases/Compare/`
  - Handler rulează greedy + GA, returnează ambele rezultate
  - Endpoint `GET /compare/{scenarioConfigId:guid}` în `Endpoints.cs`

### 2. GA Solver (Genetic Algorithm)
- Userul implementează mutation/crossover manual
- Trebuie schelet:
  - `GeneticSolverQuery.cs` + `GeneticSolverHandler.cs`
  - Endpoint `GET /genetic/{scenarioConfigId:guid}`
- Handler similar cu greedy dar apelează un `IGeneticSolvingEngine` sau similar

### 3. Verificare build
- Rulează `dotnet build` în `src/` să confirmi că totul compilează

## Structura relevantă

```
src/
  Api/
    Endpoints.cs              ← MapSolverEndpoints()
    ServiceCollectionExtensions.cs
  Modules.Solver/
    Application/
      Scheduling/
        ISchedulingEngine.cs
        SchedulingEngine.cs
      Snapshot/
        IScenarioSnapshotFactory.cs
        ScenarioSnapshotFactory.cs
      UseCases/
        SolveGreedy/
          SolveGreedyQuery.cs
          SolveGreedyHandler.cs
        Compare/              ← DE CREAT
        GeneticSolver/        ← DE CREAT (schelet)
    Domain/
      SolverResult.cs
      PreparedScenario.cs
  frontend/
    src/pages/SolverPage.tsx  ← linia 325 (compare), 344 (greedy/genetic)
```
