using Shared.Models;
using System.Threading;

namespace Shared.Algorithms;

public static class BarrierTspRunner
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
        if (workerCount < 2)
            throw new ArgumentException("Liczba zadań musi wynosić co najmniej 2.");

        if (epochCount <= 0)
            throw new ArgumentException("Liczba epok musi być większa od zera.");

        if (pmxAttempts <= 0)
            throw new ArgumentException("Liczba prób PMX musi być większa od zera.");

        var bestStore = new BestResultStore();
        long processedCount = 0;
        bool wasCancelled = false;

        var parent1Inputs = new int[workerCount][];
        var parent2Inputs = new int[workerCount][];

        var pmxResults = new Tour?[workerCount];
        var optInputs = new Tour?[workerCount];
        var optResults = new Tour?[workerCount];

        var setupRandom = new Random();

        for (int i = 0; i < workerCount; i++)
        {
            parent1Inputs[i] = TourGenerator.CreateRandomTour(cityCount, setupRandom);
            parent2Inputs[i] = TourGenerator.CreateRandomTour(cityCount, setupRandom);
        }

        void ReportIfBest(int workerId, int epoch, string phase, Tour candidate)
        {
            long count = Interlocked.Increment(ref processedCount);

            if (bestStore.TryUpdate(candidate))
            {
                onBestFound?.Invoke(new BestFoundInfo(
                    workerId,
                    epoch,
                    phase,
                    candidate.Length,
                    count));
            }
        }

        using var pmxBarrier = new Barrier(workerCount, _ =>
        {
            var selected = pmxResults
                .Where(result => result is not null)
                .Select(result => result!)
                .OrderBy(result => result.Length)
                .Take(Math.Max(1, workerCount / 2))
                .ToArray();

            for (int i = 0; i < workerCount; i++)
            {
                optInputs[i] = selected[i % selected.Length];
            }
        });

        using var optBarrier = new Barrier(workerCount, _ =>
        {
            var selected = optResults
                .Where(result => result is not null)
                .Select(result => result!)
                .OrderBy(result => result.Length)
                .Take(Math.Max(1, workerCount / 2))
                .ToArray();

            var random = new Random(unchecked(Environment.TickCount * 17 + (int)processedCount));

            for (int i = 0; i < workerCount; i++)
            {
                parent1Inputs[i] = (int[])selected[i % selected.Length].Order.Clone();

                if (selected.Length == 1)
                    parent2Inputs[i] = TourGenerator.CreateRandomTour(cityCount, random);
                else
                    parent2Inputs[i] = (int[])selected[(i + 1) % selected.Length].Order.Clone();
            }
        });

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

                    var bestChild = PmxPhase.Run(
                        parent1Inputs[capturedWorkerId],
                        parent2Inputs[capturedWorkerId],
                        distances,
                        pmxAttempts,
                        random,
                        token);

                    pmxResults[capturedWorkerId] = bestChild;
                    ReportIfBest(capturedWorkerId, epoch, "PMX", bestChild);

                    pmxBarrier.SignalAndWait(token);

                    token.ThrowIfCancellationRequested();

                    var improved = ThreeOpt.Improve(
                        optInputs[capturedWorkerId]!,
                        distances,
                        threeOptTime,
                        token);

                    optResults[capturedWorkerId] = improved;
                    ReportIfBest(capturedWorkerId, epoch, "3-opt", improved);

                    optBarrier.SignalAndWait(token);
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