using Modules.Solver.Application.GeneticAlgorithmSolver.Decoder;

namespace Modules.Solver.Application.GeneticAlgorithmSolver;

public sealed class GaOperators(Random random)
{
    private const double MutationRate = 0.05;
    private const int TournamentSize  = 3;

    public Chromosome TournamentSelect(IReadOnlyList<(Chromosome Chromosome, FitnessScore Score)> ranked)
    {
        var best = ranked[random.Next(ranked.Count)];
        for (int i = 1; i < TournamentSize; i++)
        {
            var candidate = ranked[random.Next(ranked.Count)];
            if (candidate.Score < best.Score)
                best = candidate;
        }
        return best.Chromosome;
    }

    public Chromosome OrderCrossover(Chromosome parent1, Chromosome parent2)
    {
        var n = parent1.FlightOrder.Length;
        var child = new int[n];
        Array.Fill(child, -1);

        var start = random.Next(n);
        var end   = random.Next(n);
        if (start > end) (start, end) = (end, start);

        // copy segment from parent1
        for (int i = start; i <= end; i++)
            child[i] = parent1.FlightOrder[i];

        // fill remaining from parent2 in order
        var inChild = new HashSet<int>(child.Where(x => x != -1));
        var p2Queue = parent2.FlightOrder.Where(x => !inChild.Contains(x));
        using var enumerator = p2Queue.GetEnumerator();

        for (int i = 0; i < n; i++)
        {
            if (child[i] == -1)
            {
                enumerator.MoveNext();
                child[i] = enumerator.Current;
            }
        }

        return new Chromosome(child);
    }

    public Chromosome SwapMutate(Chromosome chromosome)
    {
        if (random.NextDouble() > MutationRate)
            return chromosome;

        var order = chromosome.FlightOrder.ToArray();
        var a = random.Next(order.Length);
        var b = random.Next(order.Length);
        (order[a], order[b]) = (order[b], order[a]);
        return new Chromosome(order);
    }
}
