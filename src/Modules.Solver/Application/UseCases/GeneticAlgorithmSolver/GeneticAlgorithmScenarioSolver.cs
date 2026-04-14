using System.Diagnostics;
using Modules.Scenarios.Domain;
using Modules.Solver.Application.GeneticAlgorithmSolver.Decoder;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GeneticAlgorithmSolver;

public class GeneticAlgorithmScenarioSolver : IScenarioSolver
{
    private const string AlgorithmName = "Genetic Algorithm";

    private readonly GaSolverConfig _config;
    private readonly ScheduleDecoder _decoder = new();
    private readonly FitnessEvaluator _evaluator = new();
    private readonly Random _random;

    public GeneticAlgorithmScenarioSolver(GaSolverConfig? config = null, Random? random = null)
    {
        _config = config ?? new GaSolverConfig();
        _random = random ?? new Random();
    }

    public SolverResult Solve(ScenarioSnapshot snapshot)
    {
        var stopwatch = Stopwatch.StartNew();
        var operators = new GaOperators(_random, _config);
        var refiner = new CpSatWindowRefiner(snapshot, _config);
        var idToIndex = snapshot.Flights
            .Select((f, i) => (f.Id, i))
            .ToDictionary(x => x.Id, x => x.i);

        var population = InitializePopulation(snapshot);
        var ranked = EvaluatePopulation(population, snapshot);
        var globalBestScore = ranked[0].Score;
        var globalBestFlights = _decoder.Decode(ranked[0].Chromosome, snapshot);
        var stagnantCount = 0;

        for (int gen = 0; gen < _config.MaxGenerations; gen++)
        {
            var next = new List<Chromosome>(_config.PopulationSize);

            // elitism: top ElitismCount go directly to next generation
            for (int i = 0; i < Math.Min(_config.ElitismCount, ranked.Count); i++)
                next.Add(ranked[i].Chromosome);

            // fill rest with crossover + mutation
            while (next.Count < _config.PopulationSize)
            {
                var parent1 = operators.TournamentSelect(ranked);
                var parent2 = operators.TournamentSelect(ranked);
                var child = operators.OrderCrossover(parent1, parent2);
                next.Add(operators.Mutate(child));
            }

            ranked = EvaluatePopulation(next, snapshot);

            // CP-SAT local search on top elites every RefineEveryNGen generations
            if ((gen + 1) % _config.RefineEveryNGen == 0)
            {
                for (int i = 0; i < Math.Min(_config.CpSatEliteCount, ranked.Count); i++)
                {
                    var decoded = _decoder.Decode(ranked[i].Chromosome, snapshot);
                    var refined = refiner.Refine(decoded);
                    var improved = ToChromosome(refined, idToIndex);
                    ranked[i] = (improved, _evaluator.Evaluate(_decoder.Decode(improved, snapshot)));
                }
                ranked = ranked.OrderBy(x => x.Score).ToList();
            }

            if (ranked[0].Score < globalBestScore)
            {
                globalBestScore = ranked[0].Score;
                globalBestFlights = _decoder.Decode(ranked[0].Chromosome, snapshot);
                stagnantCount = 0;
            }
            else
            {
                stagnantCount++;
                if (stagnantCount >= _config.MaxStagnantGenerations)
                    break;
            }
        }

        stopwatch.Stop();

        return SolverResultFactory.Create(
            AlgorithmName,
            globalBestFlights,
            snapshot.Flights.Count,
            snapshot.ScenarioConfig.StartTime,
            snapshot.ScenarioConfig.EndTime,
            stopwatch.Elapsed.TotalMilliseconds);
    }

    private static Chromosome ToChromosome(IReadOnlyList<SolvedFlight> flights, Dictionary<Guid, int> idToIndex)
    {
        var order = flights
            .OrderBy(f => f.AssignedTime ?? f.ScheduledTime)
            .Select(f => idToIndex[f.FlightId])
            .ToArray();

        return new Chromosome(order);
    }

    private List<Chromosome> InitializePopulation(ScenarioSnapshot snapshot)
    {
        var population = new List<Chromosome>(_config.PopulationSize);
        population.Add(_decoder.BuildGreedyChromosome(snapshot));
        population.Add(PerturbedGreedy(snapshot));
        population.Add(PerturbedGreedy(snapshot));

        var flightCount = snapshot.Flights.Count;
        for (int i = 3; i < _config.PopulationSize; i++)
        {
            var order = Enumerable.Range(0, flightCount).ToArray();
            _random.Shuffle(order);
            population.Add(new Chromosome(order));
        }

        return population;
    }

    private List<(Chromosome Chromosome, FitnessScore Score)> EvaluatePopulation(
        List<Chromosome> population, ScenarioSnapshot snapshot) =>
        population
            .Select(c => (Chromosome: c, Score: _evaluator.Evaluate(_decoder.Decode(c, snapshot))))
            .OrderBy(x => x.Score)
            .ToList();

    private Chromosome PerturbedGreedy(ScenarioSnapshot snapshot)
    {
        var order = _decoder.BuildGreedyChromosome(snapshot).FlightOrder.ToArray();
        if (order.Length < 2)
            return new Chromosome(order);

        var swaps = _random.Next(2, 10);
        for (int i = 0; i < swaps; i++)
        {
            int a = _random.Next(order.Length);
            int b = _random.Next(order.Length);
            (order[a], order[b]) = (order[b], order[a]);
        }

        return new Chromosome(order);
    }
}
