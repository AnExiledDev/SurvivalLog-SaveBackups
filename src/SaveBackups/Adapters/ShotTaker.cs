using System;
using System.IO;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Show;
using Il2CppInterop.Runtime;

namespace SaveBackups.Adapters;

/// <summary>The pictures taken with a backup: a small one for lists, and the same view at full HD for the enlarged view.</summary>
internal sealed record Shot(byte[] Small, byte[]? Large);

/// <summary>
/// A picture of the run at the moment of a backup, taken with the camera the game uses for its ending photos: the
/// world only, without the game's menus. Main thread only.
/// </summary>
internal static class ShotTaker
{
    private const int SmallWidth = 640;
    private const int SmallHeight = 360;
    private const int SmallQuality = 75;
    private const int LargeWidth = 1920;
    private const int LargeHeight = 1080;
    private const int LargeQuality = 88;

    /// <summary>
    /// Both pictures, or null and why not. The small one decides: without it there is no picture, and a large one
    /// that failed only leaves the enlarged view showing the small one.
    /// </summary>
    public static Shot? Take(out string why)
    {
        CameraManager? camera;

        try
        {
            camera = GameSingleton.Existing<BattleShowWorld>()?._CameraManager;
        }
        catch (Exception ex)
        {
            why = ex.Message;
            return null;
        }

        if (camera == null)
        {
            why = "the camera is not ready";
            return null;
        }

        var small = Capture(camera, SmallWidth, SmallHeight, SmallQuality, out why);

        if (small == null) return null;

        return new Shot(small, Capture(camera, LargeWidth, LargeHeight, LargeQuality, out _));
    }

    private static byte[]? Capture(CameraManager camera, int width, int height, int quality, out string why)
    {
        why = "";

        var temp = Path.Combine(Path.GetTempPath(), "slsavebackups-shot-" + Guid.NewGuid().ToString("N") + ".jpg");

        try
        {
            var request = new EndingPhotoRequest
            {
                PathOf = DelegateSupport.ConvertDelegate<Il2CppSystem.Func<int, string>>(new Func<int, string>(_ => temp)),
                Height = height,
                Quality = quality,
            };

            if (!camera.CapturePlayerView(request, width, height, width, height, quality) || !File.Exists(temp))
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
