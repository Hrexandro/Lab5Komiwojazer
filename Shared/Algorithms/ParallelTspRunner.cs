using Shared.Models;
using System.Threading;

namespace Shared.Algorithms;

public static class ParallelTspRunner
{
    public static async Task<ParallelRunResult> RunAsync(
        double[,] distances,
        int cityCount,
        int workerCount,
        int epochCount,
        TimeSpan pmxTime,
        TimeSpan threeOptTime,
        Action<BestFoundInfo>? onBestFound = null,
        Action<ProgressInfo>? onProgress = null,
        CancellationToken token = default,
        PauseController? pauseController = null)
    {
        if (workerCount <= 0)
            throw new ArgumentException("Liczba zadań musi być większa od zera.");

        if (epochCount <= 0)
            throw new ArgumentException("Liczba epok musi być większa od zera.");

        if (pmxTime <= TimeSpan.Zero)
            throw new ArgumentException("Czas PMX musi być większy od zera.");

        if (threeOptTime <= TimeSpan.Zero)
            throw new ArgumentException("Czas 3-opt musi być większy od zera.");

        var bestStore = new BestResultStore();
        long processedCount = 0;
        bool wasCancelled = false;

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
                    pauseController?.WaitIfPaused(token);

                    var parent1 = TourGenerator.CreateRandomTour(cityCount, random);
                    var parent2 = TourGenerator.CreateRandomTour(cityCount, random);

                    var bestChild = PmxPhase.Run(
                        parent1,
                        parent2,
                        distances,
                        pmxTime,
                        random,
                        token,
                        pauseController);

                    long afterPmxCount = Interlocked.Increment(ref processedCount);

                    onProgress?.Invoke(new ProgressInfo(
                        capturedWorkerId,
                        epoch,
                        "PMX",
                        afterPmxCount));

                    if (bestStore.TryUpdate(bestChild))
                    {
                        onBestFound?.Invoke(new BestFoundInfo(
                            capturedWorkerId,
                            epoch,
                            "PMX",
                            bestChild.Length,
                            afterPmxCount,
                            (int[])bestChild.Order.Clone()));
                    }

                    token.ThrowIfCancellationRequested();
                    pauseController?.WaitIfPaused(token);

                    var improved = ThreeOpt.Improve(
                        bestChild,
                        distances,
                        threeOptTime,
                        token,
                        pauseController);

                    long afterThreeOptCount = Interlocked.Increment(ref processedCount);

                    onProgress?.Invoke(new ProgressInfo(
                        capturedWorkerId,
                        epoch,
                        "3-opt",
                        afterThreeOptCount));

                    if (bestStore.TryUpdate(improved))
                    {
                        onBestFound?.Invoke(new BestFoundInfo(
                            capturedWorkerId,
                            epoch,
                            "3-opt",
                            improved.Length,
                            afterThreeOptCount,
                            (int[])improved.Order.Clone()));
                    }
                }
            }, token);
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
        }

        var best = bestStore.GetBest();

        if (best is null)
            throw new InvalidOperationException("Nie znaleziono żadnego wyniku.");

        return new ParallelRunResult(best, processedCount, wasCancelled);
    }
}

public sealed record ParallelRunResult(Tour BestTour, long ProcessedCount, bool WasCancelled = false);

public sealed record BestFoundInfo(
    int WorkerId,
    int Epoch,
    string Phase,
    double Length,
    long ProcessedCount,
    int[] Tour);

public sealed record ProgressInfo(
    int WorkerId,
    int Epoch,
    string Phase,
    long ProcessedCount);