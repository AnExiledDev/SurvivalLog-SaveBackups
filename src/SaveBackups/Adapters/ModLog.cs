using System;
using System.IO;
using BepInEx.Logging;
using SaveBackups.Core;

namespace SaveBackups.Adapters;

/// <summary>
/// Writes to the BepInEx log and to an append-only file that survives restarts (LogOutput.log is overwritten on the
/// next launch). The file is written on a worker. Lines about single packages and the watch's steps go to the file
/// only, so a press with many packages does not write each of them to the console too.
/// </summary>
internal sealed class ModLog
{
    private static readonly TimeSpan QuitWait = TimeSpan.FromSeconds(3);

    private readonly ManualLogSource _bepinex;
    private readonly LineWriter _file;

    public ModLog(ManualLogSource bepinex, string path)
    {
        _bepinex = bepinex;
        _file = new LineWriter(() => OpenFile(path));
    }

    /// <summary>Never throws: a logger that can fail would take down whatever it was asked to report on.</summary>
    public void Write(string message)
    {
        try
        {
            if (HookFailure.Is(message)) _bepinex.LogError(message);
            else _bepinex.LogInfo(message);
        }
        catch (Exception)
        {
            // The file copy below still has a chance.
        }

        Detail(message);
    }

    /// <summary>A line for the mod's own file only.</summary>
    public void Detail(string message) => _file.Add(DateTime.UtcNow, message);

    /// <summary>On quit, gives the worker a few seconds to write what is still queued.</summary>
    public void Finish() => _file.WaitIdle(QuitWait);

    /// <summary>Shared for reading so the log can be tailed while the game runs.</summary>
    private static TextWriter OpenFile(string path) =>
        new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
}
