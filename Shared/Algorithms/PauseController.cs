using System.Threading;

namespace Shared.Algorithms;

public sealed class PauseController : IDisposable
{
    private readonly ManualResetEventSlim _pauseEvent = new(true);

    public bool IsPaused { get; private set; }

    public void Pause()
    {
        IsPaused = true;
        _pauseEvent.Reset();
    }

    public void Resume()
    {
        IsPaused = false;
        _pauseEvent.Set();
    }

    public void WaitIfPaused(CancellationToken token)
    {
        _pauseEvent.Wait(token);
    }

    public void Dispose()
    {
        _pauseEvent.Dispose();
    }
}