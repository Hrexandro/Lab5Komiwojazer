using System.Globalization;

namespace Shared.Configuration;

public static class CommandLineSettingsParser
{
    public static WorkerSettings Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new WorkerSettings(
                "wi29.tsp",
                4,
                100_000,
                10_000,
                TimeSpan.FromSeconds(2));
        }

        if (args.Length != 5)
            throw new ArgumentException("Oczekiwane argumenty: plik.tsp liczbaZadan liczbaEpok probyPMX czas3OptWSekundach");

        string path = args[0];

        int workerCount = int.Parse(args[1], CultureInfo.InvariantCulture);
        int epochCount = int.Parse(args[2], CultureInfo.InvariantCulture);
        int pmxAttempts = int.Parse(args[3], CultureInfo.InvariantCulture);
        double threeOptSeconds = double.Parse(args[4], CultureInfo.InvariantCulture);

        if (workerCount < 2)
            throw new ArgumentException("Liczba zadań musi wynosić co najmniej 2.");

        if (epochCount <= 0)
            throw new ArgumentException("Liczba epok musi być większa od zera.");

        if (pmxAttempts <= 0)
            throw new ArgumentException("Liczba prób PMX musi być większa od zera.");

        if (threeOptSeconds <= 0)
            throw new ArgumentException("Czas 3-opt musi być większy od zera.");

        return new WorkerSettings(
            path,
            workerCount,
            epochCount,
            pmxAttempts,
            TimeSpan.FromSeconds(threeOptSeconds));
    }
}