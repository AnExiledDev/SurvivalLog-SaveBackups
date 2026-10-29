using System.Diagnostics;

namespace SaveBackups.Adapters;

/// <summary>
/// Seconds on a clock that only moves forward, read without a call into the game: the interop getter for
/// Time.unscaledTime boxes its answer on every read, and the mod reads the time every frame.
/// </summary>
internal static class Clock
{
    private static readonly double TicksPerSecond = Stopwatch.Frequency;

    public static double Now => Stopwatch.GetTimestamp() / TicksPerSecond;
}
