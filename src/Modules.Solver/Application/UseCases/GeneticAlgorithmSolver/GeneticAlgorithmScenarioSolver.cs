using System.Diagnostics;
using Modules.Scenarios.Domain;
using Modules.Solver.Application.GeneticAlgorithmSolver.Decoder;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GeneticAlgorithmSolver;

public class GeneticAlgorithmScenarioSolver : IScenarioSolver
{
    private const string AlgorithmName        = "Genetic Algorithm";
    private const int PopulationSize          = 50;
    private const int MaxGenerations          = 100;
    private const int ElitismCount            = 5;
    private const int MaxStagnantGenerations  = 20;
    private const int RefineEveryNGen         = 5;
    private const int CpSatEliteCount         = 2;

    private readonly ScheduleDecoder _decoder   = new();
    private readonly FitnessEvaluator _evaluator = new();
    private readonly Random _random              = new();

    public SolverResult Solve(ScenarioSnapshot snapshot)
    {
        var stopwatch = Stopwatch.StartNew();
        var operators = new GaOperators(_random);
        var refiner   = new CpSatWindowRefiner(snapshot);

        var population     = InitializePopulation(snapshot);
        var ranked         = EvaluatePopulation(population, snapshot);
        var bestScore      = ranked[0].Score;
        var stagnantCount  = 0;

        for (int gen = 0; gen < MaxGenerations; gen++)
        {
            var next = new List<Chromosome>(PopulationSize);

            // elitism: top ElitismCount go directly to next generation
            for (int i = 0; i < ElitismCount; i++)
                next.Add(ranked[i].Chromosome);

            // fill rest with crossover + mutation
            while (next.Count < PopulationSize)
            {
                var parent1 = operators.TournamentSelect(ranked);
                var parent2 = operators.TournamentSelect(ranked);
                var child   = operators.OrderCrossover(parent1, parent2);
                next.Add(operators.Mutate(child));
            }

            ranked = EvaluatePopulation(next, snapshot);

            // CP-SAT local search on top elites every RefineEveryNGen generations
            if ((gen + 1) % RefineEveryNGen == 0)
            {
                for (int i = 0; i < Math.Min(CpSatEliteCount, ranked.Count); i++)
                {
                    var decoded  = _decoder.Decode(ranked[i].Chromosome, snapshot);
                    var refined  = refiner.Refine(decoded);
                    var improved = ToChromosome(refined, snapshot);
                    ranked[i]    = (improved, _evaluator.Evaluate(refined));
                }
                ranked = ranked.OrderBy(x => x.Score).ToList();
            }

            if (ranked[0].Score < bestScore)
            {
                bestScore     = ranked[0].Score;
                stagnantCount = 0;
            }
            else
            {
                stagnantCount++;
                if (stagnantCount >= MaxStagnantGenerations)
                    break;
            }
        }

        stopwatch.Stop();

        var bestFlights = _decoder.Decode(ranked[0].Chromosome, snapshot);
        var scheduled   = bestFlights.Count(f => f.Status != FlightStatus.Canceled);
        var totalDelay  = bestFlights.Sum(f => f.DelayMinutes);
        var scenarioHours = (snapshot.ScenarioConfig.EndTime - snapshot.ScenarioConfig.StartTime).TotalHours;

        return new SolverResult
        {
            AlgorithmName           = AlgorithmName,
            Flights                 = bestFlights,
            TotalFlights            = snapshot.Flights.Count,
            TotalScheduledFlights   = scheduled,
            TotalOnTimeFlights      = bestFlights.Count(f => f.Status == FlightStatus.Scheduled),
            TotalEarlyFlights       = bestFlights.Count(f => f.Status == FlightStatus.Early),
            TotalDelayedFlights     = bestFlights.Count(f => f.Status == FlightStatus.Delayed),
            TotalCanceledFlights    = bestFlights.Count(f => f.Status == FlightStatus.Canceled),
            TotalRescheduledFlights = bestFlights.Count(f => f.Status == FlightStatus.Rescheduled),
            TotalDelayMinutes       = totalDelay,
            AverageDelayMinutes     = scheduled > 0 ? (double)totalDelay / scheduled : 0.0,
            MaxDelayMinutes         = bestFlights.Count > 0 ? bestFlights.Max(f => f.DelayMinutes) : 0,
            SolveTimeMs             = stopwatch.Elapsed.TotalMilliseconds,
            ThroughputFlightsPerHour = scenarioHours > 0 ? scheduled / scenarioHours : 0.0
        };
    }

    // rebuilds chromosome from CP-SAT refined solution by sorting on assigned time
    private static Chromosome ToChromosome(IReadOnlyList<SolvedFlight> flights, ScenarioSnapshot snapshot)
    {
        var idToIndex = snapshot.Flights
            .Select((f, i) => (f.Id, i))
            .ToDictionary(x => x.Id, x => x.i);

        var order = flights
            .OrderBy(f => f.AssignedTime ?? f.ScheduledTime)
            .Select(f => idToIndex[f.FlightId])
            .ToArray();

        return new Chromosome(order);
    }

    private List<Chromosome> InitializePopulation(ScenarioSnapshot snapshot)
    {
        var population = new List<Chromosome>(PopulationSize);
        population.Add(_decoder.BuildGreedyChromosome(snapshot));
        population.Add(PerturbedGreedy(snapshot));
        population.Add(PerturbedGreedy(snapshot));

        var flightCount = snapshot.Flights.Count;
        for (int i = 3; i < PopulationSize; i++)
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
