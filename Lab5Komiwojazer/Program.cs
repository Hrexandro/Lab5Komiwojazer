using Shared.Algorithms;
using Shared.Communication;
using Shared.Configuration;
using Shared.Parsing;

using var cts = new CancellationTokenSource();
using var pauseController = new PauseController();

try
{
    var settings = CommandLineSettingsParser.Parse(args);

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
        settings.PmxAttempts,
        settings.ThreeOptTime.TotalSeconds,
        synchronizationMode));

    var controlTask = Task.Run(() =>
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

    ParallelRunResult result;

    if (settings.Mode == "threadpool")
    {
        result = await ThreadPoolTspRunner.RunAsync(
            distances,
            cities.Count,
            settings.WorkerCount,
            settings.EpochCount,
            settings.PmxAttempts,
            settings.ThreeOptTime,
            bestHandler,
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
            settings.PmxAttempts,
            settings.ThreeOptTime,
            bestHandler,
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