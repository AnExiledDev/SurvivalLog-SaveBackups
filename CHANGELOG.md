# Changelog

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
