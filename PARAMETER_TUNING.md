# Hybrid GA + CP-SAT Parameter Tuning

## Context

The current solver minimizes a weighted delay objective where:

- delay cost = `delayMinutes * 1.2^(priority - 1)`
- cancellation cost = `180 * 1.2^(priority - 1)`

This means a cancellation is treated roughly like 180 minutes of delay for the same priority. In overloaded scenarios, the most important parameters are the ones that change:

- how much diversity the GA keeps,
- how aggressively the GA converges,
- how often and how deeply CP-SAT intensifies the best candidates.

All current values below are taken from:

- `src/Modules.Solver/Application/UseCases/GeneticAlgorithmSolver/GeneticAlgorithmScenarioSolver.cs`
- `src/Modules.Solver/Application/UseCases/GeneticAlgorithmSolver/GaOperators.cs`
- `src/Modules.Solver/Application/UseCases/GeneticAlgorithmSolver/CpSatWindowRefiner.cs`

## Recommended tuning order

1. `PopulationSize`, `MutationRate`, `TournamentSize`
   These define the exploration vs exploitation balance. If these are wrong, the rest of the tuning is mostly noise.
2. `RefineEveryNGen`, `CpSatTimeLimitMs`, `MaxWindowHours`
   These determine how much the CP-SAT layer actually contributes versus how much runtime overhead it adds.
3. `MaxGenerations`, `MaxStagnantGenerations`
   These control the total search budget after the search dynamics are healthy.
4. `ElitismCount`, `CpSatEliteCount`
   These are second-order intensification knobs. They matter, but usually after the first three groups are in a good range.

## GA parameters

### `PopulationSize`

- Current value: `100`
- Recommended range to test: `40-300`
- Increase it:
  - more solution diversity,
  - lower risk of premature convergence,
  - better chance to find good flight orders in highly congested scenarios,
  - noticeably higher runtime because every generation decodes and evaluates more chromosomes.
- Decrease it:
  - faster generations,
  - easier to iterate on experiments,
  - higher risk that the GA collapses early to one scheduling pattern and never recovers.
- Notes:
  - this is usually the highest-leverage GA parameter,
  - a practical rule is to start near `5x-10x` the number of effective runway bottlenecks you expect in the busy region, then scale from there.

### `MaxGenerations`

- Current value: `200`
- Recommended range to test: `50-400`
- Increase it:
  - gives crossover, mutation, and CP-SAT more time to compound improvements,
  - helps when progress is still happening late,
  - increases runtime almost linearly,
  - also increases CP-SAT calls because refinement cadence is generation-based.
- Decrease it:
  - much lower wall-clock time,
  - useful for quick comparative sweeps,
  - risks stopping before cancellations and delay tradeoffs are fully improved.
- Notes:
  - tune this after `PopulationSize`, `MutationRate`, and `TournamentSize`,
  - if stagnation is almost always reached early, raising `MaxGenerations` alone will not help much.

### `ElitismCount`

- Current value: `5`
- Recommended range to test: `2-10` for `PopulationSize=100`
- Increase it:
  - preserves the best chromosomes more aggressively,
  - stabilizes improvements across generations,
  - reduces diversity because fewer slots remain for new offspring,
  - can lock the population around a mediocre basin if selection pressure is already high.
- Decrease it:
  - allows more exploration,
  - makes the GA noisier from one generation to the next,
  - increases the chance that good structures are lost before CP-SAT can refine them.
- Notes:
  - a good starting point is roughly `2%-8%` of the population,
  - it interacts strongly with `TournamentSize`; high elitism plus large tournaments usually converges too hard.

### `MaxStagnantGenerations`

- Current value: `20`
- Recommended range to test: `10-50`
- Increase it:
  - gives slow-improving runs more time,
  - helps when CP-SAT produces occasional late improvements,
  - wastes time if the population has genuinely converged.
- Decrease it:
  - shortens bad runs,
  - improves benchmark throughput,
  - can cut off runs that improve in bursts instead of steadily.
- Notes:
  - a good starting heuristic is `10%-25%` of `MaxGenerations`,
  - if you see many runs stop early with still-meaningful diversity, this value is probably too low.

### `RefineEveryNGen`

- Current value: `5`
- Recommended range to test: `3-15`
- Increase it:
  - reduces CP-SAT overhead,
  - keeps the algorithm more GA-driven,
  - may miss chances to clean up elites before the population drifts away.
- Decrease it:
  - invokes CP-SAT more often,
  - strengthens local improvement on promising chromosomes,
  - can dominate runtime quickly,
  - can over-intensify too early, which reduces diversity if elites become very similar.
- Notes:
  - this is the main hybridization knob,
  - if CP-SAT is giving clear fitness gains, reduce it first before raising `CpSatTimeLimitMs`.

