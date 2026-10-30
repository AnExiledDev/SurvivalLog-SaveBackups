# Changelog

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
