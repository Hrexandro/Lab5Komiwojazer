namespace Tsp.Shared.Models;

public sealed class Tour
{
    public int[] Order { get; }
    public double Length { get; set; }

    public Tour(int[] order)
    {
        Order = order;
    }
}