using Shared.Models;
using System.Diagnostics;
using System.Threading;

namespace Shared.Algorithms;

public static class ThreeOpt
{
    public static Tour Improve(
        Tour input,
        double[,] distances,
        TimeSpan maxTime,
        CancellationToken token = default,
        PauseController? pauseController = null)
    {
        var stopwatch = Stopwatch.StartNew();

        int[] bestOrder = (int[])input.Order.Clone();
        double bestLength = input.Length;

        int cityCount = bestOrder.Length;
        bool improved = true;

        while (improved && stopwatch.Elapsed < maxTime)
        {
            token.ThrowIfCancellationRequested();
            pauseController?.WaitIfPaused(token);

            improved = false;

            for (int i = 0; i < cityCount - 2; i++)
            {
                for (int j = i + 1; j < cityCount - 1; j++)
                {
                    for (int k = j + 1; k < cityCount; k++)
                    {
                        token.ThrowIfCancellationRequested();
                        pauseController?.WaitIfPaused(token);

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
        int[] a = order.Take(i + 1).ToArray();
        int[] b = order.Skip(i + 1).Take(j - i).ToArray();
        int[] c = order.Skip(j + 1).Take(k - j).ToArray();
        int[] d = order.Skip(k + 1).ToArray();

        return new List<int[]>
    {
        Join(a, ReverseCopy(b), c, d),
        Join(a, b, ReverseCopy(c), d),
        Join(a, ReverseCopy(b), ReverseCopy(c), d),

        Join(a, ReverseCopy(c), ReverseCopy(b), d),
        Join(a, c, b, d),
        Join(a, c, ReverseCopy(b), d),
        Join(a, ReverseCopy(c), b, d)
    };
    }

    private static int[] Join(params int[][] parts)
    {
        return parts.SelectMany(part => part).ToArray();
    }

    private static int[] ReverseCopy(int[] values)
    {
        var copy = (int[])values.Clone();
        Array.Reverse(copy);
        return copy;
    }
}