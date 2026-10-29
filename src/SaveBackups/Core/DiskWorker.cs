using System;
using System.Collections.Concurrent;
using System.Threading;

namespace SaveBackups.Core;

/// <summary>
/// Runs the disk work (copying a run's files, reading the backups) on one background thread, one job at a time and in
/// the order asked, so a frame never waits on the disk and two jobs never touch the same folder at once. A job's
/// answer comes back through <see cref="Drain"/> on the main thread, where the game may be touched. A job that throws
/// hands its exception to the answer instead.
/// </summary>
public sealed class DiskWorker : IDisposable
{
    private readonly BlockingCollection<Action> _jobs = new();
    private readonly ConcurrentQueue<Action> _answers = new();
    private readonly Thread _thread;
    private int _pending;

    public DiskWorker(string name)
    {
        _thread = new Thread(Work) { IsBackground = true, Name = name };
        _thread.Start();
    }

    /// <summary>Jobs asked for and not yet answered on the main thread.</summary>
    public bool IsBusy => Volatile.Read(ref _pending) > 0;

    public void Run<T>(Func<T> job, Action<T> answer, Action<Exception> failed)
    {
        Interlocked.Increment(ref _pending);
        _jobs.Add(() =>
        {
            Action reply;

            try
            {
                var value = job();

                reply = () => answer(value);
            }
            catch (Exception ex)
            {
                reply = () => failed(ex);
            }

            _answers.Enqueue(reply);
        });
    }

    /// <summary>Hands out the answers that came in; call on the main thread.</summary>
    public void Drain()
    {
        if (Volatile.Read(ref _pending) == 0) return;

        while (_answers.TryDequeue(out var reply))
        {
            Interlocked.Decrement(ref _pending);
            reply();
        }
    }

    /// <summary>Waits up to <paramref name="wait"/> for the jobs already asked for, so a backup made at quit is finished.</summary>
    public bool WaitIdle(TimeSpan wait)
    {
        using var done = new ManualResetEventSlim();

        try
        {
            _jobs.Add(done.Set);
        }
        catch (InvalidOperationException)
        {
            return true;
        }

        return done.Wait(wait);
    }

    public void Dispose() => _jobs.CompleteAdding();

    private void Work()
    {
        foreach (var job in _jobs.GetConsumingEnumerable())
        {
            try
            {
                job();
            }
            catch (Exception)
            {
                // A job catches its own failure; this only keeps the thread alive.
            }
        }
    }
}
