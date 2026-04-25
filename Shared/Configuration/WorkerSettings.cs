namespace Shared.Configuration;

public sealed record WorkerSettings(
    string Path,
    int WorkerCount,
    int EpochCount,
    int PmxAttempts,
    TimeSpan ThreeOptTime);