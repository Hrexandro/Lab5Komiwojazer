using Shared.Algorithms;
using Shared.Parsing;

string path = "wi29.tsp";

const int workerCount = 4;
const int epochCount = 100_000;
const int pmxAttempts = 10_000;

TimeSpan threeOptTime = TimeSpan.FromSeconds(2);

using var cts = new CancellationTokenSource();
using var pauseController = new PauseController();

cts.CancelAfter(TimeSpan.FromSeconds(8));

_ = Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(2));
    pauseController.Pause();
    Console.WriteLine();
    Console.WriteLine("Pauza.");

    await Task.Delay(TimeSpan.FromSeconds(4));
    pauseController.Resume();
    Console.WriteLine("Wznowienie.");
    Console.WriteLine();
});

try
{
    var cities = Parser.LoadCities(path);

    Console.WriteLine($"Wczytano miast: {cities.Count}");
    Console.WriteLine($"Liczba zadań: {workerCount}");
    Console.WriteLine($"Liczba epok: {epochCount}");
    Console.WriteLine($"Próby PMX na epokę: {pmxAttempts}");
    Console.WriteLine($"Czas 3-opt na epokę: {threeOptTime.TotalSeconds:F0} s");
    Console.WriteLine("Tryb synchronizacji: Barrier");
    Console.WriteLine("Pauza po 2 s, wznowienie po 6 s, przerwanie po 8 s");
    Console.WriteLine();

    var distances = DistanceMatrix.Build(cities);

    var result = await BarrierTspRunner.RunAsync(
        distances,
        cities.Count,
        workerCount,
        epochCount,
        pmxAttempts,
        threeOptTime,
        info =>
        {
            Console.WriteLine(
                $"Nowy najlepszy wynik | zadanie {info.WorkerId} | epoka {info.Epoch} | faza {info.Phase} | długość {info.Length:F2} | policzone: {info.ProcessedCount}");
        },
        cts.Token,
        pauseController);

    Console.WriteLine();

    if (result.WasCancelled)
        Console.WriteLine("Obliczenia zostały przerwane.");
    else
        Console.WriteLine("Obliczenia zakończone normalnie.");

    Console.WriteLine($"Policzone instancje: {result.ProcessedCount}");
    Console.WriteLine($"Najlepszy wynik: {result.BestTour.Length:F2}");
    Console.WriteLine("Najlepsza trasa:");
    Console.WriteLine(string.Join(" -> ", result.BestTour.Order.Select(index => cities[index].Id)));
}
catch (Exception ex)
{
    Console.WriteLine("Błąd:");
    Console.WriteLine(ex.Message);
}