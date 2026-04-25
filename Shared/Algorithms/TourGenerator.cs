namespace Tsp.Shared.Algorithms;

public static class TourGenerator
{
    public static int[] CreateRandomTour(int cityCount, Random random)
    {
        var order = Enumerable.Range(0, cityCount).ToArray();

        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);

            (order[i], order[j]) = (order[j], order[i]);
        }

        return order;
    }
}