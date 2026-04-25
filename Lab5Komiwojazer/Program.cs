using Shared.Algorithms;
using Shared.Communication;
using Shared.Parsing;

string path = "wi29.tsp";

const int workerCount = 4;
const int epochCount = 100_000;
const int pmxAttempts = 10_000;

TimeSpan threeOptTime = TimeSpan.FromSeconds(2);

using var cts = new CancellationTokenSource();
using var pauseController = new PauseController();

try
{
    var cities = Parser.LoadCities(path);
    var distances = DistanceMatrix.Build(cities);

    JsonLineWriter.Write(new StartedMessage(
        "started",
        cities.Count,
        workerCount,
        epochCount,
        pmxAttempts,
        threeOptTime.TotalSeconds,
        "Barrier"));

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

    var result = await BarrierTspRunner.RunAsync(
        distances,
        cities.Count,
        workerCount,
        epochCount,
        pmxAttempts,
        threeOptTime,
        info =>
        {
            JsonLineWriter.Write(new BestMessage(
                "best",
                info.WorkerId,
                info.Epoch,
                info.Phase,
                info.Length,
                info.ProcessedCount,
                info.Tour));
        },
        cts.Token,
        pauseController);

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