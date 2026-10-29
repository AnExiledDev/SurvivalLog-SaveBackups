namespace SaveBackups.Core;

/// <summary>The mod's settings as one value, swapped whole when any of them changes.</summary>
public sealed record Settings(
    string Root,
    bool OnMorning,
    bool OnQuit,
    bool BeforeRebirth,
    bool ReplaceLoadScreen,
    bool AutoBackupToasts,
    Limits Limits);
