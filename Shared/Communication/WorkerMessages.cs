namespace Shared.Communication;

public sealed record StartedMessage(
    string Type,
    int CityCount,
    int WorkerCount,
    int EpochCount,
    int PmxAttempts,
    double ThreeOptSeconds,
    string SynchronizationMode);

public sealed record BestMessage(
    string Type,
    int WorkerId,
    int Epoch,
    string Phase,
    double Length,
    long ProcessedCount,
    int[] Tour);

public sealed record ControlMessage(
    string Type,
    string Command);

public sealed record FinishedMessage(
    string Type,
    bool WasCancelled,
    long ProcessedCount,
    double BestLength,
    int[] BestTour);

public sealed record ErrorMessage(
    string Type,
    string Message);

public sealed record ProgressMessage(
    string Type,
    int WorkerId,
    int Epoch,
    string Phase,
    long ProcessedCount);