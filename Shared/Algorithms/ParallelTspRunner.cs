using Shared.Models;
using System.Threading;
using Shared.Algorithms;

namespace Shared.Algorithms;

public static class ParallelTspRunner
{
    public static async Task<ParallelRunResult> RunAsync(
        double[,] distances,
        int cityCount,
        int workerCount,
        int epochCount,
        int pmxAttempts,
        TimeSpan threeOptTime,
        Action<BestFoundInfo>? onBestFound = null,
        CancellationToken token = default)
    {
        if (workerCount <= 0)
            throw new ArgumentException("Liczba zadań musi być większa od zera.");

        if (epochCount <= 0)
            throw new ArgumentException("Liczba epok musi być większa od zera.");

        if (pmxAttempts <= 0)
            throw new ArgumentException("Liczba prób PMX musi być większa od zera.");

        var bestStore = new BestResultStore();
        long processedCount = 0;

        var tasks = new Task[workerCount];

        for (int workerId = 0; workerId < workerCount; workerId++)
        {
            int capturedWorkerId = workerId;

            tasks[workerId] = Task.Run(() =>
            {
                var random = new Random(unchecked(Environment.TickCount * 31 + capturedWorkerId * 9973));

                for (int epoch = 1; epoch <= epochCount; epoch++)
                {
                    token.ThrowIfCancellationRequested();

                    var parent1 = TourGenerator.CreateRandomTour(cityCount, random);
                    var parent2 = TourGenerator.CreateRandomTour(cityCount, random);

                    var bestChild = PmxPhase.Run(parent1, parent2, distances, pmxAttempts, random);

                    long afterPmxCount = Interlocked.Increment(ref processedCount);

                    if (bestStore.TryUpdate(bestChild))
                    {
                        onBestFound?.Invoke(new BestFoundInfo(
                            capturedWorkerId,
                            epoch,
                            "PMX",
                            bestChild.Length,
                            afterPmxCount));
                    }

                    token.ThrowIfCancellationRequested();

                    var improved = ThreeOpt.Improve(bestChild, distances, threeOptTime);

                    long afterThreeOptCount = Interlocked.Increment(ref processedCount);

                    if (bestStore.TryUpdate(improved))
                    {
                        onBestFound?.Invoke(new BestFoundInfo(
                            capturedWorkerId,
                            epoch,
                            "3-opt",
                            improved.Length,
                            afterThreeOptCount));
                    }
                }
            }, token);
        }

        await Task.WhenAll(tasks);

        var best = bestStore.GetBest();

        if (best is null)
            throw new InvalidOperationException("Nie znaleziono żadnego wyniku.");

        return new ParallelRunResult(best, processedCount);
    }
}

public sealed record ParallelRunResult(Tour BestTour, long ProcessedCount);

public sealed record BestFoundInfo(
    int WorkerId,
    int Epoch,
    string Phase,
    double Length,
    long ProcessedCount);