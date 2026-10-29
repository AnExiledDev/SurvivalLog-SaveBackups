namespace SaveBackups.Core;

/// <summary>
/// Whether the game's hotkeys are held back: while the save manager is open, and through the frame after Esc closed it.
/// The hook asks this on every frame, so <see cref="IsHeld"/> answers without the frame number while nothing is held,
/// and the hold forgets its last frame once that has passed.
/// </summary>
public sealed class HotkeyBlock
{
    private bool _isMenuOpen;
    private int _blockedThroughFrame = -1;

    public bool IsMenuOpen => _isMenuOpen;

    /// <summary>False when no menu is open and no frame is held: the hook can return at once.</summary>
    public bool IsHeld => _isMenuOpen || _blockedThroughFrame >= 0;

    public void MenuOpened() => _isMenuOpen = true;

    /// <summary>Holds hotkeys through the next frame for the Esc that closed the menu; a menu already shut changes nothing.</summary>
    public void MenuClosed(int frame)
    {
        if (!_isMenuOpen) return;

        _isMenuOpen = false;
        _blockedThroughFrame = frame + 1;
    }

    /// <summary>Whether this frame's hotkeys are held back. Ask only while <see cref="IsHeld"/>.</summary>
    public bool IsBlocked(int frame)
    {
        if (_isMenuOpen) return true;
        if (frame <= _blockedThroughFrame) return true;

        _blockedThroughFrame = -1;

        return false;
    }
}
