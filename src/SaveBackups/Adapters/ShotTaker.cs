using System;
using System.IO;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Show;
using Il2CppInterop.Runtime;

namespace SaveBackups.Adapters;

/// <summary>
/// A small picture of the run at the moment of a backup, taken with the camera the game uses for its ending photos:
/// the world only, without the game's menus. Main thread only.
/// </summary>
internal static class ShotTaker
{
    private const int Width = 640;
    private const int Height = 360;
    private const int Quality = 75;

    /// <summary>The JPG bytes, or null and why not.</summary>
    public static byte[]? Take(out string why)
    {
        why = "";

        var temp = Path.Combine(Path.GetTempPath(), "slsavebackups-shot-" + Guid.NewGuid().ToString("N") + ".jpg");

        try
        {
            var camera = GameSingleton.Existing<BattleShowWorld>()?._CameraManager;

            if (camera == null)
            {
                why = "the camera is not ready";
                return null;
            }

            var request = new EndingPhotoRequest
            {
                PathOf = DelegateSupport.ConvertDelegate<Il2CppSystem.Func<int, string>>(new Func<int, string>(_ => temp)),
                Height = Height,
                Quality = Quality,
            };

            if (!camera.CapturePlayerView(request, Width, Height, Width, Height, Quality) || !File.Exists(temp))
            {
                why = "the game did not take it";
                return null;
            }

            return File.ReadAllBytes(temp);
        }
        catch (Exception ex)
        {
            why = ex.Message;
            return null;
        }
        finally
        {
            try
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
            catch (Exception)
            {
                // The temp folder is the system's to clean.
            }
        }
    }
}
