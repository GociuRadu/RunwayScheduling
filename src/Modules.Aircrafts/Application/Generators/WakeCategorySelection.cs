using Modules.Aircrafts.Domain;

namespace Modules.Aircrafts.Application.Generators;

public static class WakeCategorySelection
{
    private static readonly Random _rand = Random.Shared;

    public static WakeTurbulenceCategory Generate(int difficulty)
    {
        difficulty = Math.Clamp(difficulty, 1, 5);

        int[] weights = difficulty switch
        {
            1 => new[] { 70, 25, 5, 0 },
            2 => new[] { 50, 35, 15, 0 },
            3 => new[] { 30, 40, 25, 5 },
            4 => new[] { 20, 30, 35, 15 },
            5 => new[] { 10, 20, 40, 30 },
            _ => new[] { 70, 25, 5, 0 }
        };

        int roll = _rand.Next(0, 100);

        int total = weights[0];
        if (roll < total) return WakeTurbulenceCategory.Light;

        total += weights[1];
        if (roll < total) return WakeTurbulenceCategory.Medium;

        total += weights[2];
        if (roll < total) return WakeTurbulenceCategory.Heavy;

        return WakeTurbulenceCategory.Super;
    }
}
