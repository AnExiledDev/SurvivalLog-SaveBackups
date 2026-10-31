namespace SaveBackups.Core;

/// <summary>The mod's settings as one value, swapped whole when any of them changes.</summary>
public sealed record Settings(
    string Root,
    bool OnMorning,
    bool OnQuit,
    bool BeforeRebirth,
    bool BeforeExpedition,
    bool BeforeWave,
    bool AtEnding,
    bool ReplaceLoadScreen,
    bool AutoBackupToasts,
    Limits Limits)
{
    /// <summary>The Folder setting as typed: empty means the default folder, which <see cref="Root"/> then holds.</summary>
    public string FolderSetting { get; init; } = "";

    /// <summary>A backup every <see cref="TimerMinutes"/> minutes of play.</summary>
    public bool OnTimer { get; init; }

    public int TimerMinutes { get; init; } = 20;

    /// <summary>A second copy of every backup in this folder; empty for none.</summary>
    public string SecondFolder { get; init; } = "";

    public bool TakePictures { get; init; } = true;

    /// <summary>Read every copy back and compare it with the save before the backup counts as made.</summary>
    public bool Verify { get; init; } = true;

    public bool WarnWhenBehind { get; init; } = true;

    public bool ShowGameSnapshots { get; init; } = true;

    /// <summary>Days the recycle bin keeps what is put in it; 0 keeps it until emptied.</summary>
    public int BinDays { get; init; } = 30;
}
