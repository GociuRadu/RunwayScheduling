using System.Diagnostics;
using Modules.Scenarios.Domain;
using Modules.Solver.Application.GeneticAlgorithmSolver.Decoder;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GeneticAlgorithmSolver;

public class GeneticAlgorithmScenarioSolver : IScenarioSolver
{
    private const string AlgorithmName = "Genetic Algorithm";
    private const int PopulationSize   = 50;
    private const int MaxGenerations   = 100;
    private const int ElitismCount     = 5;

    private readonly ScheduleDecoder _decoder   = new();
    private readonly FitnessEvaluator _evaluator = new();
    private readonly Random _random              = new();

    public SolverResult Solve(ScenarioSnapshot snapshot)
    {
        var stopwatch = Stopwatch.StartNew();
        var operators = new GaOperators(_random);

        var population = InitializePopulation(snapshot);
        var ranked     = EvaluatePopulation(population, snapshot);

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
                next.Add(operators.SwapMutate(child));
            }

            ranked = EvaluatePopulation(next, snapshot);
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
