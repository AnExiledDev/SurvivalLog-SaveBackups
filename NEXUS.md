Name: Save Backups
Summary: Automatic backups of every save each morning, on quit, before a rebirth, an expedition or a zombie wave, and at an ending, out of Steam Cloud's reach. The Load button opens a save manager: restore any backup, copy one as a new save, bring back deleted saves, add notes, search, and export or import backups. F8 backs up your run.

Description (BBCode):
The game keeps one copy of each save, and Steam Cloud can quietly put back an older one. Save Backups copies your save each morning, when you quit, before a rebirth, an expedition or a zombie wave, and at an ending, into a folder Steam Cloud never touches. The title screen's Load button then opens a save manager where you can see every backup of every save and put any of them back with one click.

[size=5]Main features[/size]
[list]
[*][b]Automatic backups[/b]: each morning right after the game's own save, when you quit or close the game, right before a rebirth, an expedition or a zombie wave, and when a run reaches an ending, so you can go back and pick another one. Every file of the save is copied, its rebirth day snapshots included.[/*]
[*][b]Back up any time[/b]: F8 in a run saves and backs it up. On the title screen, Back up now does the same for any save.[/*]
[*][b]Save manager[/b]: the Load button opens your saves with their backups, newest first, by loop and day. Load, Restore or Delete from there. Default list opens the game's normal Load screen.[/*]
[*][b]Nothing is lost by a restore or a delete[/b]: the save as it is gets a backup of its own first.[/*]
[*][b]Deleted saves[/b]: a deleted save keeps its backups and can be restored into your list again.[/*]
[*][b]Copy as new save[/b]: turn any backup, a deleted save's included, into a save of its own next to the original. The original is not changed.[/*]
[*][b]Notes and search[/b]: write a note on any backup, then search your saves by name, survivor or note, or show only saves in progress, finished saves or saves with backups.[/*]
[*][b]Export and import[/b]: Export writes a backup to one .slsave file you can keep or share. Put .slsave files in the SaveBackups\Import folder and press Import to add them.[/*]
[*][b]Day descriptions and version warning[/b]: morning backups keep the game's description of that day, and a backup made on another game version is marked and warns before you restore it.[/*]
[*][b]Went-backwards warning[/b]: if a save is suddenly behind its own last backup, a message says so and the save manager offers to restore it.[/*]
[*][b]Pin and limits[/b]: pinned backups are kept no matter what. Old ones are removed by a count for each kind and a total size limit (2 GB by default).[/*]
[/list]

[size=5]Hotkeys[/size]
[list]
[*][b]F8[/b]: save and back up the run you are playing.[/*]
[*][b]Esc[/b]: closes the save manager. In the search box or a note, it clears or closes that first.[/*]
[/list]
F8 can be changed or turned off in the config.

Works in English and Chinese.

Backups are kept in [font=Courier New]AppData\LocalLow\LLS\SLGame\SaveBackups[/font], next to the game's Saves folder. Steam Cloud only syncs Saves, so the backups stay on this PC. The mod adds no data of its own to your saves, and you can remove it at any time.

[size=5]Known issues[/size]
[list]
[*]Restore, delete, copy, export and import work only on the title screen, in the save manager.[/*]
[*]A copied save starts without the original's ending snapshots, so it cannot jump back to one of the original's endings.[/*]
[*]The rebirth day snapshots are backed up with the save, but the save manager restores the whole save, not a single snapshot.[/*]
[*]A save you rolled back on purpose with the game's own tools can show the went-backwards warning once; Dismiss clears it.[/*]
[/list]

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.19611. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.11.0 or newer. Save Backups does not load without it. It shows the mod's messages as toasts and applies settings changed on the Mod Manager's Mods screen without a restart.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Install Exiled's Toolkit the same way. Download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\SaveBackups.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]SaveBackups.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder. Your backups stay in the SaveBackups folder until you delete it.

[size=5]Configuration[/size]
To change the key, the backup folder or how many backups are kept, edit [font=Courier New]BepInEx\config\com.dev.survivallog.savebackups.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen, where changes take effect at once (see [url=https://github.com/AnExiledDev/SurvivalLog-SaveBackups/blob/main/CONFIG.md]CONFIG.md[/url]; the cfg file explains each setting in English and Chinese).

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\save-backups.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-SaveBackups]github.com/AnExiledDev/SurvivalLog-SaveBackups[/url]

[line]
[size=5]简体中文[/size]

游戏每个存档只保留一份，而 Steam 云同步可能会悄悄换回旧的存档。Save Backups 会在每天早上、退出时、重生前、探索前、尸潮前和到达结局时，把存档复制到 Steam 云同步不会动的文件夹里。标题画面的「读取存档」按钮会打开存档管理，列出每个存档的所有备份，一键就能恢复任意一个。

[size=5]主要功能[/size]
[list]
[*][b]自动备份[/b]：每天早上游戏自动存档之后、返回标题画面或关闭游戏时、重生之前、出发探索之前、尸潮开始之前（每天一次），以及到达结局时，之后可以回去选择别的结局。存档的所有文件都会复制，包括重生用的每日快照。[/*]
[*][b]随时备份[/b]：在游戏中按 F8 会先存档再备份。在标题画面，存档管理里的「立即备份」可以备份任意存档。[/*]
[*][b]存档管理[/b]：「读取存档」按钮会列出你的存档和它们的备份，按轮次和天数排列，最新的在前。可以在这里读取、恢复或删除。「默认列表」打开游戏原本的读取界面。[/*]
[*][b]恢复和删除不会丢东西[/b]：动手之前会先把当前存档备份一次。[/*]
[*][b]已删除的存档[/b]：删除的存档会保留备份，可以重新恢复到存档列表。[/*]
[*][b]复制为新存档[/b]：任意备份（包括已删除存档的备份）都可以变成一个独立的新存档，原存档不会改变。[/*]
[*][b]备注与搜索[/b]：可以给任意备份写备注，再按名称、角色或备注搜索存档，或只显示进行中、已结束或有备份的存档。[/*]
[*][b]导出与导入[/b]：「导出」把一个备份写成一个 .slsave 文件，方便保存或分享。把 .slsave 文件放进 SaveBackups\Import 文件夹，再点「导入」即可加入。[/*]
[*][b]每日描述与版本提醒[/b]：早上的备份会保留游戏对当天的描述；在其他游戏版本创建的备份会有标记，恢复前会提醒。[/*]
[*][b]存档回退提醒[/b]：如果存档突然比自己最近的备份还旧，会弹出提示，存档管理里也会提供恢复。[/*]
[*][b]固定与上限[/b]：固定的备份永远保留。旧备份按每种类型的数量和总大小上限（默认 2 GB）自动清理。[/*]
[/list]

[size=5]快捷键[/size]
[list]
[*][b]F8[/b]：保存并备份当前存档。[/*]
[*][b]Esc[/b]：关闭存档管理。在搜索框或备注里时，先清空或关闭它们。[/*]
[/list]
F8 可以在配置中修改或关闭。

支持英文和中文。

备份保存在 [font=Courier New]AppData\LocalLow\LLS\SLGame\SaveBackups[/font]，与游戏的 Saves 文件夹相邻。Steam 云只同步 Saves，所以备份只留在这台电脑上。模组不会往存档里写入自己的数据，你可以随时移除它。

[size=5]已知问题[/size]
[list]
[*]恢复、删除、复制、导出和导入只能在标题画面的存档管理里进行。[/*]
[*]复制出的新存档不带原存档的结局快照，所以不能跳回原存档的结局。[/*]
[*]重生用的每日快照会随存档一起备份，但存档管理只能恢复整个存档，不能单独恢复某个快照。[/*]
[*]用游戏自带功能主动回退的存档可能会提示一次存档回退，点「忽略」即可。[/*]
[/list]

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.19611。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.11.0 或更新版本。没有它本模组不会加载。它把模组的提示显示为弹出通知，并让在 Mod Manager 的「模组」界面改动的设置无需重启即可生效。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]用同样的方式安装 Exiled's Toolkit，下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\SaveBackups.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]SaveBackups.dll[/font]。备份会留在 SaveBackups 文件夹里，直到你自己删除。

[size=5]配置[/size]
可以在游戏内 Mod Manager 的「模组」界面修改，改动立即生效；也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.savebackups.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-SaveBackups/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\save-backups.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-SaveBackups]github.com/AnExiledDev/SurvivalLog-SaveBackups[/url]
