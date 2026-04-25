namespace Shared.Algorithms;

public static class PermutationValidator
{
    public static bool IsValid(int[] order, int expectedCount)
    {
        if (order.Length != expectedCount)
            return false;

        var seen = new bool[expectedCount];

        foreach (int value in order)
        {
            if (value < 0 || value >= expectedCount)
                return false;

            if (seen[value])
                return false;

            seen[value] = true;
        }

        return true;
    }

    public static void ThrowIfInvalid(int[] order, int expectedCount, string name)
    {
        if (!IsValid(order, expectedCount))
            throw new InvalidOperationException($"{name} nie jest poprawną permutacją.");
    }
}