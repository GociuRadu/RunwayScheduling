using System.Diagnostics;
using Modules.Scenarios.Domain;
using Modules.Solver.Application.Scheduling;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.UseCases.SolveGenetic;

public sealed class GeneticAlgorithmSolver(ISchedulingEngine engine)
{
    private const string AlgorithmName = "Genetic Algorithm";
    private const double CancellationBase = 180.0;

    private const double Alpha = 1.0;
    private const double Beta = 100.0;
    private const double Gamma = 0.01;

    private readonly ISchedulingEngine _engine = engine;
    private readonly CpSatWindowRefiner _refiner = new(engine);

    public SolverResult Solve(PreparedScenario prepared, GaConfig config, Guid scenarioConfigId, out double solveTimeMs)
    {
        ArgumentNullException.ThrowIfNull(prepared);
        ArgumentNullException.ThrowIfNull(config);

        if (config.PopulationSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config.PopulationSize), "PopulationSize must be greater than zero.");
        }

        if (config.MaxGenerations < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config.MaxGenerations), "MaxGenerations cannot be negative.");
        }

        if (config.TimeWindowSize <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(config.TimeWindowSize), "TimeWindowSize must be greater than zero.");
        }

        var sw = Stopwatch.StartNew();
        var random = new Random(config.RandomSeed);
        var flightsCount = prepared.SortedFlights.Count;

        if (flightsCount == 0)
        {
            var emptyEvaluation = _engine.Evaluate(prepared.SortedFlights, prepared);
            sw.Stop();
            solveTimeMs = sw.Elapsed.TotalMilliseconds;
            return _engine.CreateResult(emptyEvaluation, scenarioConfigId, AlgorithmName, solveTimeMs);
        }

        var populationSize = config.PopulationSize;
        var eliteCount = Math.Min(Math.Max(config.EliteCount, 0), populationSize);
        var tournamentSize = Math.Clamp(config.TournamentSize, 1, populationSize);
        var crossoverRate = Math.Clamp(config.CrossoverRate, 0.0, 1.0);
        var mutationRateLocal = Math.Clamp(config.MutationRateLocal, 0.0, 1.0);
        var mutationRateMemetic = Math.Clamp(config.MutationRateMemetic, 0.0, 1.0);
        var noImprovementLimit = Math.Max(config.NoImprovementGenerations, 0);

        var timeWindows = BuildTimeWindows(prepared, config.TimeWindowSize);
        var flightWindowIndices = BuildFlightWindowLookup(timeWindows, flightsCount);
        var sourceIdToFlightIndex = BuildSourceIdLookup(prepared);

        var population = InitializePopulation(flightsCount, populationSize, random);
        var fitness = EvaluatePopulation(population, prepared);

        var bestFitnessIdx = 0;
        for (var i = 1; i < populationSize; i++)
            if (fitness[i] < fitness[bestFitnessIdx]) bestFitnessIdx = i;

        var bestFitness = fitness[bestFitnessIdx];
        var bestChromosome = CloneChromosome(population[bestFitnessIdx]);
        var generationsWithoutImprovement = 0;

        for (var generation = 0; generation < config.MaxGenerations; generation++)
        {
            var ranked = Enumerable.Range(0, populationSize)
                .OrderBy(index => fitness[index])
                .ToArray();

            var elites = new int[eliteCount][];
            for (var i = 0; i < eliteCount; i++)
            {
                elites[i] = CloneChromosome(population[ranked[i]]);
            }

            var nextPopulation = new int[populationSize][];

            for (var i = 0; i < populationSize; i++)
            {
                var parent1 = population[TournamentSelect(fitness, tournamentSize, random)];
                var parent2 = population[TournamentSelect(fitness, tournamentSize, random)];

                var child = random.NextDouble() < crossoverRate
                    ? OrderCrossover(parent1, parent2, random)
                    : CloneChromosome(parent1);

                MutateLocalSwap(child, prepared, mutationRateLocal, random);
                MutateMemetic(
                    child,
                    prepared,
                    timeWindows,
                    flightWindowIndices,
                    sourceIdToFlightIndex,
                    mutationRateMemetic,
                    random);

                nextPopulation[i] = child;
            }

            for (var i = 0; i < eliteCount; i++)
            {
                nextPopulation[i] = elites[i];
            }

            population = nextPopulation;
            fitness = EvaluatePopulation(population, prepared);

            if (config.EnableCpSatRefinement && config.CpSatMicroEnabled
                && generation % Math.Max(1, config.CpSatMicroEveryNGenerations) == 0)
            {
                var ranked2 = Enumerable.Range(0, populationSize)
                    .OrderBy(idx => fitness[idx])
                    .ToArray();

                var microCount = Math.Min(config.CpSatEliteCount, populationSize);
                for (var ei = 0; ei < microCount; ei++)
                {
                    var ci = ranked2[ei];
                    var eval = EvaluateChromosome(population[ci], prepared);
                    var improved = ApplyCpSatRefinement(
                        population[ci], prepared, timeWindows, flightWindowIndices,
                        sourceIdToFlightIndex, 1, config.CpSatNeighborhoodSize,
                        config.CpSatTimeLimitMsMicro, eval);
                    if (improved)
                        fitness[ci] = EvaluateChromosome(population[ci], prepared).Fitness;
                }

                for (var ri = 0; ri < config.CpSatRandomCount; ri++)
                {
                    var ci = random.Next(populationSize);
                    var eval = EvaluateChromosome(population[ci], prepared);
                    var improved = ApplyCpSatRefinement(
                        population[ci], prepared, timeWindows, flightWindowIndices,
                        sourceIdToFlightIndex, 1, config.CpSatNeighborhoodSize,
                        config.CpSatTimeLimitMsMicro, eval);
                    if (improved)
                        fitness[ci] = EvaluateChromosome(population[ci], prepared).Fitness;
                }
            }

            if (config.EnableCpSatRefinement && config.CpSatMacroEnabled
                && generation % Math.Max(1, config.CpSatMacroEveryNGenerations) == 0)
            {
                var bestIdx2 = 0;
                for (var idx = 1; idx < populationSize; idx++)
                    if (fitness[idx] < fitness[bestIdx2]) bestIdx2 = idx;

                var macroNeighborSize = Math.Min(config.CpSatNeighborhoodSize * 2, flightsCount);
                var eval = EvaluateChromosome(population[bestIdx2], prepared);
                var improved = ApplyCpSatRefinement(
                    population[bestIdx2], prepared, timeWindows, flightWindowIndices,
                    sourceIdToFlightIndex, config.CpSatMacroWindowCount,
                    macroNeighborSize, config.CpSatTimeLimitMsMacro, eval);
                if (improved)
                    fitness[bestIdx2] = EvaluateChromosome(population[bestIdx2], prepared).Fitness;
            }

            var generationBestFitness = fitness.Min();
            if (generationBestFitness < bestFitness)
            {
                bestFitness = generationBestFitness;
                generationsWithoutImprovement = 0;

                var genBestIdx = 0;
                for (var i = 1; i < populationSize; i++)
                    if (fitness[i] < fitness[genBestIdx]) genBestIdx = i;
                bestChromosome = CloneChromosome(population[genBestIdx]);
            }
            else if (noImprovementLimit > 0)
            {
                generationsWithoutImprovement++;
                if (generationsWithoutImprovement >= noImprovementLimit)
                {
                    break;
                }
            }
        }

        var bestEvaluation = EvaluateChromosome(bestChromosome, prepared);

        sw.Stop();
        solveTimeMs = sw.Elapsed.TotalMilliseconds;
        return _engine.CreateResult(bestEvaluation, scenarioConfigId, AlgorithmName, solveTimeMs);
    }

    private double[] EvaluatePopulation(int[][] population, PreparedScenario prepared)
    {
        var fitness = new double[population.Length];

        for (var i = 0; i < population.Length; i++)
        {
            fitness[i] = EvaluateChromosome(population[i], prepared).Fitness;
        }

        return fitness;
    }

    private SchedulingEvaluation EvaluateChromosome(int[] chromosome, PreparedScenario prepared)
    {
        var permuted = new List<(Flight Flight, Guid SourceId)>(chromosome.Length);

        for (var i = 0; i < chromosome.Length; i++)
        {
            permuted.Add(prepared.SortedFlights[chromosome[i]]);
        }

        return _engine.Evaluate(permuted, prepared);
    }

    private static int[][] InitializePopulation(int chromosomeLength, int populationSize, Random random)
    {
        var population = new int[populationSize][];
        var structuredCount = populationSize / 2;

        for (var i = 0; i < populationSize; i++)
        {
            var chromosome = CreateNaturalOrder(chromosomeLength);

            if (i == 0)
            {
                // Use the greedy order as the first seed.
            }
            else if (i < structuredCount)
            {
                ApplyPartialAdjacentShuffle(chromosome, random);
            }
            else
            {
                FisherYatesShuffle(chromosome, random);
            }

            population[i] = chromosome;
        }

        return population;
    }

    private static int[] CreateNaturalOrder(int length)
    {
        var chromosome = new int[length];
        for (var i = 0; i < length; i++)
        {
            chromosome[i] = i;
        }

        return chromosome;
    }

    private static void ApplyPartialAdjacentShuffle(int[] chromosome, Random random)
    {
        if (chromosome.Length < 2)
        {
            return;
        }

        var swapCount = Math.Max(1, (int)Math.Round(chromosome.Length * 0.1));
        for (var i = 0; i < swapCount; i++)
        {
            var left = random.Next(0, chromosome.Length - 1);
            Swap(chromosome, left, left + 1);
        }
    }

    private static void FisherYatesShuffle(int[] chromosome, Random random)
    {
        for (var i = chromosome.Length - 1; i > 0; i--)
        {
            var swapIndex = random.Next(i + 1);
            Swap(chromosome, i, swapIndex);
        }
    }

    private static int TournamentSelect(double[] fitness, int tournamentSize, Random random)
    {
        var bestIndex = random.Next(fitness.Length);

        for (var i = 1; i < tournamentSize; i++)
        {
            var candidateIndex = random.Next(fitness.Length);
            if (fitness[candidateIndex] < fitness[bestIndex])
            {
                bestIndex = candidateIndex;
            }
        }

        return bestIndex;
    }

    private static int[] OrderCrossover(int[] parent1, int[] parent2, Random random)
    {
        if (parent1.Length < 2)
        {
            return CloneChromosome(parent1);
        }

        var child = Enumerable.Repeat(-1, parent1.Length).ToArray();
        var used = new bool[parent1.Length];

        var cut1 = random.Next(parent1.Length);
        var cut2 = random.Next(parent1.Length);
        if (cut1 > cut2)
        {
            (cut1, cut2) = (cut2, cut1);
        }

        if (cut1 == cut2)
        {
            cut2 = Math.Min(parent1.Length - 1, cut1 + 1);
        }

        for (var i = cut1; i <= cut2; i++)
        {
            child[i] = parent1[i];
            used[parent1[i]] = true;
        }

        var insertIndex = (cut2 + 1) % parent1.Length;
        for (var offset = 0; offset < parent2.Length; offset++)
        {
            var gene = parent2[(cut2 + 1 + offset) % parent2.Length];
            if (used[gene])
            {
                continue;
            }

            child[insertIndex] = gene;
            used[gene] = true;
            insertIndex = (insertIndex + 1) % parent1.Length;
        }

        return child;
    }

    private static void MutateLocalSwap(int[] chromosome, PreparedScenario prepared, double mutationRateLocal, Random random)
    {
        if (chromosome.Length < 2 || mutationRateLocal <= 0)
        {
            return;
        }

        for (var i = 0; i < chromosome.Length; i++)
        {
            if (random.NextDouble() >= mutationRateLocal)
            {
                continue;
            }

            var currentFlight = prepared.SortedFlights[chromosome[i]].Flight;
            var windowStart = currentFlight.ScheduledTime.AddMinutes(-currentFlight.MaxEarlyMinutes);
            var windowEnd = currentFlight.ScheduledTime.AddMinutes(currentFlight.MaxDelayMinutes);

            var candidates = new List<int>();
            for (var j = 0; j < chromosome.Length; j++)
            {
                if (j == i)
                {
                    continue;
                }

                var candidateTime = prepared.SortedFlights[chromosome[j]].Flight.ScheduledTime;
                if (candidateTime >= windowStart && candidateTime <= windowEnd)
                {
                    candidates.Add(j);
                }
            }

            if (candidates.Count == 0)
            {
                continue;
            }

            var swapIndex = candidates[random.Next(candidates.Count)];
            Swap(chromosome, i, swapIndex);
        }
    }

    private void MutateMemetic(
        int[] chromosome,
        PreparedScenario prepared,
        IReadOnlyList<TimeWindow> timeWindows,
        IReadOnlyList<int> flightWindowIndices,
        IReadOnlyDictionary<Guid, int> sourceIdToFlightIndex,
        double mutationRateMemetic,
        Random random)
    {
        if (chromosome.Length == 0 || mutationRateMemetic <= 0 || random.NextDouble() >= mutationRateMemetic)
        {
            return;
        }

        var evaluation = EvaluateChromosome(chromosome, prepared);
        var windowFitness = new double[timeWindows.Count];

        foreach (var solvedFlight in evaluation.Flights)
        {
            if (!sourceIdToFlightIndex.TryGetValue(solvedFlight.FlightId, out var flightIndex))
            {
                continue;
            }

            var windowIndex = flightWindowIndices[flightIndex];
            windowFitness[windowIndex] += ComputePenalty(solvedFlight);
        }

        var targetWindowIndex = SelectWindowByRoulette(windowFitness, random);
        if (targetWindowIndex < 0)
        {
            return;
        }

        var targetPositions = new List<int>();
        for (var position = 0; position < chromosome.Length; position++)
        {
            if (flightWindowIndices[chromosome[position]] == targetWindowIndex)
            {
                targetPositions.Add(position);
            }
        }

        if (targetPositions.Count == 0)
        {
            return;
        }

        var intensity = ResolveIntensity(windowFitness, targetWindowIndex);
        var destroyEntries = targetPositions
            .Select(position => new DestroyCandidate(
                position,
                chromosome[position],
                prepared.SortedFlights[chromosome[position]].Flight,
                ComputePenalty(evaluation.Flights[position])))
            .OrderByDescending(candidate => candidate.CancelCost)
            .Take(Math.Min(intensity, targetPositions.Count))
            .ToList();

        if (destroyEntries.Count == 0)
        {
            return;
        }

        var pool = new Dictionary<int, PoolEntry>();

        foreach (var entry in destroyEntries)
        {
            pool[entry.FlightIndex] = CreatePoolEntry(entry.FlightIndex, entry.Flight, entry.CancelCost);
        }

        for (var position = 0; position < chromosome.Length; position++)
        {
            var solvedFlight = evaluation.Flights[position];
            if (solvedFlight.Status != FlightStatus.Canceled)
            {
                continue;
            }

            var flightIndex = chromosome[position];
            var flight = prepared.SortedFlights[flightIndex].Flight;
            var cancelCost = ComputePenalty(solvedFlight);
            pool[flightIndex] = CreatePoolEntry(flightIndex, flight, cancelCost);
        }

        if (pool.Count == 0)
        {
            return;
        }

        var prioritizedPool = pool.Values
            .OrderByDescending(entry => entry.ReschedulingPriority)
            .Select(entry => entry.FlightIndex)
            .ToList();

        var remaining = new List<int>(chromosome.Length - prioritizedPool.Count);
        for (var i = 0; i < chromosome.Length; i++)
        {
            if (!pool.ContainsKey(chromosome[i]))
            {
                remaining.Add(chromosome[i]);
            }
        }

        var candidateChromosome = new int[chromosome.Length];
        var writeIndex = 0;

        foreach (var flightIndex in prioritizedPool)
        {
            candidateChromosome[writeIndex++] = flightIndex;
        }

        foreach (var flightIndex in remaining)
        {
            candidateChromosome[writeIndex++] = flightIndex;
        }

        var candidateEvaluation = EvaluateChromosome(candidateChromosome, prepared);
        if (candidateEvaluation.Fitness < evaluation.Fitness)
        {
            Array.Copy(candidateChromosome, chromosome, chromosome.Length);
        }
    }

    private static PoolEntry CreatePoolEntry(int flightIndex, Flight flight, double cancelCost)
    {
        var reschedulingPriority = Alpha * cancelCost
            + Beta * (1.0 / (flight.MaxDelayMinutes + 1))
            - Gamma * (flight.MaxEarlyMinutes + flight.MaxDelayMinutes);

        return new PoolEntry(flightIndex, reschedulingPriority);
    }

    private static int SelectWindowByRoulette(IReadOnlyList<double> windowFitness, Random random)
    {
        var totalFitness = windowFitness.Where(value => value > 0).Sum();
        if (totalFitness <= 0)
        {
            return -1;
        }

        var threshold = random.NextDouble() * totalFitness;
        var cumulative = 0.0;

        for (var i = 0; i < windowFitness.Count; i++)
        {
            if (windowFitness[i] <= 0)
            {
                continue;
            }

            cumulative += windowFitness[i];
            if (threshold <= cumulative)
            {
                return i;
            }
        }

        for (var i = windowFitness.Count - 1; i >= 0; i--)
        {
            if (windowFitness[i] > 0)
            {
                return i;
            }
        }

        return -1;
    }

    private static int ResolveIntensity(IReadOnlyList<double> windowFitness, int targetWindowIndex)
    {
        var rankedWindows = windowFitness
            .Select((fitness, index) => new { fitness, index })
            .Where(entry => entry.fitness > 0)
            .OrderBy(entry => entry.fitness)
            .ToList();

        if (rankedWindows.Count == 0)
        {
            return 1;
        }

        var rank = 0;
        for (var i = 0; i < rankedWindows.Count; i++)
        {
            if (rankedWindows[i].index == targetWindowIndex)
            {
                rank = i + 1;
            }
        }

        var percentile = rank / (double)rankedWindows.Count * 100.0;
        if (percentile >= 95.0)
        {
            return 3;
        }

        if (percentile >= 90.0)
        {
            return 2;
        }

        return 1;
    }

    private static double ComputePenalty(SolvedFlight flight)
    {
        var multiplier = PriorityMultiplier(flight.Priority);
        return flight.Status == FlightStatus.Canceled
            ? CancellationBase * multiplier
            : flight.DelaySeconds / 60.0 * multiplier + flight.EarlySeconds / 60.0 * 0.5 * multiplier;
    }

    private static double PriorityMultiplier(int priority) => Math.Pow(1.2, priority - 1);

    private static List<TimeWindow> BuildTimeWindows(PreparedScenario prepared, TimeSpan timeWindowSize)
    {
        var scenario = prepared.Snapshot.ScenarioConfig;
        var windows = new List<TimeWindow>();
        var scenarioStart = scenario.StartTime;
        var scenarioEnd = scenario.EndTime;

        if (scenarioEnd <= scenarioStart)
        {
            windows.Add(new TimeWindow(scenarioStart, scenarioEnd));
        }
        else
        {
            var currentStart = scenarioStart;
            while (currentStart < scenarioEnd)
            {
                var currentEnd = currentStart + timeWindowSize;
                if (currentEnd > scenarioEnd)
                {
                    currentEnd = scenarioEnd;
                }

                windows.Add(new TimeWindow(currentStart, currentEnd));
                currentStart = currentEnd;
            }
        }

        if (windows.Count == 0)
        {
            windows.Add(new TimeWindow(scenarioStart, scenarioEnd));
        }

        for (var i = 0; i < prepared.SortedFlights.Count; i++)
        {
            var scheduledTime = prepared.SortedFlights[i].Flight.ScheduledTime;
            var windowIndex = ResolveWindowIndex(scheduledTime, windows, scenarioStart, scenarioEnd, timeWindowSize);
            windows[windowIndex].FlightIndices.Add(i);
        }

        return windows;
    }

    private static int ResolveWindowIndex(
        DateTime scheduledTime,
        IReadOnlyList<TimeWindow> windows,
        DateTime scenarioStart,
        DateTime scenarioEnd,
        TimeSpan timeWindowSize)
    {
        if (windows.Count == 1 || scheduledTime <= scenarioStart)
        {
            return 0;
        }

        if (scheduledTime >= scenarioEnd)
        {
            return windows.Count - 1;
        }

        var windowIndex = (int)((scheduledTime - scenarioStart).Ticks / timeWindowSize.Ticks);
        return Math.Clamp(windowIndex, 0, windows.Count - 1);
    }

    private static int[] BuildFlightWindowLookup(IReadOnlyList<TimeWindow> timeWindows, int flightsCount)
    {
        var flightWindowIndices = new int[flightsCount];

        for (var windowIndex = 0; windowIndex < timeWindows.Count; windowIndex++)
        {
            foreach (var flightIndex in timeWindows[windowIndex].FlightIndices)
            {
                flightWindowIndices[flightIndex] = windowIndex;
            }
        }

        return flightWindowIndices;
    }

    private static Dictionary<Guid, int> BuildSourceIdLookup(PreparedScenario prepared)
    {
        var map = new Dictionary<Guid, int>(prepared.SortedFlights.Count);
        for (var i = 0; i < prepared.SortedFlights.Count; i++)
        {
            map[prepared.SortedFlights[i].SourceId] = i;
        }

        return map;
    }

    /// <summary>Scores windows from the current evaluation and sends the worst flights to CP-SAT.</summary>
    private bool ApplyCpSatRefinement(
        int[] chromosome,
        PreparedScenario prepared,
        IReadOnlyList<TimeWindow> timeWindows,
        int[] flightWindowIndices,
        Dictionary<Guid, int> sourceIdToFlightIndex,
        int windowCount,
        int neighborhoodSize,
        int timeLimitMs,
        SchedulingEvaluation currentEval)
    {
        // Score each time window from the current evaluation.
        var windowFitness = new double[timeWindows.Count];
        foreach (var sf in currentEval.Flights)
        {
            if (!sourceIdToFlightIndex.TryGetValue(sf.FlightId, out var fi)) continue;
            windowFitness[flightWindowIndices[fi]] += ComputePenalty(sf);
        }

        // Keep the worst windows.
        var worstWindows = Enumerable.Range(0, timeWindows.Count)
            .Where(w => windowFitness[w] > 0)
            .OrderByDescending(w => windowFitness[w])
            .Take(windowCount)
            .ToList();

        if (worstWindows.Count == 0) return false;

        // Map flight indices back to chromosome positions.
        var posLookup = new Dictionary<int, int>(chromosome.Length);
        for (var pos = 0; pos < chromosome.Length; pos++)
            posLookup[chromosome[pos]] = pos;

        // Keep the highest-penalty flights in the neighborhood.
        var neighborhoodCandidates = new List<(int fi, double penalty)>();
        foreach (var wi in worstWindows)
        {
            foreach (var fi in timeWindows[wi].FlightIndices)
            {
                if (posLookup.TryGetValue(fi, out var flightPos))
                {
                    var penalty = ComputePenalty(currentEval.Flights[flightPos]);
                    neighborhoodCandidates.Add((fi, penalty));
                }
            }
        }

        var neighborhood = neighborhoodCandidates
            .OrderByDescending(x => x.penalty)
            .Take(neighborhoodSize)
            .Select(x => x.fi)
            .ToList();

        if (neighborhood.Count == 0) return false;

        return _refiner.Refine(chromosome, prepared, neighborhood, currentEval, timeLimitMs);
    }

    private static int[] CloneChromosome(int[] chromosome) => (int[])chromosome.Clone();

    private static void Swap(int[] chromosome, int left, int right)
    {
        (chromosome[left], chromosome[right]) = (chromosome[right], chromosome[left]);
    }

    private sealed class TimeWindow
    {
        public TimeWindow(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        public DateTime Start { get; }
        public DateTime End { get; }
        public List<int> FlightIndices { get; } = [];
    }

    private sealed record DestroyCandidate(int Position, int FlightIndex, Flight Flight, double CancelCost);

    private sealed record PoolEntry(int FlightIndex, double ReschedulingPriority);
}
