using Tsp.Shared.Models;

namespace Tsp.Shared.Algorithms;

public static class PmxPhase
{
    public static Tour Run(
        int[] parent1,
        int[] parent2,
        double[,] distances,
        int attempts,
        Random random)
    {
        if (attempts <= 0)
            throw new ArgumentException("Liczba prób musi być większa od zera.");

        int cityCount = parent1.Length;

        Tour? bestTour = null;

        for (int i = 0; i < attempts; i++)
        {
            int start = random.Next(0, cityCount - 1);
            int end = random.Next(start + 1, cityCount);

            var (child1, child2) = PmxCrossover.Cross(parent1, parent2, start, end);

            PermutationValidator.ThrowIfInvalid(child1, cityCount, "Potomek 1");
            PermutationValidator.ThrowIfInvalid(child2, cityCount, "Potomek 2");

            double child1Length = TourEvaluator.CalculateLength(child1, distances);
            double child2Length = TourEvaluator.CalculateLength(child2, distances);

            var betterChild = child1Length <= child2Length
                ? new Tour(child1) { Length = child1Length }
                : new Tour(child2) { Length = child2Length };

            if (bestTour is null || betterChild.Length < bestTour.Length)
                bestTour = betterChild;
        }

        return bestTour!;
    }
}