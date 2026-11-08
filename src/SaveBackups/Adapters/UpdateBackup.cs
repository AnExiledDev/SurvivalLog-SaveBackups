using System;

namespace SaveBackups;

/// <summary>
/// For the Mod Manager, which finds this class by name, so neither mod needs the other. Called on the main thread right
/// before it installs mod updates.
/// </summary>
public static class UpdateBackup
{
    internal static Action<string>? Log;

    /// <summary>
    /// Backs up the run being played, or on the title screen every save, unless BeforeModUpdates is off. The copies
    /// finish on a worker thread. Returns how many saves are being backed up; 0 when none, and never throws.
    /// </summary>
    public static int BeforeModUpdates()
    {
        try
        {
            return Adapters.Hooks.Session?.ModUpdatesStarting() ?? 0;
        }
        catch (Exception ex)
        {
            Log?.Invoke("the backup before mod updates failed: " + ex);

            return 0;
        }
    }
}
