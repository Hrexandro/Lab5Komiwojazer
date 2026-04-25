using Shared.Models;
using System.Globalization;
using System.IO;

namespace Shared.Parsing;

public static class Parser
{
    public static List<City> LoadCities(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Nie znaleziono pliku TSP.", path);

        var cities = new List<City>();
        bool readingCoordinates = false;

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("NODE_COORD_SECTION", StringComparison.OrdinalIgnoreCase))
            {
                readingCoordinates = true;
                continue;
            }

            if (line.StartsWith("EOF", StringComparison.OrdinalIgnoreCase))
                break;

            if (!readingCoordinates)
                continue;

            var parts = line.Split(
                new[] { ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 3)
                continue;

            int id = int.Parse(parts[0], CultureInfo.InvariantCulture);
            double x = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double y = double.Parse(parts[2], CultureInfo.InvariantCulture);

            cities.Add(new City(id, x, y));
        }

        if (cities.Count == 0)
            throw new InvalidOperationException("Nie udało się wczytać żadnych miast z pliku.");

        return cities;
    }
}