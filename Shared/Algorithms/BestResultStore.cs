using Shared.Models;
using System.Threading;

namespace Shared.Algorithms;

public sealed class BestResultStore
{
    private readonly ReaderWriterLockSlim _lock = new();
    private Tour? _best;

    public Tour? GetBest()
    {
        _lock.EnterReadLock();

        try
        {
            return _best;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool TryUpdate(Tour candidate)
    {
        _lock.EnterUpgradeableReadLock();

        try
        {
            if (_best is not null && candidate.Length >= _best.Length)
                return false;

            _lock.EnterWriteLock();

            try
            {
                if (_best is null || candidate.Length < _best.Length)
                {
                    _best = candidate;
                    return true;
                }

                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }
}