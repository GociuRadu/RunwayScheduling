using System.Diagnostics;
using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GeneticAlgorithmSolver;

public sealed class GeneticAlgorithmScenarioSolver : IScenarioSolver
{
    private const string AlgorithmName      = "Genetic";
    private const int    PopulationSize     = 60;
    private const int    MaxGenerations     = 200;
    private const int    NoImprovementLimit = 50;
    private const int    EliteCount         = 5;
    private const int    TournamentSize     = 3;
    private const double MutationRate       = 0.15;

    public SolverResult Solve(ScenarioSnapshot snapshot)
    {
        var stopwatch = Stopwatch.StartNew();
        var flights   = snapshot.Flights.ToList();
        var n         = flights.Count;

        if (n == 0)
        {
            stopwatch.Stop();
            return SchedulerDecoder.BuildResult([], 0, stopwatch.Elapsed.TotalMilliseconds, AlgorithmName, snapshot);
        }

        var rng        = new Random();
        var population = InitializePopulation(flights, snapshot, rng);

        var bestChromosome                = (int[])population[0].Chromosome.Clone();
        var bestFitness                   = population[0].Fitness;
        var generationsWithoutImprovement = 0;

        for (var gen = 0; gen < MaxGenerations && generationsWithoutImprovement < NoImprovementLimit; gen++)
        {
            population.Sort((a, b) => a.Fitness.CompareTo(b.Fitness));

            if (population[0].Fitness < bestFitness)
            {
                bestFitness                   = population[0].Fitness;
                bestChromosome                = (int[])population[0].Chromosome.Clone();
                generationsWithoutImprovement = 0;
            }
            else
            {
                generationsWithoutImprovement++;
            }

            var nextGeneration = new List<Individual>(PopulationSize);

            for (var i = 0; i < EliteCount; i++)
                nextGeneration.Add(population[i]);

            while (nextGeneration.Count < PopulationSize)
            {
                var parent1 = TournamentSelect(population, rng);
                var parent2 = TournamentSelect(population, rng);
                var child   = OrderCrossover(parent1.Chromosome, parent2.Chromosome, rng);

                if (rng.NextDouble() < MutationRate)
                    Mutate(child, rng);

                var fitness = EvaluateFitness(SchedulerDecoder.Decode(ToFlightList(child, flights), snapshot));
                nextGeneration.Add(new Individual(child, fitness));
            }

            population = nextGeneration;
        }

        var solvedFlights = SchedulerDecoder.Decode(ToFlightList(bestChromosome, flights), snapshot);

        stopwatch.Stop();
        return SchedulerDecoder.BuildResult(solvedFlights, n, stopwatch.Elapsed.TotalMilliseconds, AlgorithmName, snapshot);
    }

    // ── Initialization ─────────────────────────────────────────────────────────

    private static List<Individual> InitializePopulation(
        List<Flight> flights,
        ScenarioSnapshot snapshot,
        Random rng)
    {
        var n          = flights.Count;
        var population = new List<Individual>(PopulationSize);

        // Seed 1: greedy order (ScheduledTime ASC, Priority DESC)
        var greedyOrder = Enumerable.Range(0, n)
            .OrderBy(i => flights[i].ScheduledTime)
            .ThenByDescending(i => flights[i].Priority)
            .ToArray();
        population.Add(Evaluate(greedyOrder, flights, snapshot));

        // Seeds 2–12: shuffle within 30-minute time buckets for diversity near greedy
        for (var i = 0; i < 11 && population.Count < PopulationSize; i++)
        {
            var bucketed = TimeBucketShuffle(greedyOrder, flights, rng, TimeSpan.FromMinutes(30));
            population.Add(Evaluate(bucketed, flights, snapshot));
        }

        // Rest: fully random permutations
        while (population.Count < PopulationSize)
        {
            var chromosome = Enumerable.Range(0, n).ToArray();
            Shuffle(chromosome, rng);
            population.Add(Evaluate(chromosome, flights, snapshot));
        }

        return population;
    }

