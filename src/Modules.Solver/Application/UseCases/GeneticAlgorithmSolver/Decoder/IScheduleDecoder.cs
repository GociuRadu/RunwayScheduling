using Modules.Scenarios.Domain;
using Modules.Solver.Domain;

namespace Modules.Solver.Application.GeneticAlgorithmSolver.Decoder;

public sealed record Chromosome(int[] FlightOrder);

public interface IScheduleDecoder
{
    Chromosome BuildGreedyChromosome(ScenarioSnapshot snapshot);
    IReadOnlyList<SolvedFlight> Decode(Chromosome chromosome, ScenarioSnapshot snapshot);
}
