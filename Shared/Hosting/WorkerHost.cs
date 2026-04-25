using Shared.Algorithms;
using Shared.Communication;
using Shared.Configuration;
using Shared.Parsing;

namespace Shared.Hosting;

public static class WorkerHost
{
    public static async Task RunAsync(string[] args, string forcedMode)
    {
        using var cts = new CancellationTokenSource();
        using var pauseController = new PauseController();

        try
        {
            var settings = CommandLineSettingsParser.Parse(args) with
            {
                Mode = forcedMode
            };

            var cities = Parser.LoadCities(settings.Path);
            var distances = DistanceMatrix.Build(cities);

            string synchronizationMode = settings.Mode == "threadpool"
                ? "ThreadPool + Barrier"
                : "TPL + Barrier";

            JsonLineWriter.Write(new StartedMessage(
                "started",
                cities.Count,
                settings.WorkerCount,
                settings.EpochCount,
                settings.PmxTime.TotalSeconds,
                settings.ThreeOptTime.TotalSeconds,
                synchronizationMode));

            _ = Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    string? line = Console.ReadLine();

                    if (line is null)
                        break;

                    string command = line.Trim().ToLowerInvariant();

                    if (command == "p" || command == "pause")
                    {
                        pauseController.Pause();
                        JsonLineWriter.Write(new ControlMessage("control", "pause"));
                    }
                    else if (command == "r" || command == "resume")
                    {
                        pauseController.Resume();
                        JsonLineWriter.Write(new ControlMessage("control", "resume"));
                    }
                    else if (command == "s" || command == "stop")
                    {
                        JsonLineWriter.Write(new ControlMessage("control", "stop"));
                        cts.Cancel();
                        break;
                    }
                }
            });

            Action<BestFoundInfo> bestHandler = info =>
            {
                JsonLineWriter.Write(new BestMessage(
                    "best",
                    info.WorkerId,
                    info.Epoch,
                    info.Phase,
                    info.Length,
                    info.ProcessedCount,
                    info.Tour));
            };
            Action<ProgressInfo> progressHandler = info =>
            {
                JsonLineWriter.Write(new ProgressMessage(
                    "progress",
                    info.WorkerId,
                    info.Epoch,
                    info.Phase,
                    info.ProcessedCount));
            };
            ParallelRunResult result;

            if (settings.Mode == "threadpool")
            {
                result = await ThreadPoolTspRunner.RunAsync(
                    distances,
                    cities.Count,
                    settings.WorkerCount,
                    settings.EpochCount,
                    settings.PmxTime,
                    settings.ThreeOptTime,
                    bestHandler,
                    progressHandler,
                    cts.Token,
                    pauseController);
            }
            else
            {
                result = await BarrierTspRunner.RunAsync(
                    distances,
                    cities.Count,
                    settings.WorkerCount,
                    settings.EpochCount,
                    settings.PmxTime,
                    settings.ThreeOptTime,
                    bestHandler,
                    progressHandler,
                    cts.Token,
                    pauseController);
            }

            JsonLineWriter.Write(new FinishedMessage(
                "finished",
                result.WasCancelled,
                result.ProcessedCount,
                result.BestTour.Length,
                (int[])result.BestTour.Order.Clone()));
        }
        catch (Exception ex)
        {
            JsonLineWriter.Write(new ErrorMessage("error", ex.Message));
        }
    }
}