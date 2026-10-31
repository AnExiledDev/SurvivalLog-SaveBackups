using System;

namespace SaveBackups.Core;

/// <summary>
/// How far a run has got, as the game's save list keeps it: the rebirth loop, the day (0 is the first day) and the
/// time in that day. Compared in that order.
/// </summary>
public readonly record struct Progress(int Turn, int Day, int GameTime) : IComparable<Progress>
{
    public int CompareTo(Progress other)
    {
        if (Turn != other.Turn) return Turn.CompareTo(other.Turn);
        if (Day != other.Day) return Day.CompareTo(other.Day);

        return GameTime.CompareTo(other.GameTime);
    }

    /// <summary>The time in the day as the game's clock shows it, "HH:MM"; <see cref="GameTime"/> is seconds into the day.</summary>
    public string Clock
    {
        get
        {
            var seconds = Math.Max(0, GameTime);

            return (seconds / 3600).ToString("00", System.Globalization.CultureInfo.InvariantCulture) + ":" + (seconds % 3600 / 60).ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    public static bool operator >(Progress a, Progress b) => a.CompareTo(b) > 0;

    public static bool operator <(Progress a, Progress b) => a.CompareTo(b) < 0;
}
