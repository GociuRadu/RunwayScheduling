using Modules.Solver.Application.GeneticAlgorithmSolver.Decoder;

namespace Modules.Solver.Application.GeneticAlgorithmSolver;

public sealed class GaOperators(Random random, GaSolverConfig config)
{
    private readonly double _mutationRate = config.MutationRate;
    private readonly int _tournamentSize = config.TournamentSize;

    public Chromosome TournamentSelect(IReadOnlyList<(Chromosome Chromosome, FitnessScore Score)> ranked)
    {
        var best = ranked[random.Next(ranked.Count)];
        for (int i = 1; i < _tournamentSize; i++)
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
        if (n < 2)
            return new Chromosome(parent1.FlightOrder.ToArray());

        var child = new int[n];
        Array.Fill(child, -1);

        var start = random.Next(n);
        var end = random.Next(n);
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

    public Chromosome Mutate(Chromosome chromosome)
    {
        if (chromosome.FlightOrder.Length < 2 || random.NextDouble() > _mutationRate)
            return chromosome;

        return random.Next(2) == 0 ? InversionMutate(chromosome) : InsertMutate(chromosome);
    }

    private Chromosome InversionMutate(Chromosome chromosome)
    {
        var order = chromosome.FlightOrder.ToArray();
        var a = random.Next(order.Length);
        var b = random.Next(order.Length);
        if (a > b) (a, b) = (b, a);
        Array.Reverse(order, a, b - a + 1);
        return new Chromosome(order);
    }

    private Chromosome InsertMutate(Chromosome chromosome)
    {
        var order = chromosome.FlightOrder.ToList();
        var a = random.Next(order.Count);
        var element = order[a];
        order.RemoveAt(a);
        var b = random.Next(order.Count);
        order.Insert(b, element);
        return new Chromosome(order.ToArray());
    }
}
