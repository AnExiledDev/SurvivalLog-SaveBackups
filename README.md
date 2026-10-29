# Save Backups

Automatic backups of every Survival Log save, kept out of Steam Cloud, and a save manager on the title screen to restore any of them.

The game keeps one copy of each save, and Steam Cloud can quietly put back an older one. Save Backups copies your save each morning, when you quit and before a rebirth, into a folder Steam Cloud never touches. The title screen's Load button then opens a save manager where you can see every backup of every save and put any of them back with one click.

## Features

- **Automatic backups**: each morning right after the game's own save, when you quit to the title screen or close the game, and right before a rebirth rolls the run back. A backup copies every file of the save, its rebirth day snapshots included.
- **Back up any time**: press `F8` in a run to save it and back it up. On the title screen, **Back up now** in the save manager does the same for any save.
- **Save manager**: the Load button on the title screen opens a list of your saves with their backups, newest first, grouped by loop and day. **Load** starts the save, **Restore** puts a backup back, **Delete save** removes the save from the game's list. **Default list** opens the game's normal Load screen.
- **Nothing is lost by a restore or a delete**: the save as it is gets a backup of its own first. If that backup cannot be made, nothing is changed.
- **Deleted saves**: a deleted save keeps its backups under **Deleted saves**, and **Restore** on one of its backups puts it back in your list.
- **Went-backwards warning**: if a save is behind its own last backup, for example after Steam Cloud or a crash put back an older copy, a message says so on the title screen and the save manager offers to restore that backup or dismiss the warning.
- **Pin and limits**: pin a backup to keep it no matter what. Older backups are removed by a count for each kind and by a total size limit (2 GB by default), oldest automatic ones first. Each save's newest backup always stays.
- **Log file**: every backup, restore and delete is written to `BepInEx\save-backups.log`.

The save manager and messages follow the game's language, English or Simplified Chinese.

Save Backups adds no data of its own to your saves. You can remove it at any time; your saves stay as they are and the backups stay in their folder.

## Hotkeys

| Key | What it does |
| --- | --- |
| `F8` | Saves the run you are playing and backs it up. Works only in a run, when the game could save; otherwise a message says to try again in a moment. |
| `Esc` | In the save manager, closes the open question, then the save manager. |

`F8` can be changed or turned off in the config.

## Where the backups are

By default in `C:\Users\<you>\AppData\LocalLow\LLS\SLGame\SaveBackups`, next to the game's `Saves` folder. Each save has a folder named after its save file, and each backup is a folder in it named after the date, time and kind of backup. **Open folder** in the save manager opens it. The `Folder` setting moves it anywhere else.

Steam Cloud only syncs `Saves`, so the backups stay on this PC. Copy the folder yourself if you want them on another one.

## Known issues and limits

- Restore and delete work only on the title screen, in the save manager.
- The game's rebirth day snapshots are backed up with the save, but the save manager restores the whole save, not a single snapshot.
- The went-backwards warning compares the loop and day the game's save list shows with the newest backup. A save you rolled back on purpose with the game's own tools can show it once; **Dismiss** clears it.
- Backups from earlier game versions can be restored, but the game decides whether it can still read them.

## Requirements

- Survival Log 1.1.19611. Built on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.11.0 or newer. Save Backups does not load without it. It shows the mod's messages as toasts, sizes the save manager to the Toolkit's UI size, and applies settings changed on the Mod Manager's Mods screen without a restart.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Download `SaveBackups-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-SaveBackups/releases/latest).
4. Extract the zip into the game folder. It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\SaveBackups.dll`

## Uninstall

Delete `SaveBackups.dll` from the `BepInEx\plugins` folder. Your backups stay in `SaveBackups` until you delete that folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.savebackups.cfg` and `BepInEx\save-backups.log`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.savebackups.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. Each setting in the cfg file is explained in English and Simplified Chinese. See [CONFIG.md](CONFIG.md) for every setting.

## Troubleshooting

Check `BepInEx\LogOutput.log` for `===== Save Backups 0.1.0 started`, followed by `backup key F8`. Every backup, restore and delete, and anything that went wrong, is in `BepInEx\save-backups.log`, which survives restarts.

If the Load button opens the game's list, check that `ReplaceLoadScreen` is `true`, and look in the log for a line starting with `could not hook`.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\save-backups.log`.

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/SaveBackups -c Release -p:GameDir="C:\path\to\Survival Log" -p:ToolkitDll="C:\path\to\ExiledToolkit.dll"
```

The output DLL is at `src\SaveBackups\bin\Release\SaveBackups.dll`.

The unit tests cover the backup folder (copy, restore, cleanup and refusing paths outside it), the limits, the went-backwards check and the save manager's messages, and need nothing from the game:

```
dotnet test tests/SaveBackups.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
