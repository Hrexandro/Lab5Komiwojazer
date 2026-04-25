using Tsp.Shared.Algorithms;
using Tsp.Shared.Parsing;

string path = "wi29.tsp";

try
{
    var cities = TspParser.LoadCities(path);

    Console.WriteLine($"Wczytano miast: {cities.Count}");

    var distances = DistanceMatrix.Build(cities);

    var random = new Random();

    var randomTour = TourGenerator.CreateRandomTour(cities.Count, random);
    double randomTourLength = TourEvaluator.CalculateLength(randomTour, distances);

    Console.WriteLine($"Losowa trasa: {randomTourLength:F2}");
    Console.WriteLine("Kolejność miast:");
    Console.WriteLine(string.Join(" -> ", randomTour.Select(index => cities[index].Id)));

    Console.WriteLine();

    const int randomAttempts = 10_000;

    double bestLength = double.MaxValue;
    int[]? bestTour = null;

    for (int i = 0; i < randomAttempts; i++)
    {
        var tour = TourGenerator.CreateRandomTour(cities.Count, random);
        double length = TourEvaluator.CalculateLength(tour, distances);

        if (length < bestLength)
        {
            bestLength = length;
            bestTour = tour;
        }
    }

    Console.WriteLine($"Najlepsza z {randomAttempts} losowych tras: {bestLength:F2}");

    if (bestTour is not null)
    {
        Console.WriteLine("Najlepsza kolejność miast:");
        Console.WriteLine(string.Join(" -> ", bestTour.Select(index => cities[index].Id)));
    }
}
catch (Exception ex)
{
    Console.WriteLine("Błąd:");
    Console.WriteLine(ex.Message);
}