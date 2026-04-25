using Shared.Algorithms;
using Shared.Parsing;

string path = "wi29.tsp";

const int workerCount = 4;
const int epochCount = 5;
const int pmxAttempts = 10_000;

TimeSpan threeOptTime = TimeSpan.FromSeconds(2);

try
{
    var cities = Parser.LoadCities(path);

    Console.WriteLine($"Wczytano miast: {cities.Count}");
    Console.WriteLine($"Liczba zadań: {workerCount}");
    Console.WriteLine($"Liczba epok: {epochCount}");
    Console.WriteLine($"Próby PMX na epokę: {pmxAttempts}");
    Console.WriteLine($"Czas 3-opt na epokę: {threeOptTime.TotalSeconds:F0} s");
    Console.WriteLine("Tryb synchronizacji: Barrier");
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
        });

    Console.WriteLine();
    Console.WriteLine("Zakończono obliczenia.");
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