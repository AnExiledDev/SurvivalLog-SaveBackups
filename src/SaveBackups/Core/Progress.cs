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

    public static bool operator >(Progress a, Progress b) => a.CompareTo(b) > 0;

    public static bool operator <(Progress a, Progress b) => a.CompareTo(b) < 0;
}
