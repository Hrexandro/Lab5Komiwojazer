namespace Tsp.Shared.Algorithms;

public static class PmxCrossover
{
    public static (int[] Child1, int[] Child2) Cross(
        int[] parent1,
        int[] parent2,
        int start,
        int end)
    {
        if (parent1.Length != parent2.Length)
            throw new ArgumentException("Rodzice muszą mieć tę samą długość.");

        if (start < 0 || end >= parent1.Length || start > end)
            throw new ArgumentException("Niepoprawny zakres krzyżowania.");

        int[] child1 = CreateChild(parent1, parent2, start, end);
        int[] child2 = CreateChild(parent2, parent1, start, end);

        return (child1, child2);
    }

    private static int[] CreateChild(
        int[] segmentParent,
        int[] fillingParent,
        int start,
        int end)
    {
        int length = segmentParent.Length;
        int[] child = Enumerable.Repeat(-1, length).ToArray();

        for (int i = start; i <= end; i++)
        {
            child[i] = segmentParent[i];
        }


        for (int i = start; i <= end; i++)
        {
            int valueFromFillingParent = fillingParent[i];

            if (child.Contains(valueFromFillingParent))
                continue;

            int position = i;

            while (true)
            {
                int mappedValue = segmentParent[position];
                position = Array.IndexOf(fillingParent, mappedValue);

                if (child[position] == -1)
                {
                    child[position] = valueFromFillingParent;
                    break;
                }
            }
        }

        for (int i = 0; i < length; i++)
        {
            if (child[i] == -1)
                child[i] = fillingParent[i];
        }

        return child;
    }
}