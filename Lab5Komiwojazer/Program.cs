using System.Net.ServerSentEvents;
using Tsp.Shared.Algorithms;
using Shared.Parsing;

string path = "wi29.tsp";
const int pmxAttempts = 10_000;

try
{
    var cities = Parser.LoadCities(path);

    Console.WriteLine($"Wczytano miast: {cities.Count}");

    var distances = DistanceMatrix.Build(cities);
    var random = new Random();

    var parent1 = TourGenerator.CreateRandomTour(cities.Count, random);
    var parent2 = TourGenerator.CreateRandomTour(cities.Count, random);

    double parent1Length = TourEvaluator.CalculateLength(parent1, distances);
    double parent2Length = TourEvaluator.CalculateLength(parent2, distances);

    Console.WriteLine();
    Console.WriteLine("Rodzic 1:");
    Console.WriteLine(string.Join(" -> ", parent1.Select(index => cities[index].Id)));
    Console.WriteLine($"Długość: {parent1Length:F2}");

    Console.WriteLine();
    Console.WriteLine("Rodzic 2:");
    Console.WriteLine(string.Join(" -> ", parent2.Select(index => cities[index].Id)));
    Console.WriteLine($"Długość: {parent2Length:F2}");

    var bestChild = PmxPhase.Run(parent1, parent2, distances, pmxAttempts, random);

    Console.WriteLine();
    Console.WriteLine($"Najlepszy potomek po {pmxAttempts} próbach PMX:");
    Console.WriteLine(string.Join(" -> ", bestChild.Order.Select(index => cities[index].Id)));
    Console.WriteLine($"Długość: {bestChild.Length:F2}");

    Console.WriteLine();
    Console.WriteLine("Faza PMX działa.");
}
catch (Exception ex)
{
    Console.WriteLine("Błąd:");
    Console.WriteLine(ex.Message);
}