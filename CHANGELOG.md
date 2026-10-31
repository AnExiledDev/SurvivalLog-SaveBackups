# Changelog

## 0.3.0

- Pictures: each backup made while you play takes a small picture of the game, shown in the timeline and the save manager. `TakePictures` turns it off.
- Each save shows its survivor's portrait, the same picture as on the new game screen.
- Pause menu: 「Backups of this run」 opens the save manager over your run, and 「Back up now」 saves and backs up the run. The run you are playing can't be restored over, renamed or deleted from there; everything else works.
- The backup key is now unbound by default, since the pause menu has a button for it. Set `BackupKey` to bring it back.
- Timeline: a save's backups and the game's own day snapshots on one timeline, grouped by day, with the save as it is now on top.
- Click a backup to see its picture, its day, how your survivor stood then (health, fullness, mood, stamina, money, planning points, items) and how that changed since, the game's journal for that day, and its buttons right under the heading.
- Compare with now: what changed between a backup and the save as it is now, item by item.
- Restore asks whether to load the backup as a new save or replace the save. The save as it is gets a backup of its own first either way.
- The game's day snapshots can each be loaded as a new save. `ShowGameSnapshots` hides them.
- Saves: rename, pin to the top of the list, write a note on the save itself, duplicate, and export a whole save with all its backups. Sort by last played, most days, survivor or name, and filter to saves playing, finished, endless or needing a look.
- Import also takes loose save files, lists what each file holds before you bring it in, and marks the ones that won't import.
- Bin: removed backups and deleted saves wait in the bin for `BinDays` days (30 by default), where you can put them back or delete them for good.
- Thinning: every backup of the last 3 days of play is kept, then one a day for 14 more days, then one per loop. Thinned backups go to the bin. `KeepAllDays`, `DailyDays` and `PerLoop` set it.
- Timer backups every `TimerMinutes` minutes of play when `OnTimer` is on (off by default).
- `SecondFolder` keeps a second copy of every backup, for example on another drive.
- Each backup is read back and checked against the save before it counts. `Verify` turns that off.
- When Steam Cloud puts back an older save, the save manager shows both days side by side and lets you keep either one. `WarnWhenBehind` turns the warning off.
- A Settings tab in the save manager changes every setting, the backup key included, and they apply at once.
- A bar shows how much of the size limit the backups and the bin use.
- Made for Survival Log 1.1.20151.

## 0.2.3

- New look for the save manager, in the game's own font.
- Each save shows its survivor, a status badge, loop and day, how many backups it has and when the last one was made.
- Click a backup to see it on the right: its day, its note, its size, and the buttons for it.
- A dot by each backup shows its kind, with a key above the list.
- A bar under the list shows how much of the backup size limit is in use.
- The save details no longer call the current day "played".
- Made for Survival Log 1.1.20151.

## 0.2.2

- 「Read save file」 on the title screen now opens the save manager instead of the game's own list.
- Fixed an error at the title screen when the game had not finished reading its saves yet.
- Made for Survival Log 1.1.20151.

## 0.2.1

- The backup key no longer goes off while you type in a search box or any other text box.
- Needs Exiled's Toolkit 0.12.0 or newer.
- Made for Survival Log 1.1.20151.

## 0.2.0

- Copy as new save: any backup, a deleted save's included, can become a save of its own next to the original. The original save is not changed.
- Export a backup to a single `.slsave` file in `SaveBackups\Exports`, to keep it safe or give it to a friend.
- Import: put `.slsave` files in `SaveBackups\Import` and press 「Import」 in the save manager. Each one shows up under its save as an imported backup.
- Add a note to any backup, so you can tell them apart later.
- Search your saves by name, survivor or note, and filter the list to saves in progress, finished saves or saves with backups.
- New automatic backups right before an expedition, right before a zombie wave and when a run reaches an ending, each with its own setting and its own number kept.
- Morning backups keep the game's description of that day, and the save manager shows it under the day.
- A backup made on another game version is marked, and restoring or copying it shows a warning first.
- Click a backup to show more actions: Copy as new save, Pin, Note, Export and Remove.

## 0.1.0

- First release.
- Every save is backed up each morning, when you quit to the title screen or close the game, and right before a rebirth. Backups are kept in `SaveBackups`, next to the game's `Saves` folder, where Steam Cloud does not touch them.
- Press F8 in a run to save and back it up yourself.
- The title screen's Load button opens a save manager: load a save, back it up, delete it, or restore any of its backups.
- Before a restore or a delete, the save as it is gets a backup of its own, so the step can be undone.
- A deleted save keeps its backups under 「Deleted saves」 and can be brought back from there.
- If a save is suddenly behind its own last backup, for example after Steam Cloud put back an older copy, a message says so and the save manager offers to restore it.
- Pin a backup to keep it no matter what. Old backups are removed by count for each kind and by a total size limit.
- Made for Survival Log 1.1.19611.
