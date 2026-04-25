namespace Shared.Algorithms;

public static class TourEvaluator
{
    public static double CalculateLength(int[] order, double[,] distances)
    {
        if (order.Length < 2)
            return 0;

        double total = 0;

        for (int i = 0; i < order.Length - 1; i++)
        {
            int from = order[i];
            int to = order[i + 1];

            total += distances[from, to];
        }

        // Powrót z ostatniego miasta do pierwszego, bo komiwojażer robi cykl.
        total += distances[order[^1], order[0]];

        return total;
    }
}