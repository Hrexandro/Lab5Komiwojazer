using Shared.Models;
using System.Diagnostics;
using System.Threading;

namespace Shared.Algorithms;

public static class PmxPhase
{
    public static Tour Run(
        int[] parent1,
        int[] parent2,
        double[,] distances,
        TimeSpan maxTime,
        Random random,
        CancellationToken token = default,
        PauseController? pauseController = null)
    {
        var pair = RunPair(parent1, parent2, distances, maxTime, random, token, pauseController);

        return pair.First.Length <= pair.Second.Length
            ? pair.First
            : pair.Second;
    }

    public static (Tour First, Tour Second) RunPair(
        int[] parent1,
        int[] parent2,
        double[,] distances,
        TimeSpan maxTime,
        Random random,
        CancellationToken token = default,
        PauseController? pauseController = null)
    {
        if (maxTime <= TimeSpan.Zero)
            throw new ArgumentException("Czas PMX musi być większy od zera.");

        int cityCount = parent1.Length;

        Tour? bestFirst = null;
        Tour? bestSecond = null;
        double bestPairScore = double.PositiveInfinity;

        var stopwatch = Stopwatch.StartNew();

        do
        {
            token.ThrowIfCancellationRequested();
            pauseController?.WaitIfPaused(token);

            int start = random.Next(0, cityCount - 1);
            int end = random.Next(start + 1, cityCount);

            var (child1, child2) = PmxCrossover.Cross(parent1, parent2, start, end);

            PermutationValidator.ThrowIfInvalid(child1, cityCount, "Potomek 1");
            PermutationValidator.ThrowIfInvalid(child2, cityCount, "Potomek 2");

            double child1Length = TourEvaluator.CalculateLength(child1, distances);
            double child2Length = TourEvaluator.CalculateLength(child2, distances);

            var first = new Tour(child1) { Length = child1Length };
            var second = new Tour(child2) { Length = child2Length };

            double pairScore = child1Length + child2Length;

            if (pairScore < bestPairScore)
            {
                bestPairScore = pairScore;
                bestFirst = first;
                bestSecond = second;
            }
        }
        while (stopwatch.Elapsed < maxTime);

        return (bestFirst!, bestSecond!);
    }
}