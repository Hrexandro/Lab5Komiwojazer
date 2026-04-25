namespace Shared.Configuration;

public sealed record WorkerSettings(
    string Path,
    int WorkerCount,
    int EpochCount,
    TimeSpan PmxTime,
    TimeSpan ThreeOptTime,
    string Mode);