    private static Individual Evaluate(int[] chromosome, List<Flight> flights, ScenarioSnapshot snapshot)
    {
        var fitness = EvaluateFitness(SchedulerDecoder.Decode(ToFlightList(chromosome, flights), snapshot));
        return new Individual(chromosome, fitness);
    }

    private static int[] TimeBucketShuffle(
        int[] baseOrder,
        List<Flight> flights,
        Random rng,
        TimeSpan bucketSize)
    {
        var result = (int[])baseOrder.Clone();
        var n      = result.Length;
        var i      = 0;

        while (i < n)
        {
            var bucketEnd = flights[result[i]].ScheduledTime + bucketSize;
            var j         = i + 1;

            while (j < n && flights[result[j]].ScheduledTime < bucketEnd)
                j++;

            // Fisher-Yates within bucket [i, j)
            for (var k = j - 1; k > i; k--)
            {
                var swapIdx = rng.Next(i, k + 1);
                (result[k], result[swapIdx]) = (result[swapIdx], result[k]);
            }

            i = j;
        }

        return result;
    }


    private static double EvaluateFitness(List<SolvedFlight> flights)
    {
        var canceled              = flights.Count(f => f.Status == FlightStatus.Canceled);
        var priorityWeightedDelay = flights.Sum(f => f.DelayMinutes * (f.Priority + 1));
        var onTimeBonus           = flights.Count(f => f.Status == FlightStatus.Scheduled);

        return canceled * 100_000.0 + priorityWeightedDelay - onTimeBonus * 10.0;
    }


    private static Individual TournamentSelect(List<Individual> population, Random rng)
    {
        var best = population[rng.Next(population.Count)];
        for (var i = 1; i < TournamentSize; i++)
        {
            var candidate = population[rng.Next(population.Count)];
            if (candidate.Fitness < best.Fitness)
                best = candidate;
        }
        return best;
    }


    private static int[] OrderCrossover(int[] parent1, int[] parent2, Random rng)
    {
        var n     = parent1.Length;
        var child = new int[n];
        Array.Fill(child, -1);

        var start  = rng.Next(n);
        var end    = rng.Next(start, n);
        var copied = new HashSet<int>();

        for (var i = start; i <= end; i++)
        {
            child[i] = parent1[i];
            copied.Add(parent1[i]);
        }

        var fillPos = 0;
        foreach (var gene in parent2)
        {
            if (copied.Contains(gene)) continue;
            while (child[fillPos] != -1) fillPos++;
            child[fillPos++] = gene;
        }

        return child;
    }


    private static void Mutate(int[] chromosome, Random rng)
    {
        if (chromosome.Length < 2) return;

        if (rng.NextDouble() < 0.5)
            SwapMutate(chromosome, rng);
        else
            InsertionMutate(chromosome, rng);
    }

    private static void SwapMutate(int[] chromosome, Random rng)
    {
        var i = rng.Next(chromosome.Length);
        var j = rng.Next(chromosome.Length);
        (chromosome[i], chromosome[j]) = (chromosome[j], chromosome[i]);
    }

    private static void InsertionMutate(int[] chromosome, Random rng)
    {
        var n = chromosome.Length;
        var i = rng.Next(n);
        var j = rng.Next(n);
        if (i == j) return;

        var gene = chromosome[i];
        if (i < j)
            Array.Copy(chromosome, i + 1, chromosome, i, j - i);
        else
            Array.Copy(chromosome, j, chromosome, j + 1, i - j);
        chromosome[j] = gene;
    }


    private static List<Flight> ToFlightList(int[] chromosome, List<Flight> flights) =>
        [..chromosome.Select(i => flights[i])];

    private static void Shuffle(int[] array, Random rng)
    {
        for (var i = array.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    private sealed record Individual(int[] Chromosome, double Fitness);
}
