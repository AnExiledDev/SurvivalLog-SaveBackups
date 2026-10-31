# Save Backups settings

Settings live in `BepInEx\config\com.dev.survivallog.savebackups.cfg`, created the first time the game starts with the mod. Change them in game in the save manager's Settings tab or on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. Delete the file to get the defaults back.

## Controls

| Setting | Default | What it does |
| --- | --- | --- |
| `BackupKey` | `None` | Key that saves the run you are playing and backs it up, like **Back up now** in the pause menu. It works only in a run, when the game could save. `None` leaves it unbound. |

The value is a Unity key name, for example `F8`, `F11`, `Insert` or `Keypad5`.

## Backups

| Setting | Default | What it does |
| --- | --- | --- |
| `Folder` | empty | Folder the backups are kept in. Empty keeps them in the game's own folder (`LocalLow\LLS\SLGame\SaveBackups`), next to `Saves` but out of Steam Cloud. |
| `SecondFolder` | empty | A second folder that gets a copy of every backup, such as another drive or a synced folder. Empty keeps one copy. |
| `OnMorning` | `true` | Back up the run each morning, right after the game's own save. |
| `OnQuit` | `true` | Back up the run when you quit to the title screen or close the game. |
| `BeforeRebirth` | `true` | Back up the run right before a rebirth rolls it back. |
| `BeforeExpedition` | `true` | Back up the run right before the survivor sets out on an expedition. The game saves first when it can. |
| `BeforeWave` | `true` | Back up the run right before a zombie wave starts, once a day. The game saves first when it can. |
| `AtEnding` | `true` | Back up the run when it reaches an ending and the game saved your choice, so you can restore it and try another ending. |
| `OnTimer` | `false` | Also back up the run after every `TimerMinutes` minutes of play. The game saves first. |
| `TimerMinutes` | `20` | Minutes of play between timer backups, from 5 to 240. |
| `TakePictures` | `true` | Take a small picture of the game with each backup made while you play, shown in the save manager. |
| `Verify` | `true` | Read each backup back and compare it with the save before it counts as made. A copy that does not match is not kept. |

## Keep

How many backups of each kind every save keeps, and how older ones are thinned. Imported backups are never removed by these counts. When a new backup is made, older ones of the same kind go to the bin. Pinned backups are never removed and do not count. `0` keeps them all.

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
| `Timer` | `6` | Timer backups. |
| `KeepAllDays` | `3` | Every backup of the last this many days of play is kept. Older automatic backups are thinned to one a day. `0` turns thinning off. |
| `DailyDays` | `14` | After `KeepAllDays`, one backup a day is kept for this many more days of play. Thinned backups go to the bin. |
| `PerLoop` | `true` | Past `DailyDays`, keep the last backup of each loop. `false` lets the counts above take them. |
| `MaxTotalMB` | `2048` | Most space all backups and the bin together may take, in MB. Past it, the bin is emptied oldest first, then the oldest backups go, automatic ones before your own. Pinned backups and each save's newest backup are always kept. `0` means no limit. |
| `BinDays` | `30` | Days a removed backup, or a deleted save's backups, stay in the bin before they are deleted for good. `0` keeps them until you empty the bin. |

## Screen

| Setting | Default | What it does |
| --- | --- | --- |
| `ReplaceLoadScreen` | `true` | The title screen's Load button opens the save manager. Its Game's own list button opens the game's list. `false` keeps the game's list. |
| `WarnWhenBehind` | `true` | Warn on the title screen when a save is older than its last backup, as when Steam Cloud put back an older copy. |
| `ShowGameSnapshots` | `true` | Show the game's own day snapshots of a save in the save manager, where each can be loaded as a new save. |

## Toasts

| Setting | Default | What it does |
| --- | --- | --- |
| `AutoBackupToasts` | `false` | Show a message for every automatic backup, not only for the ones you make. |
