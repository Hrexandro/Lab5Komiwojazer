using Tsp.Shared.Models;

namespace Tsp.Shared.Algorithms;

public static class DistanceMatrix
{
    public static double[,] Build(IReadOnlyList<City> cities)
    {
        int count = cities.Count;
        var distances = new double[count, count];

        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                double dx = cities[i].X - cities[j].X;
                double dy = cities[i].Y - cities[j].Y;

                distances[i, j] = Math.Sqrt(dx * dx + dy * dy);
            }
        }

        return distances;
    }
}