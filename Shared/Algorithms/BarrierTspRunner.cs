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
        TimeSpan pmxTime,
        TimeSpan threeOptTime,
        Action<BestFoundInfo>? onBestFound = null,
        Action<ProgressInfo>? onProgress = null,
        CancellationToken token = default,
        PauseController? pauseController = null)
    {


        if (workerCount < 2)
            throw new ArgumentException("Liczba zadań musi wynosić co najmniej 2.");

        if (epochCount <= 0)
            throw new ArgumentException("Liczba epok musi być większa od zera.");

        if (pmxTime <= TimeSpan.Zero)
            throw new ArgumentException("Czas PMX musi być większy od zera.");

        var bestStore = new BestResultStore();
        long processedCount = 0;
        int cancellationObserved = 0;

        var parent1Inputs = new int[workerCount][];
        var parent2Inputs = new int[workerCount][];

        var pmxResults = new Tour?[workerCount * 2];
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

            onProgress?.Invoke(new ProgressInfo(
                workerId,
                epoch,
                phase,
                count));

            if (bestStore.TryUpdate(candidate))
            {
                onBestFound?.Invoke(new BestFoundInfo(
                    workerId,
                    epoch,
                    phase,
                    candidate.Length,
                    count,
                    (int[])candidate.Order.Clone()));
            }
        }

        using var pmxBarrier = new Barrier(workerCount, _ =>
        {
            var selected = pmxResults
                .Where(result => result is not null)
                .Select(result => result!)
                .OrderBy(result => result.Length)
                .Take(workerCount)
                .ToArray();

            if (selected.Length < workerCount)
                return;

            for (int i = 0; i < workerCount; i++)
            {
                optInputs[i] = selected[i];
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

            if (selected.Length == 0)
                return;

            var random = new Random(unchecked(Environment.TickCount * 17 + (int)processedCount));

            var parentPool = new List<int[]>();

            while (parentPool.Count < workerCount * 2)
            {
                foreach (var tour in selected)
                {
                    parentPool.Add((int[])tour.Order.Clone());

                    if (parentPool.Count >= workerCount * 2)
                        break;
                }
            }

            Shuffle(parentPool, random);

            for (int i = 0; i < workerCount; i++)
            {
                parent1Inputs[i] = parentPool[2 * i];
                parent2Inputs[i] = parentPool[2 * i + 1];

                if (selected.Length > 1 && parent1Inputs[i].SequenceEqual(parent2Inputs[i]))
                {
                    int swapIndex = (2 * i + 2) % parentPool.Count;
                    (parent2Inputs[i], parentPool[swapIndex]) = (parentPool[swapIndex], parent2Inputs[i]);
                }

                if (parent1Inputs[i].SequenceEqual(parent2Inputs[i]))
                {
                    parent2Inputs[i] = TourGenerator.CreateRandomTour(cityCount, random);
                }
            }
        });

        var tasks = new Task[workerCount];

        for (int workerId = 0; workerId < workerCount; workerId++)
        {
            int capturedWorkerId = workerId;

            tasks[workerId] = Task.Run(() =>
            {
                try
                {
                    var random = new Random(unchecked(Environment.TickCount * 31 + capturedWorkerId * 9973));

                    for (int epoch = 1; epoch <= epochCount; epoch++)
                    {
                        token.ThrowIfCancellationRequested();
                        pauseController?.WaitIfPaused(token);

                        var children = PmxPhase.RunPair(
                            parent1Inputs[capturedWorkerId],
                            parent2Inputs[capturedWorkerId],
                            distances,
                            pmxTime,
                            random,
                            token,
                            pauseController);

                        pmxResults[capturedWorkerId * 2] = children.First;
                        pmxResults[capturedWorkerId * 2 + 1] = children.Second;

                        var bestChild = children.First.Length <= children.Second.Length
                            ? children.First
                            : children.Second;

                        ReportIfBest(capturedWorkerId, epoch, "PMX", bestChild);

                        pauseController?.WaitIfPaused(token);
                        pmxBarrier.SignalAndWait(token);

                        token.ThrowIfCancellationRequested();
                        pauseController?.WaitIfPaused(token);

                        var improved = ThreeOpt.Improve(
                            optInputs[capturedWorkerId]!,
                            distances,
                            threeOptTime,
                            token,
                            pauseController);

                        optResults[capturedWorkerId] = improved;
                        ReportIfBest(capturedWorkerId, epoch, "3-opt", improved);

                        pauseController?.WaitIfPaused(token);
                        optBarrier.SignalAndWait(token);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    Interlocked.Exchange(ref cancellationObserved, 1);
                }
            });

        }

        await Task.WhenAll(tasks);

        var best = bestStore.GetBest();

        if (best is null)
            throw new InvalidOperationException("Nie znaleziono żadnego wyniku.");

        bool wasCancelled = token.IsCancellationRequested || Volatile.Read(ref cancellationObserved) == 1;

        return new ParallelRunResult(best, processedCount, wasCancelled);
    }
    private static void Shuffle<T>(IList<T> values, Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}