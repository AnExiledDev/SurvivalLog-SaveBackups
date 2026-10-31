# Save Backups

Automatic backups of every Survival Log save, kept out of Steam Cloud, and a save manager on the title screen to restore, copy and share any of them.

The game keeps one copy of each save, and Steam Cloud can quietly put back an older one. Save Backups copies your save each morning, when you quit, before a rebirth, an expedition or a zombie wave, and at an ending, into a folder Steam Cloud never touches. The title screen's Load button then opens a save manager where you can see every backup of every save and put any of them back with one click.

## Features

- **Automatic backups**: each morning right after the game's own save, when you quit to the title screen or close the game, right before a rebirth rolls the run back, right before an expedition, right before a zombie wave (once a day), when a run reaches an ending, and, if you turn it on, every few minutes of play. A backup copies every file of the save, its rebirth day snapshots included, and is read back and checked before it counts.
- **Pictures**: each backup made while you play takes a small picture of the game, so you can see where you were. Each save also shows its survivor's portrait.
- **Pause menu buttons**: **Backups of this run** opens the save manager over the run you are playing, and **Back up now** saves the run and backs it up.
- **Save manager**: the Load button on the title screen opens your saves, each with its survivor, loop, day and play time. Search, sort, filter, pin a save to the top, rename it or write a note on it. **Game's own list** opens the game's normal Load screen.
- **Timeline**: a save's backups and the game's own day snapshots on one timeline, grouped by day, with the save as it is now on top. Click one to see its picture, how your survivor stood then (health, fullness, mood, stamina, money, planning points and items) and what changed since, and the game's journal for that day.
- **Compare with now**: what changed between a backup and the save as it is now, item by item.
- **Restore**: load any backup as a new save next to the original, or replace the save with it. The save as it is gets a backup of its own first, and if that backup cannot be made, nothing is changed.
- **Snapshots as new saves**: any of the game's own day snapshots can become a save of its own.
- **Duplicate and export**: duplicate a save, export one backup to a `.slsave` file, or export a whole save with every backup. To bring one in, put `.slsave` files or loose save files in `SaveBackups\Import` and press **Import**: it lists what each file holds before you add it.
- **Bin**: removed backups and deleted saves wait in the bin for 30 days, where you can put them back or delete them for good.
- **Thinning**: every backup of the last 3 days of play is kept, then one a day for 14 more days, then the last one of each loop. Thinned backups go to the bin. Pinned backups are never touched, and each kind of backup also has a number kept.
- **Size limit**: all backups and the bin together stay under 2 GB by default, the bin emptied first, then the oldest automatic backups. A bar in the save manager shows how much is in use.
- **Second copy**: set a second folder, such as another drive or a synced folder, and every backup is copied there too.
- **Steam Cloud check**: if a save is behind its own last backup, for example after Steam Cloud put back an older copy, the title screen says so and the save manager shows both days side by side so you can keep either.
- **Game version warning**: a backup made on another game version is marked, and restoring it warns that it may not load as it did.
- **Settings tab**: every setting can be changed in the save manager, the backup key included, and applies at once.
- **Log file**: every backup, restore and delete is written to `BepInEx\save-backups.log`.

The save manager and messages follow the game's language, English or Simplified Chinese.

Save Backups adds no data of its own to your saves. You can remove it at any time; your saves stay as they are and the backups stay in their folder.

## Hotkeys

| Key | What it does |
| --- | --- |
| `BackupKey` (unbound) | Saves the run you are playing and backs it up, like **Back up now** in the pause menu. Works only in a run, when the game could save. Set a key in the save manager's Settings tab or in the config. |
| `Esc` | In the save manager, closes the open question, then the save manager. In the search box or a note, it clears or closes that first. |

## Where the backups are

By default in `C:\Users\<you>\AppData\LocalLow\LLS\SLGame\SaveBackups`, next to the game's `Saves` folder. Each save has a folder named after its save file, and each backup is a folder in it named after the date, time and kind of backup. **Open folder** in the save manager opens it. The `Folder` setting moves it anywhere else.

Steam Cloud only syncs `Saves`, so the backups stay on this PC. Copy the folder yourself if you want them on another one.

## Known issues and limits

- In a run, the save manager can't load a save, start a new game, or restore over, rename or delete the run you are playing. Those wait for the title screen.
- A copied save starts without the game's ending snapshots of the original, so it cannot jump back to one of the original's endings.
- A game snapshot comes back as a new save, not in place of the save it belongs to.
- Pictures are taken only while you play. Backups made on the title screen, and those from before 0.3.0, have none.
- The Steam Cloud check compares the loop and day the game's save list shows with the newest backup. A save you rolled back on purpose with the game's own tools can show it once; **Dismiss** clears it.
- Backups from earlier game versions can be restored, but the game decides whether it can still read them.

## Requirements

- Survival Log 1.1.20151. Built on this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.12.0 or newer. Save Backups does not load without it. It shows the mod's messages as toasts, sizes the save manager to the Toolkit's UI size, and applies settings changed on the Mod Manager's Mods screen without a restart.

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

Check `BepInEx\LogOutput.log` for `===== Save Backups 0.2.3 started`, followed by `backup key` and the key set (`None` when unbound). Every backup, restore and delete, and anything that went wrong, is in `BepInEx\save-backups.log`, which survives restarts.

If the pause menu has no **Back up now** button, or the Load button opens the game's list, check that `ReplaceLoadScreen` is `true`, and look in the log for a line starting with `could not hook`.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\save-backups.log`.

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/SaveBackups -c Release -p:GameDir="C:\path\to\Survival Log" -p:ToolkitDll="C:\path\to\ExiledToolkit.dll"
```

The output DLL is at `src\SaveBackups\bin\Release\SaveBackups.dll`.

The unit tests cover the backup folder (copy, restore, cleanup and refusing paths outside it), copying a backup as a new save, export and import, notes, the limits, thinning, the bin, the second copy, the stats, the went-backwards check and the save manager's messages, and need nothing from the game:

```
dotnet test tests/SaveBackups.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
