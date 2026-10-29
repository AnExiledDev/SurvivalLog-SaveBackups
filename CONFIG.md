# Save Backups settings

Settings live in `BepInEx\config\com.dev.survivallog.savebackups.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. Delete the file to get the defaults back.

## Controls

| Setting | Default | What it does |
| --- | --- | --- |
| `BackupKey` | `F8` | Key that saves the run you are playing and backs it up. It works only in a run, when the game could save. `None` turns the key off. |

The value is a Unity key name, for example `F8`, `F11`, `Insert` or `Keypad5`.

## Backups

| Setting | Default | What it does |
| --- | --- | --- |
| `Folder` | empty | Folder the backups are kept in. Empty keeps them in the game's own folder (`LocalLow\LLS\SLGame\SaveBackups`), next to `Saves` but out of Steam Cloud. |
| `OnMorning` | `true` | Back up the run each morning, right after the game's own save. |
| `OnQuit` | `true` | Back up the run when you quit to the title screen or close the game. |
| `BeforeRebirth` | `true` | Back up the run right before a rebirth rolls it back. |
| `BeforeExpedition` | `true` | Back up the run right before the survivor sets out on an expedition. The game saves first when it can. |
| `BeforeWave` | `true` | Back up the run right before a zombie wave starts, once a day. The game saves first when it can. |
| `AtEnding` | `true` | Back up the run when it reaches an ending and the game saved your choice, so you can restore it and try another ending. |

## Keep

How many backups of each kind every save keeps. Imported backups are never removed by these counts. When a new backup is made, older ones of the same kind are removed. Pinned backups are never removed and do not count. `0` keeps them all.

| Setting | Default | What it does |
| --- | --- | --- |
| `Morning` | `10` | Morning backups each save keeps. |
| `Quit` | `5` | Backups made when you quit. |
| `Rebirth` | `5` | Backups made before a rebirth. |
| `Manual` | `0` | Backups you made yourself, with the key or Back up now. `0` keeps them all. |
| `Safety` | `5` | Backups made before a restore or a delete. |
| `Expedition` | `5` | Backups made before an expedition. |
| `Wave` | `5` | Backups made before a zombie wave. |
| `Ending` | `3` | Backups made at an ending. |
| `MaxTotalMB` | `2048` | Most space all backups together may take, in MB. Past it, the oldest backups go first, automatic ones before your own. Pinned backups and each save's newest backup are always kept. `0` means no limit. |

## Screen

| Setting | Default | What it does |
| --- | --- | --- |
| `ReplaceLoadScreen` | `true` | The title screen's Load button opens the save manager. Its Default list button opens the game's list. `false` keeps the game's list. |

## Toasts

| Setting | Default | What it does |
| --- | --- | --- |
| `AutoBackupToasts` | `false` | Show a message for every automatic backup, not only for the ones you make. |
