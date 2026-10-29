using GameCore.HotUpdate;
using GameCore.HotUpdate.ReduxUI;
using GameCore.Scripts;

namespace SaveBackups.Adapters;

/// <summary>
/// Sends the keyboard to the web UI while the manager's search box or note box has focus, the way the game's own
/// crafting search does, and gives it back as it was found. The game keeps the system's input method (for typing
/// Chinese) off outside its own text boxes, so it is turned on while a box has focus and off again after.
/// </summary>
internal sealed class WebKeyboard
{
    private bool? _wasEnabled;

    /// <summary>True while a page box holds the keyboard, so its keys, Esc included, are the page's to handle.</summary>
    public bool IsClaimed => _wasEnabled != null;

    public void Claim()
    {
        var layer = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer();

        if (layer == null) return;

        _wasEnabled ??= layer.IsKeyboardEnabled;
        layer.SetKeyboardEnabled(true);
        ImeGuard.SetEnabled(true);
    }

    public void Release()
    {
        if (_wasEnabled is not { } wasEnabled) return;

        _wasEnabled = null;
        GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.SetKeyboardEnabled(wasEnabled);
        ImeGuard.SetEnabled(false);
    }
}
