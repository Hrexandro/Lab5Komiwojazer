using System.Diagnostics;
using Shared.Models;

namespace Shared.Algorithms;

public static class ThreeOpt
{
    public static Tour Improve(
        Tour input,
        double[,] distances,
        TimeSpan maxTime)
    {
        var stopwatch = Stopwatch.StartNew();

        int[] bestOrder = (int[])input.Order.Clone();
        double bestLength = input.Length;

        int cityCount = bestOrder.Length;
        bool improved = true;

        while (improved && stopwatch.Elapsed < maxTime)
        {
            improved = false;

            for (int i = 0; i < cityCount - 2; i++)
            {
                for (int j = i + 1; j < cityCount - 1; j++)
                {
                    for (int k = j + 1; k < cityCount; k++)
                    {
                        if (stopwatch.Elapsed >= maxTime)
                            return new Tour(bestOrder) { Length = bestLength };

                        var candidates = GenerateCandidates(bestOrder, i, j, k);

                        foreach (var candidate in candidates)
                        {
                            double length = TourEvaluator.CalculateLength(candidate, distances);

                            if (length < bestLength)
                            {
                                bestOrder = candidate;
                                bestLength = length;
                                improved = true;
                            }
                        }
                    }
                }
            }
        }

        return new Tour(bestOrder) { Length = bestLength };
    }

    private static List<int[]> GenerateCandidates(int[] order, int i, int j, int k)
    {
        var candidates = new List<int[]>();

        candidates.Add(CloneAndReverse(order, i + 1, j));
        candidates.Add(CloneAndReverse(order, j + 1, k));
        candidates.Add(CloneAndReverse(order, i + 1, k));

        var candidate4 = (int[])order.Clone();
        Reverse(candidate4, i + 1, j);
        Reverse(candidate4, j + 1, k);
        candidates.Add(candidate4);

        var candidate5 = (int[])order.Clone();
        Reverse(candidate5, i + 1, j);
        Reverse(candidate5, i + 1, k);
        candidates.Add(candidate5);

        var candidate6 = (int[])order.Clone();
        Reverse(candidate6, j + 1, k);
        Reverse(candidate6, i + 1, k);
        candidates.Add(candidate6);

        var candidate7 = (int[])order.Clone();
        Reverse(candidate7, i + 1, k);
        Reverse(candidate7, i + 1, j);
        candidates.Add(candidate7);

        return candidates;
    }

    private static int[] CloneAndReverse(int[] order, int start, int end)
    {
        var copy = (int[])order.Clone();
        Reverse(copy, start, end);
        return copy;
    }

    private static void Reverse(int[] order, int start, int end)
    {
        while (start < end)
        {
            (order[start], order[end]) = (order[end], order[start]);
            start++;
            end--;
        }
    }
}