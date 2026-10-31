Name: Save Backups
Summary: Automatic backups of every save, out of Steam Cloud's reach, each with a picture of the game. The Load button opens a save manager with survivor portraits and a timeline: compare a backup with now, restore it as a new save or in place, and undo deletes from the bin. Back up now from the pause menu, plus export, import and a Steam Cloud check.

Description (BBCode):
The game keeps one copy of each save, and Steam Cloud can quietly put back an older one. Save Backups copies your save each morning, when you quit, before a rebirth, an expedition or a zombie wave, and at an ending, into a folder Steam Cloud never touches, with a picture of where you were. The title screen's Load button then opens a save manager where you can see every backup of every save on a timeline and put any of them back.

[size=5]Main features[/size]
[list]
[*][b]Automatic backups[/b]: each morning right after the game's own save, when you quit or close the game, right before a rebirth, an expedition or a zombie wave, when a run reaches an ending, and on a timer if you want. Every file of the save is copied and checked.[/*]
[*][b]Pictures and portraits[/b]: each backup made while you play takes a small picture of the game, and each save shows its survivor's portrait.[/*]
[*][b]Pause menu[/b]: Backups of this run opens the save manager over your run, and Back up now saves and backs it up.[/*]
[*][b]Timeline[/b]: a save's backups and the game's own day snapshots, grouped by day. Click one to see its picture, your survivor's health, fullness, mood, stamina, money and items then and how they changed, and the game's journal for that day.[/*]
[*][b]Compare with now[/b]: what changed between a backup and the save as it is now, item by item.[/*]
[*][b]Restore[/b]: load a backup as a new save next to the original, or replace the save. The save as it is gets a backup of its own first.[/*]
[*][b]Bin[/b]: removed backups and deleted saves wait 30 days in the bin, where you can put them back.[/*]
[*][b]Saves[/b]: search, sort, filter, pin, rename, write notes, duplicate a save, and turn any of the game's day snapshots into a save of its own.[/*]
[*][b]Export and import[/b]: export one backup or a whole save to a .slsave file you can keep or share. Put .slsave files or loose save files in SaveBackups\Import and press Import to see what each holds and bring it in.[/*]
[*][b]Steam Cloud check[/b]: if a save is suddenly behind its own last backup, the save manager shows both days side by side so you can keep either.[/*]
[*][b]Keeps what matters[/b]: every backup of the last 3 days of play, then one a day, then one per loop, under a 2 GB limit by default. Pinned backups are kept no matter what. A second folder can get a copy of everything.[/*]
[*][b]Settings tab[/b]: change every setting in the save manager, and it applies at once.[/*]
[/list]

[size=5]Hotkeys[/size]
[list]
[*][b]Back up key[/b]: unbound by default, since the pause menu has Back up now. Set one in the save manager's Settings tab to save and back up the run with a key.[/*]
[*][b]Esc[/b]: closes the save manager. In the search box or a note, it clears or closes that first.[/*]
[/list]

Works in English and Chinese.

Backups are kept in [font=Courier New]AppData\LocalLow\LLS\SLGame\SaveBackups[/font], next to the game's Saves folder. Steam Cloud only syncs Saves, so the backups stay on this PC. The mod adds no data of its own to your saves, and you can remove it at any time.

[size=5]Known issues[/size]
[list]
[*]In a run, the save manager can't load a save, start a new game, or restore over, rename or delete the run you are playing. Those wait for the title screen.[/*]
[*]A copied save starts without the original's ending snapshots, so it cannot jump back to one of the original's endings.[/*]
[*]Pictures are taken only while you play. Backups made on the title screen, and older ones, have none.[/*]
[*]A save you rolled back on purpose with the game's own tools can show the Steam Cloud warning once; keep the day you want and it clears.[/*]
[/list]

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.20151. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.14.0 or newer. Save Backups does not load without it. It shows the mod's messages as toasts and applies settings changed on the Mod Manager's Mods screen without a restart.[/*]
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
To change the key, the backup folder or how many backups are kept, use the save manager's Settings tab, or edit [font=Courier New]BepInEx\config\com.dev.survivallog.savebackups.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen, where changes take effect at once (see [url=https://github.com/AnExiledDev/SurvivalLog-SaveBackups/blob/main/CONFIG.md]CONFIG.md[/url]; the cfg file explains each setting in English and Chinese).

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\save-backups.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-SaveBackups]github.com/AnExiledDev/SurvivalLog-SaveBackups[/url]

[line]
[size=5]简体中文[/size]

游戏每个存档只保留一份，而 Steam 云同步可能会悄悄换回旧的存档。Save Backups 会在每天早上、退出时、重生前、探索前、尸潮前和到达结局时，把存档复制到 Steam 云同步不会动的文件夹里，并拍下当时的游戏画面。标题画面的「读取存档」按钮会打开存档管理，用时间线列出每个存档的所有备份，随时可以恢复任意一个。

[size=5]主要功能[/size]
[list]
[*][b]自动备份[/b]：每天早上游戏自动存档之后、返回标题画面或关闭游戏时、重生之前、出发探索之前、尸潮开始之前、到达结局时，也可以按游玩时间定时备份。存档的所有文件都会复制并校验。[/*]
[*][b]画面与头像[/b]：游玩时的每次备份都会拍一张游戏画面的小图，每个存档都显示角色头像。[/*]
[*][b]暂停菜单[/b]：「本局备份」在游戏中打开存档管理，「立即备份」会先存档再备份。[/*]
[*][b]时间线[/b]：存档的备份和游戏自带的每日快照按天排列。点开任意一个，可以看到当时的画面、角色的健康、饱腹、心情、体力、金钱和物品以及之后的变化，还有当天的游戏日志。[/*]
[*][b]与当前对比[/b]：逐个物品列出备份与当前存档之间的变化。[/*]
[*][b]恢复[/b]：把备份读取为原存档旁边的新存档，或直接覆盖原存档。动手之前会先把当前存档备份一次。[/*]
[*][b]回收站[/b]：移除的备份和删除的存档会在回收站里保留 30 天，期间可以找回。[/*]
[*][b]存档[/b]：搜索、排序、筛选、置顶、重命名、写备注、复制存档，还能把游戏自带的每日快照变成独立的新存档。[/*]
[*][b]导出与导入[/b]：把单个备份或整个存档导出为 .slsave 文件，方便保存或分享。把 .slsave 文件或零散的存档文件放进 SaveBackups\Import，点「导入」即可查看内容并加入。[/*]
[*][b]Steam 云检查[/b]：如果存档突然比自己最近的备份还旧，存档管理会并排显示两个天数，保留哪个由你决定。[/*]
[*][b]保留重要的备份[/b]：最近 3 个游戏日的备份全部保留，之后每天一个，再往前每个轮回一个，默认总大小不超过 2 GB。固定的备份永远保留。还可以设置第二个文件夹，再存一份副本。[/*]
[*][b]设置页[/b]：在存档管理里就能修改所有设置，立即生效。[/*]
[/list]

[size=5]快捷键[/size]
[list]
[*][b]备份键[/b]：默认不绑定，因为暂停菜单里已有「立即备份」。可以在存档管理的设置页里设置按键，用来保存并备份当前存档。[/*]
[*][b]Esc[/b]：关闭存档管理。在搜索框或备注里时，先清空或关闭它们。[/*]
[/list]

支持英文和中文。

备份保存在 [font=Courier New]AppData\LocalLow\LLS\SLGame\SaveBackups[/font]，与游戏的 Saves 文件夹相邻。Steam 云只同步 Saves，所以备份只留在这台电脑上。模组不会往存档里写入自己的数据，你可以随时移除它。

[size=5]已知问题[/size]
[list]
[*]游戏进行中，存档管理不能读取存档、开始新游戏，也不能覆盖、重命名或删除正在玩的存档，这些需回到标题画面。[/*]
[*]复制出的新存档不带原存档的结局快照，所以不能跳回原存档的结局。[/*]
[*]只有游玩时的备份会拍下画面。在标题画面做的备份以及较早的备份没有画面。[/*]
[*]用游戏自带功能主动回退的存档可能会提示一次 Steam 云检查，保留你想要的那一天即可。[/*]
[/list]

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.20151。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.14.0 或更新版本。没有它本模组不会加载。它把模组的提示显示为弹出通知，并让在 Mod Manager 的「模组」界面改动的设置无需重启即可生效。[/*]
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
可以在存档管理的设置页或游戏内 Mod Manager 的「模组」界面修改，改动立即生效；也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.savebackups.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-SaveBackups/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\save-backups.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-SaveBackups]github.com/AnExiledDev/SurvivalLog-SaveBackups[/url]
