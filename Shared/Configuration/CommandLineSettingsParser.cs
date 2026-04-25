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
                TimeSpan.FromSeconds(2),
                "tpl");
        }

        if (args.Length != 6)
            throw new ArgumentException("Oczekiwane argumenty: plik.tsp liczbaZadan liczbaEpok probyPMX czas3OptWSekundach tryb");

        string path = args[0];

        int workerCount = int.Parse(args[1], CultureInfo.InvariantCulture);
        int epochCount = int.Parse(args[2], CultureInfo.InvariantCulture);
        int pmxAttempts = int.Parse(args[3], CultureInfo.InvariantCulture);
        double threeOptSeconds = double.Parse(args[4], CultureInfo.InvariantCulture);
        string mode = args[5].Trim().ToLowerInvariant();

        if (workerCount < 2)
            throw new ArgumentException("Liczba zadań musi wynosić co najmniej 2.");

        if (epochCount <= 0)
            throw new ArgumentException("Liczba epok musi być większa od zera.");

        if (pmxAttempts <= 0)
            throw new ArgumentException("Liczba prób PMX musi być większa od zera.");

        if (threeOptSeconds <= 0)
            throw new ArgumentException("Czas 3-opt musi być większy od zera.");

        if (mode is not "tpl" and not "tasks" and not "task" and not "threadpool")
            throw new ArgumentException("Tryb musi mieć wartość: tpl albo threadpool.");

        if (mode is "tasks" or "task")
            mode = "tpl";

        return new WorkerSettings(
            path,
            workerCount,
            epochCount,
            pmxAttempts,
            TimeSpan.FromSeconds(threeOptSeconds),
            mode);
    }
}