### `CpSatEliteCount`

- Current value: `2`
- Recommended range to test: `1-5`
- Increase it:
  - refines more top solutions at each refinement step,
  - can widen the number of locally improved candidates entering the next cycle,
  - increases runtime almost linearly with the number of refined elites.
- Decrease it:
  - makes CP-SAT cheaper,
  - concentrates local search on the very best chromosome(s),
  - risks missing alternate promising basins in the top-ranked set.
- Notes:
  - usually keep this low,
  - a good default is `1` or `2`,
  - values larger than `ElitismCount` are usually hard to justify experimentally.

### `MutationRate`

- Current value: `0.05`
- Recommended range to test: `0.02-0.15`
- Increase it:
  - injects more diversity,
  - helps escape local optima in overloaded scenarios,
  - can destroy useful order segments if pushed too high,
  - makes results noisier across runs.
- Decrease it:
  - preserves building blocks found by crossover and CP-SAT,
  - makes convergence faster and smoother,
  - increases the risk of premature convergence.
- Notes:
  - this is usually the second most important GA parameter after population size,
  - for permutation problems like sequencing flights, values above `0.15` often start behaving too randomly unless the population is also large.

### `TournamentSize`

- Current value: `5`
- Recommended range to test: `2-7`
- Increase it:
  - raises selection pressure,
  - pushes the population faster toward current best solutions,
  - often improves early fitness quickly,
  - can collapse diversity and make mutation work harder.
- Decrease it:
  - lowers selection pressure,
  - preserves more weak-but-different chromosomes,
  - slows convergence,
  - can make the GA wander if mutation is also high.
- Notes:
  - this is the cleanest way to change convergence pressure without changing runtime much,
  - if fitness improves fast in the first generations and then freezes, tournament size is often too large.

## CP-SAT parameters

### `CpSatTimeLimitMs`

- Current value: `30`
- Recommended range to test: `20-200`
- Increase it:
  - lets CP-SAT search deeper inside each refinement window,
  - improves the chance of turning elites into genuinely better schedules,
  - directly increases solve time and can dominate runtime because it is called repeatedly.
- Decrease it:
  - makes the hybrid loop much cheaper,
  - useful for large benchmark grids,
  - may return weaker feasible solutions or fail to improve elites meaningfully.
- Notes:
  - this is usually the largest runtime lever on the CP-SAT side,
  - tune it together with `RefineEveryNGen`; frequent refinements plus large time limits multiply each other.

### `MaxWindowHours`

- Current value: `3`
- Recommended range to test: `1-6`
- Increase it:
  - creates fewer but larger windows,
  - gives CP-SAT a more global view inside each optimization slice,
  - increases model size and makes a low time limit less effective,
  - can reduce boundary artifacts between windows.
- Decrease it:
  - creates more but smaller windows,
  - makes each CP-SAT model easier to solve,
  - makes refinement more myopic because flights near window boundaries are optimized separately.
- Notes:
  - tune this only after you understand whether the current time limit is enough,
  - if `CpSatTimeLimitMs` stays very small, large windows usually underperform because the solver cannot exploit the larger search space.

## Practical tuning recipe

1. Fix CP-SAT at the current defaults first: `CpSatTimeLimitMs=30`, `MaxWindowHours=3`.
2. Sweep GA dynamics first:
   - `PopulationSize`: `60, 100, 160`
   - `MutationRate`: `0.03, 0.05, 0.08`
   - `TournamentSize`: `3, 5, 7`
3. Once a stable region appears, tune budget:
   - `MaxGenerations`: `100, 200, 300`
   - `MaxStagnantGenerations`: `15, 25, 40`
4. Then tune hybrid intensity:
   - `RefineEveryNGen`: `3, 5, 8, 12`
   - `CpSatTimeLimitMs`: `30, 60, 120`
   - `CpSatEliteCount`: `1, 2, 3`
5. Only then test larger CP-SAT windows:
   - `MaxWindowHours`: `2, 3, 4, 6`
6. Finish with small elitism adjustments:
   - `ElitismCount`: `3, 5, 8`

## What to watch in benchmarks

- If `SolveTimeMs` rises sharply with little fitness improvement:
  - reduce `MaxGenerations`,
  - reduce CP-SAT intensity before shrinking the population.
- If fitness is unstable across runs:
  - raise `PopulationSize`,
  - lower `TournamentSize`,
  - slightly lower `MutationRate`.
- If cancellations stay high:
  - increase exploration first,
  - then increase CP-SAT intensity,
  - only after that increase the total generation budget.
- If delay decreases but cancellations rise:
  - selection pressure is likely too strong,
  - or CP-SAT windows are too myopic for the amount of overload in the scenario.
