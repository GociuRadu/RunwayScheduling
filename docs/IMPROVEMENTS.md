# Improvements Report

## Architectural Issues

- The API project hardcodes `OutputPath` to `C:\Projects\RunwayApi\`, which makes solution builds environment-dependent and breaks reproducible builds outside one machine.
- Validation is scattered inside handlers and endpoints instead of using a single validation pipeline. This increases duplication and causes invalid requests to surface as generic exceptions.
- Error handling relies heavily on `Exception` and `UnauthorizedAccessException` instead of typed application errors mapped centrally to HTTP responses.
- The API composition root registers concrete solver types directly. There is an `IScenarioSolver` abstraction, but the runtime wiring still assumes a single strategy.
- `ScenarioSnapshotLoader` depends on MediatR queries rather than a dedicated read model or repository facade, which couples the solver path to application-layer message contracts.
- Domain entities are mostly anemic data containers. Business rules live in handlers and services, which makes reuse and test focus harder as complexity grows.

## Missing Features

- There is no unified request validation framework such as FluentValidation for commands and queries.
- There is no consistent problem-details response model for API errors.
- Authentication has no password rotation, bootstrap-user workflow, refresh token flow, or account lockout policy.
- Solver results are not persisted or versioned, which will matter once multiple solving strategies exist.
- There is no benchmark or scenario-comparison surface for evaluating solver quality across algorithms.

## Performance Concerns

- `ScenarioSnapshotLoader` issues multiple sequential MediatR calls to assemble one snapshot. This creates avoidable overhead and will become more expensive as scenario size grows.
- `CreateFlightsHandler` repeatedly searches the aircraft list with `First(...)` inside a loop. A dictionary keyed by aircraft id would avoid repeated scans.
- `GreedyScenarioSolver` scans weather intervals and random events with `FirstOrDefault(...)` for every assignment. Pre-sorted interval lookups or indexed windows would scale better.
- The current in-memory flow materializes full collections at each layer. Larger scenarios will need tighter projection and fewer intermediate lists.

## GA Solver Readiness

- Promote `IScenarioSolver` to the primary runtime abstraction and register strategies by key or enum so `Greedy` and `GA` can coexist cleanly.
- Introduce a solver-selection request contract and response metadata that always includes solver id, version, seed, objective scores, and execution time.
- Separate snapshot loading from solver execution with a stable immutable input model. Both greedy and GA solvers should consume the same normalized snapshot.
- Define a shared objective model now: delay cost, cancellation cost, runway utilization, fairness, and disruption penalties. GA fitness can then reuse the same metrics already exposed by greedy results.
- Extract constraint evaluation into reusable services or domain components. The GA solver will need the same runway compatibility, scenario-window, weather, and random-event rules.
- Add deterministic seed handling at the solver level. GA runs need explicit reproducibility for tests, comparisons, and debugging.
- Add benchmark fixtures and golden scenarios so new strategies can be compared on quality and runtime before becoming user-selectable.
