using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveBackups.Adapters;

/// <summary>
/// The Unity object that hands frames to the session. Each distinct failure is logged once, never rethrown into the
/// game; a failure that repeats every frame would otherwise flood the log.
/// </summary>
public sealed class Ticker : MonoBehaviour
{
    private const int MaxReportedFailures = 20;

    internal static Session? Session;
    internal static Action<string> Log = _ => { };

    /// <summary>Run when the game quits, so the log lines still queued for the file are written.</summary>
    internal static Action Quit = () => { };

    private static readonly HashSet<string> ReportedFailures = new();

    public Ticker(IntPtr pointer) : base(pointer)
    {
    }

    private void OnApplicationQuit()
    {
        try
        {
            Quit();
        }
        catch (Exception)
        {
            // The game is closing; there is nowhere left to report this.
        }
    }

    private void Update()
    {
        try
        {
            Session?.Update();
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    private static void ReportFailure(Exception ex)
    {
        if (ReportedFailures.Count >= MaxReportedFailures || !ReportedFailures.Add(ex.GetType().FullName + ": " + ex.Message)) return;

        Log("update failed (each distinct failure reported once): " + ex);

        if (ReportedFailures.Count == MaxReportedFailures) Log($"{MaxReportedFailures} distinct update failures reported; further ones are not logged");
    }
}
