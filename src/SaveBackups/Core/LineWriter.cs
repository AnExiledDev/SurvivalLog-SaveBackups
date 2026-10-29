using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SaveBackups.Core;

/// <summary>
/// Writes log lines to a file on a worker thread, so a press that logs dozens of lines does not wait on the disk. The
/// caller only queues the line with the time it happened; the worker stamps it in local time, writes every line
/// waiting, and flushes once per batch. A write that fails drops the writer and the next batch opens a fresh one. The
/// worker never logs anything itself.
/// </summary>
public sealed class LineWriter
{
    private readonly Func<TextWriter> _open;
    private readonly object _gate = new();
    private readonly Queue<(DateTime At, string Text)> _waiting = new();
    private readonly List<(DateTime At, string Text)> _batch = new();
    private Thread? _worker;
    private TextWriter? _file;
    private bool _isWriting;

    public LineWriter(Func<TextWriter> open)
    {
        _open = open;
    }

    /// <summary>Queues one line; <paramref name="at"/> is UTC, so the caller never reads the time zone.</summary>
    public void Add(DateTime at, string text)
    {
        lock (_gate)
        {
            _waiting.Enqueue((at, text));
            _worker ??= StartWorker();
            Monitor.PulseAll(_gate);
        }
    }

    /// <summary>Waits until every queued line is written, for at most <paramref name="timeout"/>. False when time ran out.</summary>
    public bool WaitIdle(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        lock (_gate)
        {
            while (_waiting.Count > 0 || _isWriting)
            {
                var left = deadline - DateTime.UtcNow;

                if (left <= TimeSpan.Zero || !Monitor.Wait(_gate, left)) return false;
            }
        }

        return true;
    }

    /// <summary>The time and text of one line as it lands in the file.</summary>
    public static string Format(DateTime atUtc, string text) => $"{atUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff} {text}";

    private Thread StartWorker()
    {
        var worker = new Thread(WriteForever) { IsBackground = true, Name = "SaveBackups log" };

        worker.Start();

        return worker;
    }

    private void WriteForever()
    {
        while (true)
        {
            TakeBatch();
            WriteBatch();
        }
    }

    private void TakeBatch()
    {
        lock (_gate)
        {
            _isWriting = false;
            Monitor.PulseAll(_gate);

            while (_waiting.Count == 0) Monitor.Wait(_gate);

            _batch.Clear();
            _batch.AddRange(_waiting);
            _waiting.Clear();
            _isWriting = true;
        }
    }

    private void WriteBatch()
    {
        try
        {
            _file ??= _open();

            foreach (var (at, text) in _batch) _file.WriteLine(Format(at, text));

            _file.Flush();
        }
        catch (Exception)
        {
            DropFile();
        }
    }

    private void DropFile()
    {
        try
        {
            _file?.Dispose();
        }
        catch (Exception)
        {
            // A writer that failed to write can fail to close too; it is being thrown away either way.
        }

        _file = null;
    }
}
