// Save Backups' web half. Runs in Root.html, the document every game page iframe sits in. Draws the save manager over
// the title screen (or over a run, from the pause menu) when the plugin opens it. The pause menu's "Backups of this
// run" and "Back up now" buttons are placed by Exiled's Toolkit, from the plugin. Talks to the plugin through Exiled's
// Toolkit's page API ("slmod|savebackups|<verb>") and is driven by window.__slsavebackups. Every step that changes a
// save waits for the plugin: the page takes no clicks until the plugin sends the new view (show) or says the step is
// over (done). The look follows the Mod Manager's window: ink grid, corner brackets, display tabs, mono labels.
(function () {
  if (window.__slsavebackups) return;

  var PREFIX = 'slmod|savebackups|';
  var ROOT_ID = 'slsb-root';
  var WIN_ID = 'slsb';
  var DOT = ' \u00b7 ';

  // Every word the save manager shows, English then Simplified Chinese. {0}, {1} are filled in.
  var TEXT = {
    tabSaves: ['Saves', '存档'],
    tabBin: ['Trash', '回收站'],
    tabSettings: ['Settings', '设置'],
    deco: ['// Save Backups', '// 存档备份'],
    decoSaves: ['{0} saves', '{0} 个存档'],
    decoBackups: ['{0} backups', '{0} 个备份'],
    decoPlaying: ['playing {0}', '正在游玩 {0}'],
    close: ['close', '关闭'],

    search: ['Search saves, survivors, notes', '搜索存档、角色、备注'],
    filters: { all: ['All', '全部'], live: ['In progress', '进行中'], end: ['Finished', '已结束'], warn: ['Needs a look', '需要查看'] },
    sorts: { played: ['Last played', '最近游玩'], days: ['Most days', '天数最多'], survivor: ['Survivor', '角色'], name: ['Name', '名称'] },
    grpPinned: ['Pinned', '置顶'],
    grpSaves: ['Saves', '存档'],
    grpSort: ['Sort: {0}', '排序：{0}'],
    newGame: ['+ New game', '+ 新游戏'],
    noMatch: ['No saves match.', '没有符合条件的存档。'],
    pip: { playing: ['Playing', '游玩中'], ending: ['Ending', '结局'], check: ['Check', '待查看'], endless: ['Endless', '无尽'], none: ['None', '无'] },
    storage: ['Backups', '备份'],
    storageOf: ['{0} of {1}', '{0} / {1}'],
    storageSaves: ['Your saves {0}', '存档本身 {0}'],
    storageBin: ['Trash {0}', '回收站 {0}'],

    day: ['Day {0}', '第 {0} 天'],
    dayAt: ['Day {0}, {1}', '第 {0} 天 {1}'],
    loopDay: ['Loop {0}, day {1}', '第 {0} 轮第 {1} 天'],
    loopDayAt: ['Loop {0}, day {1}, {2}', '第 {0} 轮第 {1} 天 {2}'],
    loop: ['Loop {0}', '第 {0} 轮'],
    played: ['Played {0}', '游玩 {0}'],
    lastPlayed: ['Last played {0}', '上次游玩 {0}'],
    game: ['Game {0}', '游戏版本 {0}'],
    withBackups: ['{0} with backups', '含备份 {0}'],
    endless: ['Endless', '无尽'],

    safe: ['Backed up', '已备份'],
    safeSub: ['Newest: {0} ({1})', '最新：{0}（{1}）'],
    noneYet: ['No backups yet', '还没有备份'],
    noneYetSub: ['The first one is made tomorrow morning', '明天早上会自动备份第一次'],
    wentBack: ['Went back in time', '存档变旧了'],
    wentBackSub: ['Your newest backup is ahead of it', '你的最新备份比它新'],
    playingNow: ['Playing now', '正在游玩'],

    cont: ['Continue', '继续'],
    seeEnding: ['See the ending', '查看结局'],
    backUp: ['Back up now', '立即备份'],
    rename: ['Rename', '改名'],
    pin: ['Pin', '置顶'],
    unpin: ['Unpin', '取消置顶'],
    duplicate: ['Duplicate', '复制'],
    exportBtn: ['Export', '导出'],
    openFolder: ['Open folder', '打开文件夹'],
    del: ['Delete', '删除'],
    notePh: ['Add a note to this save', '给这个存档写个备注'],
    playingHint: ['This save is open in your game right now. You can back it up or copy it. To load, replace or delete it, quit to the title screen first.', '这个存档正在游戏中运行。现在可以备份或复制它；要读取、替换或删除，请先退回标题画面。'],
    otherHint: ['To load a different save, quit to the title screen first.', '要读取其他存档，请先退回标题画面。'],

    behindTitle: ['This save went back in time.', '这个存档变旧了。'],
    behindBody: ['It is at {0} now, but your backup from {1} is at {2}. That is what Steam Cloud putting back an older copy, or a crash, looks like. The newer copy is safe in your backups.', '它现在在{0}，但你 {1} 的备份在{2}。Steam 云放回旧存档或者游戏崩溃时就会这样。较新的那份在备份里，很安全。'],
    lookBoth: ['Look at both', '对比两者'],
    dismiss: ['Dismiss', '忽略'],

    tabBackups: ['Backups', '备份'],
    tabSnaps: ['Day snapshots', '每日快照'],
    tabPhotos: ['Ending photos', '结局照片'],
    bkFilter: { all: ['All', '全部'], auto: ['Automatic', '自动'], mine: ['Yours', '手动'], kept: ['Kept forever', '永久保留'] },
    snapsIntro: ['The game keeps these so a rebirth can go back to them. Any of them can become a save of its own.', '游戏保存这些快照供重生时回到过去。每一个都可以变成一个新存档。'],
    photosIntro: ['The game takes these at each ending this save reached.', '这个存档每到达一个结局，游戏都会拍下照片。'],
    nowRow: ['The save right now', '存档现在的样子'],
    newest: ['Newest', '最新'],
    older: ['Older game', '旧版本'],
    noKind: ['Nothing here yet.', '这里还没有。'],

    howTitle: ['How backups work', '备份是怎么工作的'],
    how1: ['Automatic', '自动备份'],
    how1b: ['Every morning, when you quit, and before a rebirth, an expedition or a zombie wave.', '每天早上、退出时，以及重生、出门探索和尸潮之前都会备份。'],
    how2: ['Out of Steam Cloud\'s reach', '不受 Steam 云影响'],
    how2b: ['They live in their own folder, so an older copy from the cloud can never replace them.', '备份放在单独的文件夹里，云端的旧存档永远不会覆盖它们。'],
    how3: ['Restore any of them', '随时恢复'],
    how3b: ['As a new save next to this one, or over it. You can always come back.', '可以恢复为新存档，也可以覆盖这个存档，之后随时都能回来。'],
    howKey: ['You can also press {0} in a run, or use Back up now in the pause menu.', '游戏中也可以按 {0}，或者在暂停菜单里点「立即备份」。'],
    howPause: ['You can also use Back up now in the pause menu.', '游戏中也可以在暂停菜单里点「立即备份」。'],

    restore: ['Restore', '恢复'],
    compare: ['Compare with now', '与现在对比'],
    keep: ['Keep forever', '永久保留'],
    unkeep: ['Kept forever', '已永久保留'],
    noteBtn: ['Note', '备注'],
    playAsNew: ['Play as a new save', '作为新存档游玩'],
    noPicture: ['No picture', '没有截图'],
    noPictureWhy: ['Pictures are taken only for backups made while you play.', '只有游玩时做的备份才会截图。'],
    thisIsNow: ['This is your save as it is right now.', '这是存档现在的样子。'],
    atThis: ['At this backup', '备份时的状态'],
    atNow: ['Right now', '现在的状态'],
    atSnap: ['At this snapshot', '快照时的状态'],
    nowVal: ['now {0}', '现在 {0}'],
    madeAt: ['Made {0}', '备份于 {0}'],
    madeAuto: ['automatic', '自动'],
    madeYou: ['made by you', '手动'],
    byGame: ['kept by the game', '由游戏保存'],
    oldWarn: ['Made on game version {0}, and you are playing {1}. It may not load as it did.', '这是游戏版本 {0} 时的备份，当前版本是 {1}，读取后可能和原来不一样。'],
    st: { life: ['Life', '生命'], satiety: ['Satiety', '饱腹'], mood: ['Morale', '心态'], stamina: ['Stamina', '体力'], planning: ['Planning Points', '信念点'], survival: ['Survival Points', '生存点'], items: ['Items', '物品'], day: ['Day', '天数'] },

    kinds: {
      now: ['Now', '现在'],
      morning: ['Morning', '早上'],
      quit: ['Quit', '退出'],
      rebirth: ['Before rebirth', '重生前'],
      manual: ['Made by you', '手动备份'],
      safety: ['Before a restore or delete', '恢复或删除前'],
      expedition: ['Before an expedition', '探索前'],
      wave: ['Before a zombie wave', '尸潮前'],
      ending: ['Ending', '结局'],
      imported: ['Imported', '导入'],
      timer: ['Timer', '定时'],
      update: ['Before mod updates', '模组更新前'],
      snap: ['Day snapshot', '每日快照'],
      round: ['Start of the loop', '本轮开始']
    },
    to: ['to {0}', '去{0}'],

    cancel: ['Cancel', '取消'],
    closeBtn: ['Close', '关闭'],
    save: ['Save', '保存'],
    restoreTitle: ['Restore a backup', '恢复备份'],
    restoreNow: ['Now', '现在'],
    asNew: ['As a new save', '作为新存档'],
    safest: ['Safest', '最安全'],
    asNewBody: ['A copy from {0} is added to your saves. {1} stays exactly as it is.', '把{0}的副本加到存档列表里，「{1}」保持原样。'],
    replace: ['Replace this save', '替换这个存档'],
    replaceBody: ['The save goes back to {0}. The save as it is now, at {1}, is backed up first, so you can come back to it from this list.', '存档回到{0}。现在的存档（{1}）会先备份，以后可以从列表里回来。'],
    replaceBlocked: ['Not while you are playing it. Quit to the title screen first.', '正在游玩时不能替换，请先回到标题画面。'],
    doAsNew: ['Add as a new save', '加为新存档'],
    doReplace: ['Go back to {0}', '回到{0}'],

    compareTitle: ['Compare with now', '与现在对比'],
    then: ['Then', '当时'],
    nowCol: ['Now', '现在'],
    change: ['Change', '变化'],
    same: ['same', '不变'],
    itemsChanged: ['What changed in your storage since then', '从那以后储物的变化'],
    gained: ['Gained since', '之后多了'],
    lost: ['Used or lost since', '之后用掉或丢了'],
    noItemChange: ['No change in items.', '物品没有变化。'],

    cloudTitle: ['A save went back in time', '存档变旧了'],
    cloudBody: ['<b>{0}</b> is older than your newest backup of it. Steam Cloud or another PC may have put back an older copy.', '「{0}」比它的最新备份还旧，可能是 Steam 云或别的电脑放回了旧存档。'],
    onDisk: ['On disk now', '当前存档'],
    newestBackup: ['Your newest backup', '你的最新备份'],
    written: ['written {0}', '写入于 {0}'],
    made: ['made {0}', '备份于 {0}'],
    cloudFoot: ['Whichever you pick, the other one stays in your backups.', '无论选哪个，另一个都会留在备份里。'],
    keepDisk: ['Keep {0}', '保留{0}'],
    useBackup: ['Use {0}', '使用{0}'],

    exportTitle: ['Export', '导出'],
    exportBody: ['Export <b>{0}</b> as one .slsave file. Move it to another PC, keep it somewhere safe, or send it with a bug report.', '把「{0}」导出为一个 .slsave 文件，可以拿到别的电脑、妥善保存，或随问题报告一起发送。'],
    exportSave: ['The save as it is now', '存档现在的样子'],
    exportAll: ['All {0} backups', '全部 {0} 个备份'],
    exportAllSize: ['{0} instead of {1}', '{0}，只导出存档为 {1}'],
    exportExtras: ['Pictures and your notes', '截图和备注'],
    savedTo: ['Saved to', '保存到'],

    importBtn: ['Import', '导入'],
    importTitle: ['Import a save', '导入存档'],
    importBody: ['Put a <b>.slsave</b> file, or a loose <b>Save_*.bytes</b> file from another PC, in this folder, then pick it here.', '把 <b>.slsave</b> 文件或别的电脑上的 <b>Save_*.bytes</b> 存档放进这个文件夹，再在这里选择。'],
    importLoose: ['A loose save file', '单独的存档文件'],
    importBad: ['Could not be read', '无法读取'],
    importPoints: ['{0} backups', '{0} 个备份'],
    importFoot: ['It is added as a new save. Nothing you have is replaced.', '会作为新存档加入，不会替换已有的存档。'],
    lookAgain: ['Look again', '刷新'],

    deleteTitle: ['Delete this save?', '删除这个存档？'],
    deleteBody: ['<b>{0}</b> and its {1} backups go to the trash. You can put them back for {2} days.', '「{0}」和它的 {1} 个备份会移到回收站，{2} 天内都可以还原。'],
    removeTitle: ['Delete this backup?', '删除这个备份？'],
    removeBody: ['{0} ({1}) goes to the trash. You can put it back for {2} days.', '{0}（{1}）会移到回收站，{2} 天内都可以还原。'],
    renameTitle: ['Rename', '改名'],
    noteTitle: ['Note on this backup', '这个备份的备注'],
    noteHint: ['A few words to tell it apart. Leave it empty to remove the note.', '写几个字方便区分。留空就是删除备注。'],

    binIntro: ['Deleted saves and removed backups wait here for {0} days, then they are gone for good. Backups that get thinned out land here too.', '删除的存档和备份会在这里保留 {0} 天，之后永久删除。被精简掉的备份也会先放到这里。'],
    binSaves: ['Deleted saves', '已删除的存档'],
    binBackups: ['Backups', '备份'],
    binEmpty: ['The trash is empty.', '回收站是空的。'],
    putBack: ['Put back', '还原'],
    deleteNow: ['Delete now', '立即删除'],
    emptyBin: ['Empty the trash ({0})', '清空回收站（{0}）'],
    emptyTitle: ['Empty the trash?', '清空回收站？'],
    emptyBody: ['Everything in the trash is deleted for good: {0}.', '回收站里的所有内容将被永久删除：{0}。'],
    daysLeft: ['{0} days left', '还剩 {0} 天'],
    thinned: ['thinned out', '已精简'],
    deletedWhy: ['deleted', '已删除'],
    goneRun: ['Putting it back brings its newest backup back into your saves.', '还原后，它的最新备份会回到存档列表。'],
    backupsN: ['{0} backups', '{0} 个备份'],

    setNav: [
      ['When to back up', '何时备份'],
      ['How long to keep them', '保留多久'],
      ['Where they go', '保存位置'],
      ['Extras', '其他']
    ],
    setNavSub: [
      ['{0} of 7 on', '已开启 {0} / 7'],
      ['{0} days, then daily', '{0} 天全留，之后每天一个'],
      ['Default folder', '默认文件夹'],
      ['Key, pictures, messages', '按键、截图、提示']
    ],
    setWhenEx: ['Each kind keeps its own number, so lots of expeditions never push out your mornings. 0 keeps them all.', '每种备份单独计数，探索再多也不会挤掉早上的备份。0 表示全部保留。'],
    keepN: ['Keep', '保留'],
    always: ['Always', '始终'],
    every: ['every', '每'],
    minutes: ['minutes', '分钟'],
    trig: {
      morning: ['Every morning', '每天早上', 'When a new day starts, right after the game saves.', '新的一天开始、游戏存档之后。'],
      expedition: ['Before each expedition', '每次探索前', 'When you leave home for a map.', '离家前往地图时。'],
      wave: ['Before a zombie wave', '尸潮前', 'Once a day, the last save before it hits.', '每天一次，尸潮来临前的最后一次存档。'],
      rebirth: ['Before a rebirth', '重生前', 'The next loop never writes over it.', '下一轮不会覆盖它。'],
      ending: ['At an ending', '到达结局时', 'So you can go back and try another ending.', '方便回去尝试其他结局。'],
      quit: ['When you quit', '退出时', 'To the title screen or out of the game.', '回到标题画面或退出游戏时。'],
      timer: ['On a timer', '定时备份', 'Real minutes, counted only while you play.', '现实时间，只在游玩时计时。'],
      update: ['Before mod updates', '模组更新前', 'When the Mod Manager installs updates.', 'Mod Manager 安装更新时。'],
      manual: ['Your own backups', '手动备份', 'Back up now, here or in the pause menu.', '这里或暂停菜单里的「立即备份」。'],
      safety: ['Before a restore or delete', '恢复或删除前', 'So any step here can be undone.', '这里的任何操作都能撤销。']
    },
    ladderEx: ['Old backups thin out on their own, so the folder never fills up but you can still go far back. Days count days of play, not real days.', '旧备份会自动精简，文件夹不会被塞满，也依然能回到很久以前。这里的天数是游戏天数，不是现实天数。'],
    lad1a: ['Keep everything from the last', '保留最近'],
    lad1b: ['days of play', '天的全部备份'],
    lad2a: ['Then one a day for', '之后每天保留一个，持续'],
    lad2b: ['more days', '天'],
    lad3a: ['Then the last one of each loop', '再往前，每轮保留最后一个'],
    lad3b: ['forever', '永久'],
    ladKept: ['Backups you keep forever are never thinned out or removed.', '永久保留的备份不会被精简或删除。'],
    ladNow: ['Today', '今天'],
    ladOld: ['Older', '更早'],
    maxMb: ['Space limit for all backups', '所有备份的空间上限', 'When it is full the trash is emptied first, then the oldest automatic backups go. 0 for no limit.', '满了以后先清空回收站，再删除最旧的自动备份。0 表示不限。'],
    binDays: ['The trash keeps things for', '回收站保留', '0 keeps them until you empty it.', '0 表示保留到你手动清空。'],
    days: ['days', '天'],
    mb: ['MB', 'MB'],
    folder: ['Backup folder', '备份文件夹', 'Outside the game\'s Saves folder, so Steam Cloud never touches it. Leave it empty for the default.', '在游戏的存档文件夹之外，Steam 云不会碰它。留空就是默认位置。'],
    second: ['Second copy', '第二份副本', 'Every backup is copied here too. Another drive, a USB stick, OneDrive or Dropbox. Leave it empty for none.', '每个备份也复制一份到这里。可以是另一块硬盘、U 盘、OneDrive 或 Dropbox。留空就是不复制。'],
    setPath: ['Set', '设置'],
    cloudNote: ['Steam Cloud only syncs the game\'s Saves folder, so your backups stay on this PC. Set a second copy if you want them somewhere else too.', 'Steam 云只同步游戏的存档文件夹，所以备份只在这台电脑上。想在别处也留一份，就设置第二份副本。'],
    keyRow: ['Back up now key', '立即备份按键', 'Saves the run and backs it up. Click, then press a key. Backspace clears it.', '保存当前这局并备份。点击后按下按键，退格键清除。'],
    keyNone: ['None', '无'],
    keyPress: ['Press a key', '请按键'],
    pics: ['Take a picture with each backup', '每次备份时截图', 'Only for backups made while you play.', '只限游玩时做的备份。'],
    verify: ['Check every backup after writing it', '写入后检查每个备份', 'Reads it back, so a broken copy never counts as a backup.', '回读一遍，坏掉的副本不会被当成备份。'],
    warnBehind: ['Warn me when a save goes back in time', '存档变旧时提醒我', 'For example after Steam Cloud puts back an older copy.', '比如 Steam 云放回了旧存档。'],
    snapsRow: ['Show the game\'s day snapshots', '显示游戏的每日快照', 'Adds the Day snapshots tab to each save.', '在每个存档里显示「每日快照」标签。'],
    replaceScreen: ['Open the save manager from the Load button', '点击「读取」时打开存档管理', 'Off opens the default Load screen.', '关闭后打开默认的读取画面。'],
    toasts: ['A message for each automatic backup', '每次自动备份时显示提示', '', ''],
    uiScale: ['Window size', '窗口大小', 'From 75 to 200. 0 follows the UI size set in Exiled\'s Toolkit.', '范围 75 到 200。0 表示跟随 Exiled\'s Toolkit 的界面大小。'],
    percent: ['%', '%'],

    footOk: ['All {0} saves are backed up. Newest backup: {1}, {2}.', '全部 {0} 个存档都有备份。最新备份：{1}，{2}。'],
    footSome: ['{0} of {1} saves are backed up. Newest backup: {2}, {3}.', '{1} 个存档中有 {0} 个已备份。最新备份：{2}，{3}。'],
    footWarn: ['{0} save needs a look.', '有 {0} 个存档需要查看。'],
    footRun: ['To load a save or start a new game, quit to the title screen first.', '要读取存档或开始新游戏，请先退回标题画面。'],
    defaultLoad: ['Default Load screen', '默认读取画面'],

    toastBackup: ['Backed up: {0}', '已备份：{0}'],
    toastDone: ['Done: {0}', '完成：{0}'],
    ago: { now: ['just now', '刚刚'], min: ['{0} min ago', '{0} 分钟前'], hour: ['{0} h ago', '{0} 小时前'], day: ['yesterday', '昨天'], days: ['{0} days ago', '{0} 天前'] },
    noRuns: ['No saves yet.', '还没有存档。'],
    noPictureSnap: ['The game keeps no picture with its snapshots.', '游戏的快照没有截图。'],
    reading: ['Reading the save...', '正在读取存档...'],
    untilEmptied: ['Kept until you empty the trash', '保留到你清空回收站'],
    binIntroKeep: ['Deleted saves and removed backups wait here until you empty the trash. Backups that get thinned out land here too.', '删除的存档和备份会一直留在这里，直到你清空回收站。被精简掉的备份也会先放到这里。'],
    openNow: ['Open the backup folder', '打开备份文件夹', '', ''],
    footNone: ['No backups yet. The first ones are made the next time you play.', '还没有备份。下次游玩时会自动开始备份。'],
    deleteBodyKeep: ['<b>{0}</b> and its {1} backups go to the trash. You can put them back until you empty it.', '「{0}」和它的 {1} 个备份会移到回收站，清空回收站之前都可以还原。'],
    removeBodyKeep: ['{0} ({1}) goes to the trash. You can put it back until you empty it.', '{0}（{1}）会移到回收站，清空回收站之前都可以还原。'],
    deleteForGood: ['{0} and all its backups are deleted for good.', '「{0}」和它的所有备份将被永久删除。'],
    looking: ['Looking in the folder...', '正在查看文件夹...'],
    importNone: ['Nothing in the folder yet.', '文件夹里还没有文件。'],
    playTime: ['{0} h {1} m', '{0} 小时 {1} 分']
  };

  var CSS = `
#slsb-root { position: fixed; inset: 0; z-index: 2147481000; background: rgba(0,0,0,0.6); animation: slsb-fade .2s ease; }
@keyframes slsb-fade { from { opacity: 0; } }
#slsb-root .stage { position: absolute; left: 50%; top: 50%; width: 1920px; height: 1080px; display: flex; align-items: center; justify-content: center; transform-origin: center center; }
#slsb.busy { cursor: progress; }
#slsb.busy * { pointer-events: none; }
#slsb .keycap { font-family: var(--mono); }
#slsb-root .lightbox { position: fixed; inset: 0; z-index: 10; background: rgba(0,0,0,0.9); display: grid; place-items: center; cursor: zoom-out; animation: lb-in .15s ease-out; }
#slsb-root .lightbox img { width: min(92vw, calc(88vh * 16 / 9)); max-height: 88vh; object-fit: contain; border: 1px solid #333; border-radius: 3px; box-shadow: 0 20px 60px rgba(0,0,0,0.7); animation: lb-zoom .18s ease-out; }
#slsb-root .lightbox .lb-x { position: fixed; top: 14px; right: 22px; font-size: 2rem; color: #bbb; line-height: 1; font-family: Consolas, monospace; }
@keyframes lb-in { from { opacity: 0; } }
@keyframes lb-zoom { from { transform: scale(.94); } }
#slsb { --amber: #EBA308; --blue: #4fa8ff; --teal: #8fc7bf; --red: #ff5a5a; --gold: #FFC83A; --ink: #121214; --line: rgba(255,255,255,0.08);
  --mono: Consolas, "Cascadia Mono", monospace; --display: Impact, "Arial Black", "Microsoft YaHei", sans-serif; --body: "Microsoft YaHei", "Noto Sans SC", "Segoe UI", sans-serif;
  position: relative; width: 1640px; height: 940px; display: grid; grid-template-rows: 70px 1fr 56px; font-family: var(--body); color: #d6d6d6; user-select: none;
  background: var(--ink); background-image: linear-gradient(rgba(255,255,255,0.02) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.02) 1px, transparent 1px); background-size: 20px 20px;
  border: 1px solid #3a3a3a; box-shadow: 0 30px 80px rgba(0,0,0,0.85), 0 0 0 1px rgba(255,255,255,0.05); }
#slsb *, #slsb *::before, #slsb *::after { box-sizing: border-box; margin: 0; padding: 0; }
#slsb button, #slsb input, #slsb textarea { font-family: inherit; }
#slsb .corner { position: absolute; width: 18px; height: 18px; border-color: #777; border-style: solid; pointer-events: none; z-index: 5; }
#slsb .tl { top: -1px; left: -1px; border-width: 2px 0 0 2px; } #slsb .tr { top: -1px; right: -1px; border-width: 2px 2px 0 0; }
#slsb .bl { bottom: -1px; left: -1px; border-width: 0 0 2px 2px; } #slsb .br { bottom: -1px; right: -1px; border-width: 0 2px 2px 0; }
#slsb ::-webkit-scrollbar { width: 6px; height: 6px; }
#slsb ::-webkit-scrollbar-thumb { background: rgba(235,163,8,0.35); border-radius: 3px; }
#slsb ::-webkit-scrollbar-track { background: rgba(255,255,255,0.03); }

/* header */
#slsb .hd { position: relative; display: flex; align-items: center; gap: 22px; padding: 0 30px; background: rgba(255,255,255,0.03); border-bottom: 1px solid rgba(255,255,255,0.1); }
#slsb .hd::after { content: ""; position: absolute; left: 0; right: 0; bottom: -1px; height: 1px; background: linear-gradient(90deg, rgba(0,229,255,0.0), rgba(0,229,255,0.45) 20%, rgba(255,43,214,0.45) 80%, rgba(255,43,214,0)); opacity: .55; }
#slsb .hd-icon { width: 52px; height: 40px; background: url(data:image/png;base64,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) center/contain no-repeat; filter: drop-shadow(0 2px 6px rgba(0,0,0,0.7)); margin-right: -8px; }
#slsb .hd-tabs { display: flex; align-items: center; gap: 28px; }
#slsb .hd-title { font-family: var(--display); font-size: 2.3rem; color: #fff; letter-spacing: 3px; line-height: 1; text-transform: uppercase; cursor: pointer; display: flex; align-items: center; gap: 10px; transition: color .2s; }
#slsb .hd-title:not(.on) { color: #555; }
#slsb .hd-title:not(.on):hover { color: #999; }
#slsb .hd-title i { font-style: normal; font-family: var(--mono); font-size: .85rem; letter-spacing: 0; color: #111; background: #777; border-radius: 3px; padding: 2px 7px; }
#slsb .hd-title.on i { background: var(--amber); }
#slsb .hd-deco { font-family: var(--mono); font-size: .85rem; color: var(--amber); opacity: .75; letter-spacing: 1px; display: flex; gap: 14px; text-transform: uppercase; }
#slsb .hd-deco b { color: #fff; font-weight: 400; opacity: .8; }
#slsb .hd-deco .run { color: var(--teal); opacity: 1; }
#slsb .hd-sp { flex: 1; }
#slsb .kbd { font-family: var(--mono); font-size: .8rem; color: #aaa; border: 1px solid #555; border-bottom-width: 2px; border-radius: 3px; padding: 1px 7px; }
#slsb .hint-k { font-family: var(--mono); font-size: .8rem; color: #666; display: flex; gap: 8px; align-items: center; }
#slsb .close-btn { cursor: pointer; font-size: 1.7rem; color: #888; transition: color .2s; line-height: 1; padding: 4px; }
#slsb .close-btn:hover { color: #fff; }

#slsb .bd { display: grid; grid-template-columns: 400px 1fr; min-height: 0; }
#slsb .bd.solo { grid-template-columns: 1fr; }

/* rail */
#slsb .rail { display: flex; flex-direction: column; min-height: 0; border-right: 1px solid var(--line); background: rgba(0,0,0,0.25); }
#slsb .search { margin: 18px 18px 10px; position: relative; }
#slsb .search input { width: 100%; background: #000; border: 1px solid #444; border-radius: 3px; padding: 11px 14px 11px 40px; color: #eee; font: 700 1rem var(--body); letter-spacing: .5px; outline: none; transition: border-color .15s, box-shadow .15s; user-select: text; }
#slsb .search input::placeholder { color: #666; font-family: var(--mono); font-weight: 400; font-size: .85rem; letter-spacing: 1px; text-transform: uppercase; }
#slsb .search input:focus { border-color: var(--amber); box-shadow: 0 0 12px rgba(235,163,8,0.2); }
#slsb .search svg { position: absolute; left: 13px; top: 50%; transform: translateY(-50%); width: 17px; height: 17px; color: #777; }
#slsb .chips { display: flex; flex-wrap: wrap; gap: 6px; padding: 0 18px 14px; border-bottom: 1px solid var(--line); }
#slsb .chip { font-family: var(--mono); font-size: .78rem; letter-spacing: 1px; padding: 4px 9px; border: 1px solid rgba(255,255,255,0.12); color: #999; border-radius: 2px; cursor: pointer; text-transform: uppercase; display: flex; gap: 6px; align-items: center; background: none; }
#slsb .chip:hover { border-color: rgba(235,163,8,0.6); color: #ddd; }
#slsb .chip.on { background: rgba(235,163,8,0.15); border-color: var(--amber); color: var(--amber); }
#slsb .chip i { font-style: normal; color: #fff; opacity: .55; }
#slsb .chip.warn i { color: var(--red); opacity: 1; }
#slsb .list { overflow-y: auto; flex: 1; padding: 4px 0 14px; }
#slsb .grp { font-family: var(--mono); font-size: .75rem; color: var(--amber); letter-spacing: 3px; text-transform: uppercase; display: flex; align-items: center; gap: 10px; padding: 14px 18px 6px; }
#slsb .grp > span:first-child { opacity: .75; }
#slsb .grp::after { content: ""; flex: 1; height: 1px; background: linear-gradient(90deg, rgba(235,163,8,0.25), transparent); order: 1; }
#slsb .grp .sort { order: 2; letter-spacing: 1px; color: #777; cursor: pointer; }
#slsb .grp .sort:hover { color: #ddd; }
#slsb .save { position: relative; display: grid; grid-template-columns: 48px 1fr auto; gap: 12px; align-items: center; padding: 8px 16px 8px 15px; border-left: 3px solid transparent; cursor: pointer; transition: background .15s; }
#slsb .save:hover { background: linear-gradient(90deg, rgba(235,163,8,0.07), transparent); }
#slsb .save.sel { border-left-color: var(--amber); background: linear-gradient(90deg, rgba(235,163,8,0.16), transparent 90%); }
#slsb .face { position: relative; width: 48px; height: 48px; border: 1px solid rgba(255,255,255,0.14); border-radius: 3px; background-color: #2a2622; background-repeat: no-repeat; overflow: hidden; display: grid; place-items: center; font-family: var(--display); color: #fff; font-size: 1.3rem; }
#slsb .face .st { position: absolute; right: 2px; bottom: 2px; width: 9px; height: 9px; border-radius: 50%; border: 2px solid #121214; background: var(--blue); }
#slsb .face .st.none { background: #666; }
#slsb .face .st.warn { background: var(--red); }
#slsb .face .st.play { background: var(--teal); }
#slsb .sname { font-size: 1.02rem; font-weight: 700; color: #ddd; letter-spacing: .5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
#slsb .save.sel .sname { color: #fff; }
#slsb .sname .star { color: var(--amber); font-size: .8rem; margin-right: 5px; }
#slsb .ssub { font-family: var(--mono); font-size: .75rem; color: #6f6f6f; letter-spacing: .5px; margin-top: 3px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
#slsb .pips { display: flex; flex-direction: column; align-items: flex-end; gap: 4px; }
#slsb .pip { font-family: var(--mono); font-size: .7rem; line-height: 1; padding: 3px 5px; border-radius: 2px; letter-spacing: .5px; white-space: nowrap; text-transform: uppercase; }
#slsb .pip.n { color: var(--blue); border: 1px solid rgba(79,168,255,0.45); }
#slsb .pip.zero { color: #888; border: 1px dashed #555; }
#slsb .pip.play { color: #111; background: var(--teal); border: 1px solid var(--teal); }
#slsb .pip.end { color: var(--gold); border: 1px solid rgba(255,200,58,0.45); }
#slsb .pip.endless { color: #c9a4ff; border: 1px solid rgba(201,164,255,0.45); }
#slsb .pip.chk { color: #111; background: var(--red); border: 1px solid var(--red); }
#slsb .pip.old { color: #aaa; border: 1px solid #666; }
#slsb .pip.new { color: var(--blue); border: 1px solid rgba(79,168,255,0.45); }
#slsb .pip.nowp { color: #111; background: var(--teal); }
#slsb .newgame { margin: 10px 18px 0; padding: 11px; border: 1px dashed #4a4a4a; text-align: center; font-family: var(--mono); font-size: .82rem; letter-spacing: 2px; color: #888; cursor: pointer; text-transform: uppercase; }
#slsb .newgame:hover { border-color: var(--amber); color: var(--amber); }
#slsb .empty-list { padding: 30px 18px; text-align: center; color: #666; font-family: var(--mono); font-size: .85rem; }

#slsb .store { padding: 14px 18px 16px; border-top: 1px solid var(--line); background: rgba(0,0,0,0.25); }
#slsb .store-t { display: flex; justify-content: space-between; font-family: var(--mono); font-size: .75rem; letter-spacing: 2px; text-transform: uppercase; color: #888; }
#slsb .store-t b { color: #ddd; font-weight: 400; letter-spacing: .5px; }
#slsb .bar { height: 6px; background: rgba(255,255,255,0.07); border-radius: 3px; margin-top: 8px; display: flex; overflow: hidden; }
#slsb .bar i { display: block; height: 100%; }
#slsb .bar .b1 { background: var(--blue); box-shadow: 0 0 8px rgba(79,168,255,0.5); }
#slsb .bar .b2 { background: var(--amber); }
#slsb .bar .b3 { background: #666; }
#slsb .store-l { display: flex; gap: 12px; margin-top: 7px; font-family: var(--mono); font-size: .7rem; color: #6f6f6f; white-space: nowrap; }
#slsb .store-l span::before { content: ""; display: inline-block; width: 7px; height: 7px; border-radius: 1px; margin-right: 5px; background: var(--c); }

/* main pane */
#slsb .pane { display: flex; flex-direction: column; min-height: 0; min-width: 0; }
#slsb .hero { position: relative; height: 178px; flex: none; overflow: hidden; border-bottom: 1px solid var(--line); }
#slsb .hero .banner { position: absolute; inset: 0; background: #1a1712 center 40%/cover; }
#slsb .hero .shade { position: absolute; inset: 0; background: linear-gradient(90deg, rgba(18,18,20,0.97) 0%, rgba(18,18,20,0.85) 38%, rgba(18,18,20,0.35) 75%, rgba(18,18,20,0.15) 100%), linear-gradient(0deg, rgba(18,18,20,0.9), transparent 45%); }
#slsb .hero .glow { position: absolute; inset: 0; background: radial-gradient(ellipse at 12% 50%, rgba(79,168,255,0.22) 0%, transparent 55%); }
#slsb .hero .gen { position: absolute; inset: 0; display: flex; align-items: center; gap: 26px; padding: 0 36px; }
#slsb .hero .big { flex: none; width: 112px; height: 112px; border: 1px solid rgba(255,255,255,0.18); border-radius: 4px; box-shadow: 0 10px 30px rgba(0,0,0,0.6); background-color: #2a2622; background-repeat: no-repeat; display: grid; place-items: center; font-family: var(--display); font-size: 2.8rem; color: #fff; }
#slsb .hero .ttl { min-width: 0; }
#slsb .hero .gt { font-family: var(--display); font-size: 3.1rem; color: #fff; letter-spacing: 2px; line-height: 1.05; text-shadow: 0 4px 12px rgba(0,0,0,0.6); max-width: 760px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
#slsb .hero .gs { font-size: 1.08rem; color: #c8c8c8; margin-top: 8px; text-shadow: 0 2px 6px rgba(0,0,0,0.8); }
#slsb .hero .gs b { color: #fff; font-weight: 700; }
#slsb .hero .gm { font-family: var(--mono); font-size: .8rem; color: #8a8a8a; margin-top: 8px; letter-spacing: .5px; display: flex; gap: 16px; }
#slsb .hero-side { position: absolute; right: 28px; top: 20px; z-index: 2; display: flex; flex-direction: column; align-items: flex-end; gap: 12px; }
#slsb .guard { display: flex; align-items: center; gap: 12px; background: rgba(0,0,0,0.6); border: 1px solid rgba(79,168,255,0.4); padding: 9px 16px 9px 14px; border-radius: 3px; }
#slsb .guard .ic { width: 28px; height: 28px; display: grid; place-items: center; border-radius: 50%; background: rgba(79,168,255,0.15); color: var(--blue); }
#slsb .guard .ic svg { width: 16px; height: 16px; }
#slsb .guard .lbl { font-family: var(--mono); font-size: .82rem; letter-spacing: 2px; color: var(--blue); text-transform: uppercase; }
#slsb .guard .lbl small { display: block; font-size: .72rem; color: #8a8a8a; letter-spacing: .5px; margin-top: 2px; text-transform: none; }
#slsb .guard.none { border-color: rgba(255,255,255,0.15); } #slsb .guard.none .ic { background: rgba(255,255,255,0.06); color: #999; } #slsb .guard.none .lbl { color: #bbb; }
#slsb .guard.warn { border-color: rgba(255,90,90,0.55); } #slsb .guard.warn .ic { background: rgba(255,90,90,0.15); color: var(--red); } #slsb .guard.warn .lbl { color: var(--red); }
#slsb .guard.play { border-color: rgba(143,199,191,0.55); } #slsb .guard.play .ic { background: rgba(143,199,191,0.15); color: var(--teal); } #slsb .guard.play .lbl { color: var(--teal); }
#slsb .big-btns { display: flex; gap: 10px; }

#slsb .btn { font-family: var(--mono); cursor: pointer; padding: 7px 14px; background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.15); color: #ccc; font-size: .85rem; letter-spacing: 1px; transition: all .15s; border-radius: 2px; white-space: nowrap; text-transform: uppercase; }
#slsb .btn:hover { background: rgba(235,163,8,0.15); border-color: var(--amber); color: var(--amber); }
#slsb .btn.danger:hover { background: rgba(200,50,50,0.15); border-color: #ff4444; color: #ff6b6b; }
#slsb .btn.pri { background: linear-gradient(180deg, #f5b51f, #d99406); border-color: #f5b51f; color: #111; font-weight: 700; box-shadow: 0 4px 16px rgba(235,163,8,0.25); }
#slsb .btn.pri:hover { background: linear-gradient(180deg, #ffc640, #e8a210); color: #000; }
#slsb .btn.blue { background: rgba(79,168,255,0.12); border-color: rgba(79,168,255,0.6); color: #9ccbff; }
#slsb .btn.blue:hover { background: rgba(79,168,255,0.25); border-color: var(--blue); color: #fff; }
#slsb .btn.lg { padding: 11px 22px; font-size: 1rem; letter-spacing: 2px; }
#slsb .btn.sm { padding: 5px 10px; font-size: .78rem; }
#slsb .btn[disabled] { opacity: .35; pointer-events: none; }
#slsb .btn .k { display: inline-block; margin-right: 7px; }

#slsb .tools { display: flex; align-items: center; gap: 10px; padding: 12px 30px; border-bottom: 1px solid var(--line); flex: none; }
#slsb .tools .sp { flex: 1; }
#slsb .note-in { flex: 1; min-width: 0; background: rgba(0,0,0,0.5); border: 1px solid #333; border-radius: 3px; padding: 7px 11px; color: #ddd; font-size: .9rem; outline: none; user-select: text; }
#slsb .note-in::placeholder { color: #5d5d5d; }
#slsb .note-in:focus { border-color: var(--amber); }

#slsb .notice { margin: 12px 30px 0; padding: 12px 16px; border: 1px dashed rgba(255,255,255,0.15); color: #aaa; font-size: .93rem; line-height: 1.55; display: flex; gap: 14px; align-items: flex-start; flex: none; }
#slsb .notice b { color: #eee; }
#slsb .notice .ico { font-family: var(--mono); color: var(--amber); font-size: 1.1rem; line-height: 1.3; }
#slsb .notice .acts { margin-top: 9px; display: flex; gap: 10px; }
#slsb .notice.warn { border: 1px solid rgba(255,90,90,0.5); background: rgba(255,90,90,0.05); color: #d9a3a3; }
#slsb .notice.warn .ico { color: var(--red); }
#slsb .notice.run { border: 1px solid rgba(143,199,191,0.45); background: rgba(143,199,191,0.05); color: #a9c9c4; }
#slsb .notice.run .ico { color: var(--teal); }

/* backups area */
#slsb .tabs { display: flex; align-items: flex-end; padding: 0 30px; border-bottom: 1px solid var(--line); flex: none; margin-top: 6px; }
#slsb .tab { position: relative; padding: 12px 16px 10px; font-size: .98rem; font-weight: 700; color: #888; letter-spacing: 2px; cursor: pointer; text-transform: uppercase; display: flex; gap: 7px; align-items: center; }
#slsb .tab:hover { color: #ddd; }
#slsb .tab.on { color: #fff; }
#slsb .tab.on::after { content: ""; position: absolute; left: 12px; right: 12px; bottom: -1px; height: 2px; background: var(--amber); }
#slsb .tab i { font-style: normal; font-family: var(--mono); font-size: .72rem; color: #666; letter-spacing: 0; }
#slsb .tabs .sp { flex: 1; }
#slsb .tabs .chips { border: 0; padding: 0 0 9px; }

#slsb .work { flex: 1; display: grid; grid-template-columns: minmax(0, 1fr) 470px; min-height: 0; }
#slsb .bk { overflow-y: auto; padding: 4px 18px 20px 30px; min-height: 0; }
#slsb .dayh { font-family: var(--mono); font-size: .75rem; color: var(--amber); letter-spacing: 3px; text-transform: uppercase; display: flex; align-items: center; gap: 10px; padding: 14px 0 6px; }
#slsb .dayh span { opacity: .75; }
#slsb .dayh small { font-size: .7rem; color: #666; letter-spacing: 1px; }
#slsb .dayh::after { content: ""; flex: 1; height: 1px; background: linear-gradient(90deg, rgba(235,163,8,0.25), transparent); }
#slsb .pt { position: relative; display: grid; grid-template-columns: 96px 1fr auto; gap: 14px; align-items: center; padding: 7px 10px 7px 8px; border: 1px solid transparent; border-left: 3px solid transparent; cursor: pointer; transition: background .15s; }
#slsb .pt:hover { background: rgba(255,255,255,0.025); }
#slsb .pt.sel { border-left-color: var(--amber); background: linear-gradient(90deg, rgba(235,163,8,0.12), rgba(235,163,8,0.02)); }
#slsb .pt .th { width: 96px; height: 54px; border: 1px solid rgba(255,255,255,0.1); border-radius: 2px; background: #1a1712 center/cover; position: relative; }
#slsb .pt .th.blank { display: grid; place-items: center; background: repeating-linear-gradient(135deg, rgba(255,255,255,0.03) 0 6px, transparent 6px 12px), #141416; color: #555; font-family: var(--mono); font-size: .65rem; letter-spacing: 1px; }
#slsb .pt .th .dot { position: absolute; left: 4px; bottom: 4px; }
#slsb .dot { display: inline-block; width: 9px; height: 9px; border-radius: 50%; box-shadow: 0 0 0 2px rgba(0,0,0,0.7); flex: none; }
#slsb .pt .l1 { display: flex; align-items: baseline; gap: 10px; min-width: 0; }
#slsb .pt .tm { font-family: var(--mono); font-size: 1rem; font-weight: 700; color: #fff; }
#slsb .pt .kn { font-size: .98rem; font-weight: 700; color: #ddd; white-space: nowrap; }
#slsb .pt .nt { font-size: .88rem; color: #d8c9a6; font-style: italic; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; min-width: 0; }
#slsb .pt .l2 { font-family: var(--mono); font-size: .74rem; color: #6f6f6f; margin-top: 4px; letter-spacing: .5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
#slsb .pt .rt { display: flex; gap: 6px; align-items: center; }
#slsb .pt .kept { color: var(--amber); font-size: .95rem; }
#slsb .pt.now .tm { color: var(--teal); }
#slsb .intro { font-size: .9rem; color: #888; line-height: 1.55; padding: 14px 0 4px; max-width: 640px; }

#slsb .insp { border-left: 1px solid var(--line); overflow-y: auto; padding: 18px 26px 24px; background: rgba(0,0,0,0.2); min-height: 0; }
#slsb .shot { position: relative; width: 100%; aspect-ratio: 16 / 9; border: 1px solid rgba(255,255,255,0.12); border-radius: 3px; background: #1a1712 center/cover; overflow: hidden; box-shadow: 0 10px 26px rgba(0,0,0,0.5); }
#slsb .shot.blank { display: grid; place-items: center; text-align: center; padding: 20px; background: repeating-linear-gradient(135deg, rgba(255,255,255,0.025) 0 8px, transparent 8px 16px), #131315; }
#slsb .shot.blank b { display: block; font-family: var(--mono); color: #777; letter-spacing: 2px; font-weight: 400; text-transform: uppercase; font-size: .85rem; }
#slsb .shot.blank small { display: block; color: #5d5d5d; font-size: .82rem; margin-top: 6px; }
#slsb .shot .tag { position: absolute; left: 10px; top: 10px; display: flex; gap: 6px; }
#slsb .shot .tag span { font-family: var(--mono); font-size: .7rem; letter-spacing: 1px; text-transform: uppercase; padding: 3px 7px; background: rgba(0,0,0,0.75); border: 1px solid rgba(255,255,255,0.15); border-radius: 2px; color: #ddd; display: flex; align-items: center; gap: 6px; }
#slsb .ih { margin-top: 14px; }
#slsb .ih h3 { font-family: var(--display); font-size: 1.9rem; color: #fff; letter-spacing: 1.5px; line-height: 1.05; font-weight: 400; }
#slsb .ih .sub { font-size: .95rem; color: #aaa; margin-top: 4px; display: flex; align-items: center; gap: 8px; }
#slsb .ia { display: flex; gap: 8px; margin-top: 14px; }
#slsb .ia .btn.pri { flex: 1; }
#slsb .ia2 { display: flex; gap: 6px; margin-top: 8px; flex-wrap: wrap; }
#slsb .ia2 .btn { flex: 1; }
#slsb .btn.on { color: var(--amber); border-color: rgba(235,163,8,0.6); background: rgba(235,163,8,0.08); }
#slsb .nb { margin-top: 12px; padding: 9px 12px; border-left: 2px solid var(--amber); background: rgba(235,163,8,0.06); color: #e6d8b8; font-size: .9rem; font-style: italic; }
#slsb .ow { margin-top: 12px; padding: 9px 12px; border-left: 2px solid #e8813a; background: rgba(232,129,58,0.07); color: #e6c3a6; font-size: .86rem; line-height: 1.5; }
#slsb .sec-t { font-family: var(--mono); font-size: .75rem; color: var(--amber); letter-spacing: 3px; text-transform: uppercase; display: flex; align-items: center; gap: 10px; margin: 20px 0 10px; }
#slsb .sec-t span { opacity: .75; }
#slsb .sec-t::after { content: ""; flex: 1; height: 1px; background: linear-gradient(90deg, rgba(235,163,8,0.25), transparent); }
#slsb .meter { display: grid; grid-template-columns: 92px 1fr 92px; gap: 12px; align-items: center; margin-bottom: 9px; }
#slsb .meter .ml { font-size: .88rem; color: #bbb; }
#slsb .meter .mt { position: relative; height: 8px; background: rgba(255,255,255,0.07); border-radius: 4px; }
#slsb .meter .mt i { position: absolute; left: 0; top: 0; bottom: 0; border-radius: 4px; }
#slsb .meter .mt u { position: absolute; top: -4px; bottom: -4px; width: 2px; background: #fff; opacity: .55; border-radius: 1px; }
#slsb .meter .mv { font-family: var(--mono); font-size: .85rem; color: #eee; text-align: right; white-space: nowrap; }
#slsb .meter .mv small { display: block; font-size: .7rem; color: #777; }
#slsb .meter .mv small.up { color: #8fd08f; } #slsb .meter .mv small.dn { color: #e88a8a; }
#slsb .tiles { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; margin-top: 12px; }
#slsb .tile { border: 1px solid rgba(255,255,255,0.08); background: rgba(255,255,255,0.025); border-radius: 3px; padding: 9px 11px; }
#slsb .tile span { display: block; font-size: .78rem; color: #888; }
#slsb .tile b { display: block; font-family: var(--mono); font-size: 1.15rem; color: #fff; font-weight: 700; margin-top: 3px; }
#slsb .tile small { font-family: var(--mono); font-size: .72rem; color: #777; }
#slsb .tile small.up { color: #8fd08f; } #slsb .tile small.dn { color: #e88a8a; }
#slsb .facts { margin-top: 16px; font-family: var(--mono); font-size: .74rem; color: #666; letter-spacing: .5px; line-height: 1.7; }
#slsb .photos { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; padding: 12px 0; }
#slsb .photos div { aspect-ratio: 16 / 9; border: 1px solid rgba(255,255,255,0.12); border-radius: 3px; background: #1a1712 center/cover; cursor: pointer; }
#slsb .photos div:hover { border-color: var(--amber); }

/* how it works: the numbered cards from the Nexus features images */
#slsb .how { padding: 18px 0 8px; }
#slsb .how h4 { font-family: var(--display); font-weight: 400; font-size: 1.7rem; color: #fff; letter-spacing: 1.5px; }
#slsb .how-cards { display: grid; gap: 10px; margin-top: 14px; }
#slsb .hc { display: flex; align-items: center; gap: 18px; padding: 14px 18px; border-radius: 8px; background: rgba(13,13,19,0.8); border: 1px solid rgba(79,168,255,0.35); border-left: 3px solid var(--blue); }
#slsb .hc .no { font-family: var(--mono); font-weight: 700; font-size: 1.5rem; color: var(--blue); width: 34px; }
#slsb .hc b { display: block; color: #fff; font-size: 1.1rem; }
#slsb .hc small { display: block; color: #aaa; font-size: .9rem; margin-top: 3px; line-height: 1.45; }
#slsb .how .ft-note { color: #777; font-size: .85rem; margin-top: 12px; }

/* bin */
#slsb .binp { overflow-y: auto; padding: 26px 60px 30px; min-height: 0; }
#slsb .binp .inner { max-width: 1200px; margin: 0 auto; }
#slsb .binhead { display: flex; align-items: center; gap: 26px; padding: 20px 26px; border-radius: 6px; border: 1px solid rgba(79,168,255,0.3); background: linear-gradient(110deg, rgba(10,31,56,0.75), rgba(18,18,20,0.6) 60%); }
#slsb .binhead .t { font-family: var(--display); font-size: 2.4rem; color: #fff; letter-spacing: 2px; line-height: 1; }
#slsb .binhead p { color: #aab4c0; font-size: .95rem; line-height: 1.55; max-width: 760px; margin-top: 6px; }
#slsb .binhead .sp { flex: 1; }
#slsb .brow { display: grid; grid-template-columns: 120px 1fr 200px auto; gap: 18px; align-items: center; padding: 12px 14px; border-bottom: 1px solid rgba(255,255,255,0.04); }
#slsb .brow:hover { background: rgba(255,255,255,0.02); }
#slsb .brow .th { width: 120px; height: 68px; border: 1px solid rgba(255,255,255,0.1); border-radius: 2px; background: #1a1712 center/cover; filter: grayscale(.55) brightness(.85); position: relative; }
#slsb .brow .th .face { position: absolute; left: -6px; bottom: -6px; width: 34px; height: 34px; filter: none; }
#slsb .brow .bt { font-size: 1.02rem; font-weight: 700; color: #ddd; }
#slsb .brow .bs { font-family: var(--mono); font-size: .75rem; color: #777; margin-top: 4px; letter-spacing: .5px; }
#slsb .brow .bs2 { font-size: .82rem; color: #888; margin-top: 3px; }
#slsb .left { font-family: var(--mono); font-size: .74rem; color: #999; letter-spacing: 1px; text-transform: uppercase; }
#slsb .left .bar { margin-top: 6px; height: 4px; }
#slsb .left .bar i { background: var(--blue); }
#slsb .left.low .bar i { background: var(--red); }
#slsb .brow .acts { display: flex; gap: 8px; }
#slsb .bin-foot { display: flex; justify-content: flex-end; margin-top: 18px; }
#slsb .bin-empty { padding: 60px; text-align: center; font-family: var(--display); font-size: 2rem; color: #444; letter-spacing: 3px; }

/* settings */
#slsb .snav .item { display: grid; grid-template-columns: 48px 1fr; gap: 14px; align-items: center; padding: 12px 16px 12px 15px; border-left: 3px solid transparent; cursor: pointer; }
#slsb .snav .item:hover { background: linear-gradient(90deg, rgba(235,163,8,0.07), transparent); }
#slsb .snav .item.sel { border-left-color: var(--amber); background: linear-gradient(90deg, rgba(235,163,8,0.16), transparent 90%); }
#slsb .snav .no { width: 48px; height: 48px; display: grid; place-items: center; font-family: var(--mono); font-weight: 700; font-size: 1.15rem; color: var(--blue); border: 1px solid rgba(79,168,255,0.45); border-radius: 3px; background: rgba(79,168,255,0.07); }
#slsb .snav .item.sel .no { color: #111; background: var(--blue); border-color: var(--blue); }
#slsb .snav .nm { font-size: 1.02rem; font-weight: 700; color: #ddd; }
#slsb .snav .item.sel .nm { color: #fff; }
#slsb .snav .ns { font-family: var(--mono); font-size: .75rem; color: #6f6f6f; margin-top: 3px; }
#slsb .sets { overflow-y: auto; padding: 0 40px 40px; min-height: 0; }
#slsb .sets-hero { display: flex; align-items: center; gap: 24px; padding: 30px 0 10px; }
#slsb .sets-hero .no { font-family: var(--mono); font-weight: 700; font-size: 3rem; color: var(--blue); line-height: 1; }
#slsb .sets-hero h2 { font-family: var(--display); font-weight: 400; font-size: 3rem; color: #fff; letter-spacing: 2px; line-height: 1; }
#slsb .sets-hero p { color: #999; font-size: .98rem; margin-top: 8px; max-width: 820px; line-height: 1.55; }
#slsb .row { position: relative; display: grid; grid-template-columns: 50px 1fr auto; gap: 18px; align-items: center; padding: 13px 10px 13px 4px; border-bottom: 1px solid rgba(255,255,255,0.035); }
#slsb .row.notog { grid-template-columns: 1fr auto; padding-left: 10px; }
#slsb .row:hover { background: rgba(255,255,255,0.02); }
#slsb .row.off .lab, #slsb .row.off .ctl { opacity: .45; }
#slsb .lab { font-size: 1.05rem; font-weight: 700; color: #ddd; letter-spacing: .5px; }
#slsb .hint { font-size: .86rem; color: rgba(200,200,200,0.55); margin-top: 3px; line-height: 1.45; font-weight: 400; letter-spacing: .3px; }
#slsb .ctl { display: flex; align-items: center; gap: 10px; justify-content: flex-end; font-family: var(--mono); font-size: .8rem; color: #888; letter-spacing: 1px; text-transform: uppercase; }
#slsb .ctl .always { color: #666; border: 1px dashed #444; padding: 3px 8px; border-radius: 2px; }
#slsb .tog { position: relative; width: 50px; height: 26px; cursor: pointer; }
#slsb .tog .track { position: absolute; inset: 0; border-radius: 13px; background: rgba(255,255,255,0.14); border: 1px solid rgba(255,255,255,0.18); transition: background .18s, border-color .18s; }
#slsb .tog .knob { position: absolute; top: 3px; left: 3px; width: 20px; height: 20px; border-radius: 50%; background: #ddd; box-shadow: 0 1px 3px rgba(0,0,0,0.6); transition: transform .18s cubic-bezier(0.2,1,0.4,1); }
#slsb .tog.on .track { background: rgba(224,190,85,0.55); border-color: rgba(224,190,85,0.8); }
#slsb .tog.on .knob { transform: translateX(24px); background: #fff; }
#slsb .num { width: 76px; background: #000; border: 1px solid #444; border-radius: 3px; color: var(--amber); font: 700 .98rem var(--mono); text-align: right; padding: 6px 9px; outline: none; user-select: text; }
#slsb .num:focus, #slsb .path:focus { border-color: var(--amber); box-shadow: 0 0 10px rgba(235,163,8,0.2); }
#slsb .path { width: 560px; background: #000; border: 1px solid #444; border-radius: 3px; color: #eee; font: .88rem var(--mono); padding: 8px 10px; outline: none; user-select: text; }
#slsb .path::placeholder { color: #555; }
#slsb .keycap { min-width: 120px; text-align: center; font-family: var(--mono); font-weight: 700; font-size: .95rem; color: var(--amber); background: rgba(235,163,8,0.06); border: 1px solid rgba(235,163,8,0.45); border-bottom-width: 3px; border-radius: 4px; padding: 6px 14px; cursor: pointer; letter-spacing: 1px; text-transform: none; }
#slsb .keycap.listen { color: #111; background: var(--amber); }

#slsb .ladder { display: grid; grid-template-columns: 1fr 34px 1fr 34px 1fr; align-items: stretch; margin-top: 18px; }
#slsb .rung { padding: 18px 20px; border-radius: 8px; background: rgba(13,13,19,0.85); border: 1px solid rgba(79,168,255,0.35); border-left: 3px solid var(--blue); }
#slsb .rung .no { font-family: var(--mono); font-weight: 700; font-size: 1.3rem; color: var(--blue); }
#slsb .rung .rt { font-size: 1.02rem; color: #ddd; margin-top: 8px; line-height: 1.9; }
#slsb .rung .rt .num { width: 64px; margin: 0 4px; }
#slsb .rung .rt .tog { display: inline-block; vertical-align: middle; margin-left: 6px; }
#slsb .arrow { display: grid; place-items: center; color: #555; font-family: var(--mono); font-size: 1.3rem; }
#slsb .density { margin-top: 18px; padding: 16px 20px 12px; border: 1px solid rgba(255,255,255,0.07); border-radius: 6px; background: rgba(0,0,0,0.25); }
#slsb .density .ticks { position: relative; height: 34px; }
#slsb .density .ticks i { position: absolute; bottom: 0; width: 2px; border-radius: 1px; background: var(--blue); }
#slsb .density .ticks i.k { background: var(--amber); }
#slsb .density .ends { display: flex; justify-content: space-between; font-family: var(--mono); font-size: .72rem; color: #666; letter-spacing: 1px; text-transform: uppercase; margin-top: 8px; }
#slsb .density .zones { display: flex; margin-top: 6px; font-family: var(--mono); font-size: .7rem; color: #777; letter-spacing: 1px; }
#slsb .density .zones span { border-top: 1px solid #333; padding-top: 5px; text-align: center; }
#slsb .info { margin-top: 14px; padding: 12px 16px; border: 1px dashed rgba(255,255,255,0.15); color: #999; font-size: .92rem; line-height: 1.55; display: flex; gap: 12px; }
#slsb .info .ico { font-family: var(--mono); color: var(--blue); }

/* footer */
#slsb .ft { display: flex; align-items: center; gap: 16px; padding: 0 24px 0 26px; background: rgba(0,0,0,0.35); border-top: 1px solid rgba(255,255,255,0.06); }
#slsb .status-dot { flex-shrink: 0; width: 7px; height: 7px; background: var(--blue); border-radius: 50%; box-shadow: 0 0 6px var(--blue); }
#slsb .status-dot.warn { background: var(--red); box-shadow: 0 0 6px var(--red); animation: sb-blink 2s infinite; }
#slsb .status-dot.run { background: var(--teal); box-shadow: 0 0 6px var(--teal); }
@keyframes sb-blink { 0%,100% { opacity: 1; } 50% { opacity: .3; } }
#slsb .ft-t { flex: 1; min-width: 0; font-family: var(--mono); font-size: .85rem; color: #999; letter-spacing: .5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
#slsb .ft-t em { color: var(--red); font-style: normal; }
#slsb .ft-t b { color: #ddd; font-weight: 400; }
#slsb .ft .btn { padding: 5px 12px; font-size: .8rem; }

/* dialogs */
#slsb .modal { position: absolute; inset: 0; z-index: 20; background: rgba(0,0,0,0.7); display: grid; place-items: center; }
#slsb .mbox { position: relative; width: 760px; max-height: 860px; display: flex; flex-direction: column; background: var(--ink); background-image: linear-gradient(rgba(255,255,255,0.02) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.02) 1px, transparent 1px); background-size: 20px 20px; border: 1px solid #3a3a3a; box-shadow: 0 30px 80px rgba(0,0,0,0.9); }
#slsb .mbox.wide { width: 1040px; }
#slsb .mhd { display: flex; align-items: center; gap: 14px; padding: 18px 24px; border-bottom: 1px solid rgba(255,255,255,0.1); background: rgba(255,255,255,0.03); }
#slsb .mhd h2 { font-family: var(--display); font-weight: 400; font-size: 2rem; color: #fff; letter-spacing: 2px; text-transform: uppercase; line-height: 1; }
#slsb .mhd .sp { flex: 1; }
#slsb .mbd { padding: 20px 24px; overflow-y: auto; font-size: .98rem; line-height: 1.6; color: #bbb; }
#slsb .mbd b { color: #fff; }
#slsb .mft { display: flex; align-items: center; gap: 10px; justify-content: flex-end; padding: 14px 24px; border-top: 1px solid rgba(255,255,255,0.06); background: rgba(0,0,0,0.35); }
#slsb .mft .sp { flex: 1; }
#slsb .mft .mh { font-size: .85rem; color: #777; }
#slsb .strip { display: grid; grid-template-columns: 1fr 50px 1fr; align-items: center; margin-bottom: 18px; }
#slsb .strip .arr { text-align: center; color: var(--amber); font-family: var(--mono); font-size: 1.6rem; }
#slsb .strip .shot { box-shadow: none; aspect-ratio: auto; height: 220px; }
#slsb .strip .cap { margin-top: 8px; font-family: var(--mono); font-size: .8rem; color: #999; letter-spacing: 1px; text-transform: uppercase; }
#slsb .strip .cap b { display: block; font-family: var(--body); font-size: 1.05rem; color: #fff; letter-spacing: .5px; text-transform: none; margin-top: 2px; }
#slsb .choice { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
#slsb .opt { position: relative; padding: 16px 18px; border: 1px solid rgba(255,255,255,0.12); border-radius: 4px; cursor: pointer; background: rgba(255,255,255,0.02); }
#slsb .opt:hover { border-color: rgba(235,163,8,0.5); }
#slsb .opt.on { border-color: var(--amber); background: rgba(235,163,8,0.08); box-shadow: 0 0 0 1px rgba(235,163,8,0.25) inset; }
#slsb .opt.off { opacity: .45; cursor: default; }
#slsb .opt .ot { display: flex; align-items: center; gap: 10px; font-family: var(--mono); font-size: .95rem; letter-spacing: 1.5px; color: #fff; text-transform: uppercase; }
#slsb .opt .radio { width: 16px; height: 16px; border-radius: 50%; border: 2px solid #666; flex: none; }
#slsb .opt.on .radio { border-color: var(--amber); box-shadow: inset 0 0 0 3px var(--ink); background: var(--amber); }
#slsb .opt small { display: block; color: #999; font-size: .9rem; line-height: 1.5; margin-top: 8px; }
#slsb .opt .pip { margin-left: auto; }
#slsb .vs { display: grid; grid-template-columns: 1fr 1fr; gap: 18px; }
#slsb .vs .side { padding: 12px; border: 1px solid rgba(255,255,255,0.08); border-radius: 4px; background: rgba(0,0,0,0.25); }
#slsb .vs .side.pick { cursor: pointer; }
#slsb .vs .side.pick:hover { border-color: rgba(235,163,8,0.5); }
#slsb .vs .who { margin-top: 10px; font-family: var(--mono); font-size: .78rem; letter-spacing: 2px; text-transform: uppercase; color: #888; }
#slsb .vs .who b { display: block; font-family: var(--body); font-size: 1.15rem; letter-spacing: .5px; text-transform: none; color: #fff; margin-top: 2px; }
#slsb .vs .who small { display: block; font-family: var(--mono); font-size: .75rem; color: #666; letter-spacing: .5px; text-transform: none; margin-top: 2px; }
#slsb table.cmp { width: 100%; border-collapse: collapse; margin-top: 16px; font-size: .95rem; }
#slsb table.cmp th, #slsb table.cmp td { padding: 7px 12px; border-bottom: 1px solid rgba(255,255,255,0.05); text-align: right; }
#slsb table.cmp th { font-family: var(--mono); font-size: .72rem; color: #777; letter-spacing: 2px; text-transform: uppercase; font-weight: 400; }
#slsb table.cmp td { font-family: var(--mono); color: #ddd; }
#slsb table.cmp th:first-child, #slsb table.cmp td:first-child { text-align: left; font-family: var(--body); color: #bbb; }
#slsb .up { color: #8fd08f !important; } #slsb .dn { color: #e88a8a !important; }
#slsb .idiff { display: grid; grid-template-columns: 1fr 1fr; gap: 18px; margin-top: 4px; }
#slsb .idiff h5 { font-family: var(--mono); font-weight: 400; font-size: .72rem; letter-spacing: 2px; color: #777; text-transform: uppercase; margin-bottom: 8px; }
#slsb .icards { display: flex; flex-wrap: wrap; gap: 8px; }
#slsb .icard { display: flex; align-items: center; gap: 8px; padding: 5px 10px 5px 5px; border: 1px solid rgba(220,188,120,0.18); background: linear-gradient(180deg, rgba(44,36,26,0.7), rgba(20,17,13,0.8)); border-radius: 3px; font-size: .88rem; color: #ddd; }
#slsb .icard img { width: 30px; height: 30px; object-fit: contain; filter: drop-shadow(0 2px 3px rgba(0,0,0,0.7)); }
#slsb .icard b { font-family: var(--mono); font-size: .9rem; }
#slsb .mrow { display: flex; align-items: center; gap: 14px; padding: 11px 0; border-bottom: 1px solid rgba(255,255,255,0.05); }
#slsb .mrow .lb { flex: 1; color: #ddd; }
#slsb .mrow .lb small { display: block; color: #777; font-size: .84rem; }
#slsb .mrow .tog.lock { opacity: .5; pointer-events: none; }
#slsb .ifile { display: grid; grid-template-columns: 44px 1fr auto; gap: 14px; align-items: center; padding: 10px 12px; margin-top: 8px; border: 1px solid rgba(255,255,255,0.1); border-radius: 3px; cursor: pointer; }
#slsb .ifile:hover { border-color: rgba(235,163,8,0.5); }
#slsb .ifile.on { border-color: var(--amber); background: rgba(235,163,8,0.07); }
#slsb .ifile.bad { opacity: .45; cursor: default; }
#slsb .ifile .face { width: 44px; height: 44px; font-size: 1rem; }
#slsb .ifile .fn { font-family: var(--mono); font-size: .88rem; color: #eee; }
#slsb .ifile small { display: block; font-size: .84rem; color: #888; margin-top: 2px; }
#slsb .pathline { display: flex; align-items: center; gap: 10px; margin-top: 12px; padding: 9px 12px; background: #000; border: 1px solid #333; border-radius: 3px; font-family: var(--mono); font-size: .82rem; color: #aaa; }
#slsb .pathline span { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
#slsb .txt { width: 100%; margin-top: 10px; background: #000; border: 1px solid #444; border-radius: 3px; color: #eee; font-size: 1rem; padding: 9px 12px; outline: none; user-select: text; }
#slsb .txt:focus { border-color: var(--amber); }

#slsb .toast { position: absolute; left: 50%; bottom: 76px; transform: translateX(-50%); z-index: 30; display: flex; align-items: center; gap: 12px; padding: 11px 18px; background: rgba(10,12,16,0.95); border: 1px solid rgba(79,168,255,0.5); border-radius: 3px; box-shadow: 0 10px 30px rgba(0,0,0,0.7); font-family: var(--mono); font-size: .88rem; color: #ddd; letter-spacing: .5px; animation: sb-toast .25s ease; }
#slsb .toast .status-dot { width: 8px; height: 8px; }
@keyframes sb-toast { from { opacity: 0; transform: translate(-50%, 8px); } }

#slsb .zoomable { cursor: zoom-in; }
#slsb .shot.zoomable:hover { border-color: rgba(255,255,255,0.35); }
#slsb .vs .shot { aspect-ratio: auto; height: 170px; box-shadow: none; }
#slsb .cmpgrid { display: grid; grid-template-columns: 1fr 1fr; gap: 28px; align-items: start; }
#slsb .cmpgrid .idiff { grid-template-columns: 1fr; gap: 12px; }
#slsb table.cmp th, #slsb table.cmp td { padding: 5px 12px; }
#slsb .ctl .all { width: 30px; color: var(--teal); font-size: .72rem; text-align: left; }
#slsb .snav { display: flex; flex-direction: column; }
#slsb .ia .btn { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; }
#slsb .ia .btn.lg { padding: 11px 10px; letter-spacing: 1.5px; font-size: .95rem; }
#slsb table.cmp td, #slsb table.cmp th { white-space: nowrap; }
`;

  // Survivors' portraits are whole-body pictures; these show the face. Ids are the game's PlayerSelect ids.
  var FACE_CROPS = {
    1: ['330% auto', '50% 12%'],
    2: ['250% auto', '50% 10%'],
    3: ['250% auto', '50% 10%'],
    4: ['240% auto', '50% 8%']
  };
  var DEFAULT_CROP = ['250% auto', '50% 10%'];
  var KIND_COLORS = {
    now: '#8fc7bf', morning: '#FFC83A', expedition: '#8fc7bf', wave: '#ff5a5a', rebirth: '#c9a4ff', ending: '#EBA308',
    manual: '#ffffff', quit: '#9a9a9a', timer: '#d8b070', update: '#a0c86e', safety: '#6f6a60', imported: '#4fa8ff', snap: '#6f8fb8', round: '#6f8fb8'
  };
  var METER_COLORS = { life: '#e06a5a', satiety: '#d8b070', mood: '#c9a4ff', stamina: '#8fc7bf' };
  var METERS = ['life', 'satiety', 'mood', 'stamina'];
  var TILES = ['planning', 'survival', 'items'];
  var SORTS = ['played', 'days', 'survivor', 'name'];
  var FILTERS = ['all', 'live', 'end', 'warn'];
  var TRIGGERS = [
    ['morning', 'Backups.OnMorning', 'Keep.Morning'],
    ['expedition', 'Backups.BeforeExpedition', 'Keep.Expedition'],
    ['wave', 'Backups.BeforeWave', 'Keep.Wave'],
    ['rebirth', 'Backups.BeforeRebirth', 'Keep.Rebirth'],
    ['ending', 'Backups.AtEnding', 'Keep.Ending'],
    ['quit', 'Backups.OnQuit', 'Keep.Quit'],
    ['timer', 'Backups.OnTimer', 'Keep.Timer'],
    ['update', 'Backups.BeforeModUpdates', 'Keep.Update'],
    ['manual', '', 'Keep.Manual'],
    ['safety', '', 'Keep.Safety']
  ];
  var SCROLLERS = ['.list', '.bk', '.insp', '.sets', '.binp', '.mbd'];
  var STAGE_WIDTH = 1920;
  var STAGE_HEIGHT = 1080;

  var SHIELD = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 3l7 3v6c0 4.5-3 7.5-7 9-4-1.5-7-4.5-7-9V6z"/><path d="M8.5 12l2.5 2.5 4.5-5"/></svg>';
  var CLOCK = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round"><circle cx="12" cy="12" r="8"/><path d="M12 8v4l3 2"/></svg>';
  var ALERT = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round"><path d="M12 4l9 16H3z"/><path d="M12 10v4M12 17v.5"/></svg>';
  var PLAY = '<svg viewBox="0 0 24 24" fill="currentColor"><path d="M8 5v14l11-7z"/></svg>';
  var SEARCH = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="M20 20l-3.5-3.5"/></svg>';

  // What the plugin sent (open, show, detail, imports) and what the player picked on the page.
  var view = null;
  var detail = null;
  var detailKey = '';
  var imports = null;
  var ui = freshUi();
  var drafts = {};
  var isBusy = false;
  var isTyping = false;
  var pageEscAt = 0;

  // The Toolkit's page API (window.ExiledToolkit). The game can run its script after this one, so this stays null
  // until it has; the page says it is ready only then, so the plugin opens nothing before.
  var etk = null;

  function freshUi() {
    return { tab: 'saves', selected: null, pointKey: null, sub: 'backups', bkFilter: 'all', filter: 'all', sortBy: 'played', query: '', modal: null, setSec: 0, isListening: false };
  }

  // ---------- talking to the plugin ----------

  function post(parts) {
    if (etk) etk.send(PREFIX, parts.join('|'));
  }

  // A step that changes saves: the page takes no clicks until the plugin sends the new view or says it is done.
  function step(parts) {
    isBusy = true;
    var win = document.getElementById(WIN_ID);
    if (win) win.classList.add('busy');
    post(parts);
  }

  // ---------- words ----------

  function isZh() {
    return !!(view && view.zh);
  }

  function pick(pair) {
    return pair ? pair[isZh() ? 1 : 0] || pair[0] : '';
  }

  function fill(text, args) {
    return String(text).replace(/\{(\d)\}/g, function (m, i) { return args[i] == null ? '' : args[i]; });
  }

  // A text by key, with {0}, {1}... filled from the remaining arguments.
  function t(key) {
    return fill(pick(TEXT[key]), [].slice.call(arguments, 1));
  }

  function tf(group, key) {
    return pick(TEXT[group][key]);
  }

  function esc(s) {
    return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
    });
  }

  // A file:// or game URL as a CSS background, safe inside a style attribute.
  function bgUrl(url) {
    if (!url) return '';
    return 'background-image:url(&quot;' + esc(String(url).replace(/\\/g, '/').replace(/"/g, '%22')) + '&quot;)';
  }

  function size(bytes) {
    bytes = bytes || 0;
    if (bytes >= 1073741824) return (bytes / 1073741824).toFixed(2) + ' GB';
    if (bytes >= 1048576) return (bytes / 1048576).toFixed(1) + ' MB';
    return Math.max(1, Math.round(bytes / 1024)) + ' KB';
  }

  function ago(seconds) {
    if (seconds == null) return '-';
    if (seconds < 90) return tf('ago', 'now');
    if (seconds < 3600) return fill(tf('ago', 'min'), [Math.round(seconds / 60)]);
    if (seconds < 86400) return fill(tf('ago', 'hour'), [Math.round(seconds / 3600)]);
    if (seconds < 172800) return tf('ago', 'day');
    return fill(tf('ago', 'days'), [Math.round(seconds / 86400)]);
  }

  function playTime(seconds) {
    return seconds ? t('playTime', Math.floor(seconds / 3600), Math.floor(seconds % 3600 / 60)) : '-';
  }

  function where(p) {
    return p.turn > 1 ? t('loopDay', p.turn, p.day) : t('day', p.day);
  }

  function whereAt(p) {
    if (!p.clock) return where(p);
    return p.turn > 1 ? t('loopDayAt', p.turn, p.day, p.clock) : t('dayAt', p.day, p.clock);
  }

  // A place written mid-sentence: 'loop 2, day 16' in English.
  function mid(text) {
    return isZh() ? text : text.charAt(0).toLowerCase() + text.slice(1);
  }

  function kindName(kind) {
    return TEXT.kinds[kind] ? tf('kinds', kind) : kind;
  }

  function kindDot(kind) {
    return '<span class="dot" style="background:' + (KIND_COLORS[kind] || '#888') + '"></span>';
  }

  function setting(id) {
    return view && view.settings && view.settings[id] != null ? String(view.settings[id]) : '';
  }

  function isOn(id) {
    return setting(id) === 'true';
  }

  // ---------- runs and pictures ----------

  // The game's WebUI folder, from this page's own address; the survivors' and items' pictures are in it.
  function webRoot() {
    var at = location.href.indexOf('/WebUI/');
    return at >= 0 ? location.href.slice(0, at + 7) : '';
  }

  // The game writes a picture as "../../Res/NPC/Play01.png" (from a page two folders down), a path under WebUI, or a
  // bare file name in the folder it belongs to.
  function gamePic(path, folder) {
    if (!path) return '';
    if (/^(\.\.\/)+/.test(path)) return webRoot() + path.replace(/^(\.\.\/)+/, '');
    return path.indexOf('/') >= 0 ? webRoot() + path : webRoot() + folder + path;
  }

  function survivor(run) {
    return view && view.survivors ? view.survivors[String(run.survivor)] || null : null;
  }

  function survivorName(run) {
    var s = survivor(run);
    if (!s) return run.role || '';
    return (isZh() ? s.zh || s.en : s.en || s.zh) || run.role || '';
  }

  function isStampName(name) {
    return /^\d{4}_\d\d_\d\d_\d\d_\d\d/.test(name || '');
  }

  // A run the player never named shows its survivor and start time, not the game's raw stamp.
  function title(run) {
    if (run.name && !isStampName(run.name)) return run.name;
    var m = /(\d{4})_(\d\d)_(\d\d)_(\d\d)_(\d\d)/.exec(run.name || '');
    var who = survivorName(run) || run.stem || '';
    return m ? who + ' ' + m[1] + '-' + m[2] + '-' + m[3] + ' ' + m[4] + ':' + m[5] : who;
  }

  function shotOf(stem, backup) {
    return backup && backup.shot && view.shots ? view.shots + encodeURIComponent(stem) + '/' + encodeURIComponent(backup.id) + '/shot.jpg' : '';
  }

  // The full-size picture beside shot.jpg; backups made before 0.4 have none, and the enlarged view falls back.
  function largeOf(small) {
    return small ? small.replace(/shot\.jpg$/, 'shot-large.jpg') : '';
  }

  function binShot(entry) {
    return entry.shot && view.shots ? view.shots + 'Bin/' + encodeURIComponent(entry.stem) + '/' + encodeURIComponent(entry.id) + '/shot.jpg' : '';
  }

  function newestShot(stem, backups) {
    for (var i = 0; i < (backups || []).length; i++) if (backups[i].shot) return shotOf(stem, backups[i]);
    return '';
  }

  // The picture a run is shown with: its newest backup picture, else the survivor's new game background.
  function runPic(run) {
    var shot = newestShot(run.stem, run.backups);
    if (shot) return shot;
    var s = survivor(run);
    return s ? gamePic(s.bg, 'UI/SelectPeople/') : '';
  }

  function faceStyle(run) {
    var s = survivor(run);
    var pic = s ? gamePic(s.pic, 'Res/NPC/') : '';
    if (!pic) return '';
    var crop = FACE_CROPS[run.survivor] || DEFAULT_CROP;
    return bgUrl(pic) + ';background-size:' + crop[0] + ';background-position:' + crop[1];
  }

  function face(run, cls, withState) {
    var style = faceStyle(run);
    var dot = withState ? '<span class="st ' + (isPlaying(run) ? 'play' : run.behind ? 'warn' : (run.backups || []).length ? '' : 'none') + '"></span>' : '';
    return '<div class="' + (cls || 'face') + '" style="' + style + '">' + (style ? '' : esc(title(run).charAt(0).toUpperCase())) + dot + '</div>';
  }

  function runs() {
    return view ? view.runs || [] : [];
  }

  function findRun(stem) {
    var list = runs();
    for (var i = 0; i < list.length; i++) if (list[i].stem === stem) return list[i];
    return null;
  }

  function selRun() {
    return ui.selected ? findRun(ui.selected) : null;
  }

  function isPlaying(run) {
    return !!view.inRun && run.stem === view.inRun;
  }

  function backupBytes(run) {
    return (run.backups || []).reduce(function (n, b) { return n + (b.bytes || 0); }, 0);
  }

  function backupById(run, id) {
    var list = run.backups || [];
    for (var i = 0; i < list.length; i++) if (list[i].id === id) return list[i];
    return null;
  }

  function totalBackups() {
    return runs().reduce(function (n, run) { return n + (run.backups || []).length; }, 0);
  }

  function trashBytes() {
    return (view.binBytes || 0) + (view.deleted || []).reduce(function (n, run) { return n + backupBytes(run); }, 0);
  }

  function trashCount() {
    return (view.bin || []).length + (view.deleted || []).length;
  }

  function detailOf(run) {
    return detail && detail.stem === run.stem ? detail : null;
  }

  // ---------- saves rail ----------

  function matches(run) {
    if (ui.filter === 'live' && run.finished) return false;
    if (ui.filter === 'end' && !run.finished) return false;
    if (ui.filter === 'warn' && !run.behind) return false;
    var q = ui.query.trim().toLowerCase();
    if (!q) return true;
    var s = survivor(run) || {};
    var text = [run.name, title(run), s.en, s.zh, run.diff, run.note, run.finished ? run.status : ''].join(' ');
    (run.backups || []).forEach(function (b) { text += ' ' + (b.note || ''); });
    return text.toLowerCase().indexOf(q) >= 0;
  }

  function sorted(list) {
    var by = {
      played: function (a, b) { return (a.lastPlayedAgo || 0) - (b.lastPlayedAgo || 0); },
      days: function (a, b) { return (b.turn * 1000 + b.day) - (a.turn * 1000 + a.day); },
      survivor: function (a, b) { return survivorName(a).localeCompare(survivorName(b)); },
      name: function (a, b) { return title(a).localeCompare(title(b)); }
    }[ui.sortBy];
    return list.slice().sort(by);
  }

  function saveRow(run) {
    var sub = [where(run)];
    if (run.finished && run.status) sub.push(run.status);
    else if (run.endless) sub.push(t('endless'));
    sub.push(ago(run.lastPlayedAgo));
    var count = (run.backups || []).length;
    var pips = '';
    if (isPlaying(run)) pips += '<span class="pip play">' + esc(tf('pip', 'playing')) + '</span>';
    else if (run.behind) pips += '<span class="pip chk">' + esc(tf('pip', 'check')) + '</span>';
    else if (run.finished) pips += '<span class="pip end">' + esc(tf('pip', 'ending')) + '</span>';
    pips += count ? '<span class="pip n">' + count + '</span>' : '<span class="pip zero">0</span>';
    return '<div class="save' + (run.stem === ui.selected ? ' sel' : '') + '" data-run="' + esc(run.stem) + '">' + face(run, 'face', true) +
      '<div style="min-width:0"><div class="sname">' + (run.pinned ? '<span class="star">&#9733;</span>' : '') + esc(title(run)) + '</div>' +
      '<div class="ssub">' + esc(survivorName(run) + DOT + sub.join(DOT)) + '</div></div><div class="pips">' + pips + '</div></div>';
  }

  function listHtml() {
    var shown = sorted(runs().filter(matches));
    var pinned = shown.filter(function (run) { return run.pinned; });
    var rest = shown.filter(function (run) { return !run.pinned; });
    var html = '';
    if (pinned.length) html += '<div class="grp"><span>' + esc(t('grpPinned')) + ' &middot; ' + pinned.length + '</span></div>' + pinned.map(saveRow).join('');
    html += '<div class="grp"><span>' + esc(t('grpSaves')) + ' &middot; ' + rest.length + '</span><span class="sort" data-act="sort">' + esc(t('grpSort', tf('sorts', ui.sortBy))) + ' &#9662;</span></div>' + rest.map(saveRow).join('');
    if (!shown.length) html += '<div class="empty-list">' + esc(runs().length ? t('noMatch') : t('noRuns')) + '</div>';
    if (!view.inRun) html += '<div class="newgame" data-act="newgame">' + esc(t('newGame')) + '</div>';
    return html;
  }

  function rail() {
    var counts = { all: runs().length, live: 0, end: 0, warn: 0 };
    runs().forEach(function (run) {
      if (run.finished) counts.end++;
      else counts.live++;
      if (run.behind) counts.warn++;
    });
    var chips = FILTERS.filter(function (k) { return k !== 'warn' || counts.warn; }).map(function (k) {
      return '<button class="chip' + (ui.filter === k ? ' on' : '') + (k === 'warn' ? ' warn' : '') + '" data-filter="' + k + '">' + esc(tf('filters', k)) + '<i>' + counts[k] + '</i></button>';
    }).join('');
    var query = drafts.query != null ? drafts.query : ui.query;
    return '<div class="rail"><div class="search">' + SEARCH +
      '<input data-in="query" data-focus="q" type="text" spellcheck="false" placeholder="' + esc(t('search')) + '" value="' + esc(query) + '"></div>' +
      '<div class="chips">' + chips + '</div><div class="list">' + listHtml() + '</div>' + storeBlock() + '</div>';
  }

  function storeBlock() {
    var limit = view.limit || 0;
    var used = view.bytes || 0;
    var trash = trashBytes();
    var pct = function (b) { return limit ? Math.min(100, b / limit * 100).toFixed(1) : 0; };
    return '<div class="store"><div class="store-t"><span>' + esc(t('storage')) + '</span><b>' + esc(limit ? t('storageOf', size(used + trash), size(limit)) : size(used + trash)) + '</b></div>' +
      '<div class="bar"><i class="b1" style="width:' + pct(used) + '%"></i><i class="b3" style="width:' + pct(trash) + '%"></i></div>' +
      '<div class="store-l"><span style="--c:#4fa8ff">' + esc(t('storage') + ' ' + size(used)) + '</span><span style="--c:#666">' + esc(t('storageBin', size(trash))) + '</span><span style="--c:transparent">' + esc(t('storageSaves', size(view.saveBytes))) + '</span></div></div>';
  }

  // ---------- the picked save ----------

  function guard(run) {
    var newest = (run.backups || [])[0];
    if (isPlaying(run)) return '<div class="guard play"><div class="ic">' + PLAY + '</div><div class="lbl">' + esc(t('playingNow')) + '<small>' + esc(newest ? t('safe') + ': ' + whereAt(newest) : t('noneYet')) + '</small></div></div>';
    if (run.behind) return '<div class="guard warn"><div class="ic">' + ALERT + '</div><div class="lbl">' + esc(t('wentBack')) + '<small>' + esc(t('wentBackSub')) + '</small></div></div>';
    if (!newest) return '<div class="guard none"><div class="ic">' + CLOCK + '</div><div class="lbl">' + esc(t('noneYet')) + '<small>' + esc(t('noneYetSub')) + '</small></div></div>';
    return '<div class="guard"><div class="ic">' + SHIELD + '</div><div class="lbl">' + esc(t('safe')) + '<small>' + esc(t('safeSub', whereAt(newest), kindName(newest.kind))) + '</small></div></div>';
  }

  function hero(run) {
    var mode = [survivorName(run), run.diff].filter(Boolean);
    if (run.endless) mode.push(t('endless'));
    var reached = whereAt(run) + (run.finished && run.status ? DOT + run.status : '');
    var meta = [t('played', playTime(run.played)), t('lastPlayed', ago(run.lastPlayedAgo)), t('game', view.version || '-'), t('withBackups', size((run.saveBytes || 0) + backupBytes(run)))];
    var big = '';
    if (!isPlaying(run)) big += '<button class="btn pri lg" data-act="load"' + (view.inRun ? ' disabled' : '') + '>' + esc(run.finished ? t('seeEnding') : t('cont')) + '</button>';
    big += '<button class="btn blue lg" data-act="backup">' + esc(t('backUp')) + '</button>';
    return '<div class="hero"><div class="banner" style="' + bgUrl(runPic(run)) + '"></div><div class="shade"></div><div class="glow"></div>' +
      '<div class="gen">' + face(run, 'big') + '<div class="ttl"><div class="gt">' + esc(title(run)) + '</div>' +
      '<div class="gs">' + esc(mode.join(DOT)) + DOT + '<b>' + esc(reached) + '</b></div>' +
      '<div class="gm">' + meta.map(function (m) { return '<span>' + esc(m) + '</span>'; }).join('') + '</div></div></div>' +
      '<div class="hero-side">' + guard(run) + '<div class="big-btns">' + big + '</div></div></div>';
  }

  function tools(run) {
    var playing = isPlaying(run);
    var note = drafts['note:' + run.stem] != null ? drafts['note:' + run.stem] : run.note || '';
    return '<div class="tools"><input class="note-in" data-note="' + esc(run.stem) + '" data-focus="runnote" maxlength="300" spellcheck="false" placeholder="' + esc(t('notePh')) + '" value="' + esc(note) + '">' +
      '<span class="sp"></span>' +
      (playing ? '' : '<button class="btn sm" data-act="rename">' + esc(t('rename')) + '</button>') +
      '<button class="btn sm' + (run.pinned ? ' on' : '') + '" data-act="runpin">' + (run.pinned ? '&#9733; ' + esc(t('unpin')) : '&#9734; ' + esc(t('pin'))) + '</button>' +
      '<button class="btn sm" data-act="duplicate">' + esc(t('duplicate')) + '</button>' +
      '<button class="btn sm" data-act="export">' + esc(t('exportBtn')) + '</button>' +
      '<button class="btn sm" data-act="runfolder">' + esc(t('openFolder')) + '</button>' +
      (playing ? '' : '<button class="btn sm danger" data-act="delrun">' + esc(t('del')) + '</button>') + '</div>';
  }

  function notices(run) {
    var html = '';
    if (isPlaying(run)) html += '<div class="notice run"><span class="ico">&#9656;</span><div>' + esc(t('playingHint')) + '</div></div>';
    else if (view.inRun) html += '<div class="notice run"><span class="ico">&#9656;</span><div>' + esc(t('otherHint')) + '</div></div>';
    if (run.behind) {
      var ahead = backupById(run, run.behind.id);
      html += '<div class="notice warn"><span class="ico">!</span><div><b>' + esc(t('behindTitle')) + '</b> ' + esc(t('behindBody', where(run), ahead ? ahead.at : '-', where(run.behind))) +
        '<div class="acts">' + (ahead ? '<button class="btn sm pri" data-act="cloud">' + esc(t('lookBoth')) + '</button>' : '') + '<button class="btn sm" data-act="dismiss">' + esc(t('dismiss')) + '</button></div></div></div>';
    }
    return html;
  }

  // Every point a save can go back to on the open tab: the save right now and its backups, or the game's day
  // snapshots, newest first.
  function points(run) {
    var d = detailOf(run);
    var list = [];
    if (ui.sub === 'backups') {
      list.push({ key: 'now', kind: 'now', turn: run.turn, day: run.day, clock: run.clock, isNow: true, shot: runPic(run), stats: d && d.now ? d.now : null });
      (run.backups || []).forEach(function (b, i) {
        if (ui.bkFilter === 'auto' && !b.auto) return;
        if (ui.bkFilter === 'mine' && b.kind !== 'manual') return;
        if (ui.bkFilter === 'kept' && !b.pinned) return;
        list.push({ key: b.id, kind: b.kind, turn: b.turn, day: b.day, clock: b.clock, backup: b, isNewest: i === 0, shot: shotOf(run.stem, b), stats: b.stats || null });
      });
    } else if (ui.sub === 'snaps' && d) {
      (d.snaps || []).slice().reverse().forEach(function (s) {
        list.push({ key: 'snap:' + s.file, kind: s.round ? 'round' : 'snap', turn: s.turn, day: s.day, clock: s.clock, snap: s, shot: '', stats: s.stats || null });
      });
    }
    return list;
  }

  function pickedPoint(pts) {
    for (var i = 0; i < pts.length; i++) if (pts[i].key === ui.pointKey) return pts[i];
    var first = pts.length > 1 && pts[0].isNow ? pts[1] : pts[0];
    ui.pointKey = first ? first.key : null;
    return first || null;
  }

  function currentPoint() {
    var run = selRun();
    if (!run) return null;
    var pts = points(run);
    for (var i = 0; i < pts.length; i++) if (pts[i].key === ui.pointKey) return pts[i];
    return null;
  }

  function pointRow(p, sel) {
    var b = p.backup;
    var l2 = [];
    if (p.isNow) l2.push(t('nowRow'));
    if (b) { l2.push(b.at); l2.push(b.auto ? t('madeAuto') : t('madeYou')); l2.push(size(b.bytes)); }
    if (p.snap) { l2.push(p.snap.at); l2.push(t('byGame')); }
    var rt = '';
    if (p.isNow) rt += '<span class="pip nowp">' + esc(kindName('now')) + '</span>';
    if (p.isNewest) rt += '<span class="pip new">' + esc(t('newest')) + '</span>';
    if (b && b.oldVersion) rt += '<span class="pip old">' + esc(t('older')) + '</span>';
    if (b && b.pinned) rt += '<span class="kept">&#9733;</span>';
    var th = '<div class="th' + (p.shot ? '" style="' + bgUrl(p.shot) : ' blank') + '">' + kindDot(p.kind) + '</div>';
    return '<div class="pt' + (p.isNow ? ' now' : '') + (sel && sel.key === p.key ? ' sel' : '') + '" data-pt="' + esc(p.key) + '">' + th +
      '<div style="min-width:0"><div class="l1"><span class="tm">' + esc(p.clock) + '</span><span class="kn">' + esc(kindName(p.kind)) + '</span>' +
      (b && b.note ? '<span class="nt">' + esc(b.note) + '</span>' : '') + '</div><div class="l2">' + esc(l2.join(DOT)) + '</div></div><div class="rt">' + rt + '</div></div>';
  }

  function backupList(run, pts, sel) {
    var html = ui.sub === 'snaps' ? '<div class="intro">' + esc(t('snapsIntro')) + '</div>' : '';
    var last = '';
    pts.forEach(function (p) {
      var group = p.isNow ? 'now' : p.turn + ':' + p.day;
      if (group !== last && !p.isNow) html += '<div class="dayh"><span>' + esc(t('day', p.day)) + '</span>' + (p.turn > 1 || run.turn > 1 ? '<small>' + esc(t('loop', p.turn)) + '</small>' : '') + '</div>';
      last = group;
      html += pointRow(p, sel);
    });
    if (pts.length <= (ui.sub === 'backups' ? 1 : 0)) html += '<div class="intro">' + esc(t('noKind')) + '</div>';
    return html;
  }

  function nowCompare(isNowPoint, then, now) {
    if (isNowPoint || now == null || then == null) return '';
    var d = Math.round(then - now);
    return '<small class="' + (d > 0 ? 'up' : d < 0 ? 'dn' : '') + '">' + esc(t('nowVal', Math.round(now))) + '</small>';
  }

  function meter(key, stats, nowStats, isNowPoint) {
    var v = stats[key] || [0, 0];
    var max = v[1] || 100;
    var now = nowStats && nowStats[key] ? nowStats[key][0] : null;
    var tick = !isNowPoint && now != null ? '<u style="left:calc(' + Math.min(100, now / max * 100) + '% - 1px)"></u>' : '';
    return '<div class="meter"><span class="ml">' + esc(tf('st', key)) + '</span><div class="mt"><i style="width:' + Math.min(100, v[0] / max * 100) + '%;background:' + METER_COLORS[key] + '"></i>' + tick + '</div>' +
      '<span class="mv">' + Math.round(v[0]) + (v[1] ? ' / ' + v[1] : '') + nowCompare(isNowPoint, v[0], now) + '</span></div>';
  }

  function tile(key, stats, nowStats, isNowPoint) {
    var v = stats[key] || 0;
    return '<div class="tile"><span>' + esc(tf('st', key)) + '</span><b>' + v + '</b>' + nowCompare(isNowPoint, v, nowStats ? nowStats[key] : null) + '</div>';
  }

  function zoomAttrs(small) {
    return ' data-act="zoom" data-src="' + esc(largeOf(small)) + '" data-small="' + esc(small) + '"';
  }

  function inspector(run, p) {
    if (!p) return '<div class="insp"></div>';
    var b = p.backup;
    var d = detailOf(run);
    var nowStats = d ? d.now : null;
    var shot = p.shot ? '<div class="shot zoomable"' + zoomAttrs(p.shot) + ' style="' + bgUrl(p.shot) + '"><div class="tag"><span>' + kindDot(p.kind) + esc(kindName(p.kind)) + '</span></div></div>' :
      '<div class="shot blank"><div><b>' + esc(t('noPicture')) + '</b><small>' + esc(p.snap ? t('noPictureSnap') : t('noPictureWhy')) + '</small></div></div>';
    var html = '<div class="insp">' + shot + '<div class="ih"><h3>' + esc(whereAt(p)) + '</h3><div class="sub">' + kindDot(p.kind) + esc(p.isNow ? t('thisIsNow') : kindName(p.kind)) + '</div></div>';

    if (p.isNow) {
      html += '<div class="ia"><button class="btn blue" data-act="backup" style="flex:1">' + esc(t('backUp')) + '</button></div>';
    } else if (b) {
      html += '<div class="ia"><button class="btn pri lg" data-act="restore">' + esc(t('restore')) + '</button><button class="btn lg" data-act="compare">' + esc(t('compare')) + '</button></div>' +
        '<div class="ia2"><button class="btn sm' + (b.pinned ? ' on' : '') + '" data-act="keep">' + (b.pinned ? '&#9733; ' + esc(t('unkeep')) : '&#9734; ' + esc(t('keep'))) + '</button>' +
        '<button class="btn sm" data-act="ptnote">' + esc(t('noteBtn')) + '</button><button class="btn sm" data-act="ptexport">' + esc(t('exportBtn')) + '</button>' +
        '<button class="btn sm danger" data-act="delpt">' + esc(t('del')) + '</button></div>';
      if (b.note) html += '<div class="nb">' + esc(b.note) + '</div>';
      if (b.oldVersion) html += '<div class="ow">' + esc(t('oldWarn', b.version, view.version)) + '</div>';
    } else if (p.snap) {
      html += '<div class="ia"><button class="btn pri lg" data-act="snapnew">' + esc(t('playAsNew')) + '</button></div><div class="ia2"><button class="btn" data-act="compare">' + esc(t('compare')) + '</button></div>';
    }

    if (p.stats) {
      html += '<div class="sec-t"><span>' + esc(p.isNow ? t('atNow') : p.snap ? t('atSnap') : t('atThis')) + '</span></div>' +
        METERS.map(function (k) { return meter(k, p.stats, nowStats, p.isNow); }).join('') +
        '<div class="tiles">' + TILES.map(function (k) { return tile(k, p.stats, nowStats, p.isNow); }).join('') + '</div>';
    } else if (!d) {
      html += '<div class="sec-t"><span>' + esc(t('reading')) + '</span></div>';
    }
    var facts = [];
    if (b) facts.push(t('madeAt', b.at), b.auto ? t('madeAuto') : t('madeYou'), size(b.bytes), t('game', b.version || '-'));
    if (p.snap) facts.push(t('made', p.snap.at), t('byGame'), size(p.snap.bytes));
    if (facts.length) html += '<div class="facts">' + esc(facts.join(DOT)) + '</div>';
    return html + '</div>';
  }

  function howItWorks() {
    var key = setting('Controls.BackupKey');
    return '<div class="how"><h4>' + esc(t('howTitle')) + '</h4><div class="how-cards">' +
      [['01', 'how1', 'how1b'], ['02', 'how2', 'how2b'], ['03', 'how3', 'how3b']].map(function (c) {
        return '<div class="hc"><span class="no">' + c[0] + '</span><div><b>' + esc(t(c[1])) + '</b><small>' + esc(t(c[2])) + '</small></div></div>';
      }).join('') + '</div><div class="ft-note">' + esc(key && key !== 'None' ? t('howKey', key) : t('howPause')) + '</div></div>';
  }

  function workArea(run) {
    var d = detailOf(run);
    var snaps = d && isOn('Screen.ShowGameSnapshots') ? d.snaps || [] : [];
    var photos = d ? d.photos || [] : [];
    if (ui.sub === 'snaps' && !snaps.length) ui.sub = 'backups';
    if (ui.sub === 'photos' && !photos.length) ui.sub = 'backups';
    var count = (run.backups || []).length;
    var tabs = [['backups', t('tabBackups'), count]];
    if (snaps.length) tabs.push(['snaps', t('tabSnaps'), snaps.length]);
    if (photos.length) tabs.push(['photos', t('tabPhotos'), photos.length]);
    var chips = ui.sub === 'backups' && count ? '<div class="chips">' + ['all', 'auto', 'mine', 'kept'].map(function (k) {
      return '<button class="chip' + (ui.bkFilter === k ? ' on' : '') + '" data-bkf="' + k + '">' + esc(tf('bkFilter', k)) + '</button>';
    }).join('') + '</div>' : '';
    var head = '<div class="tabs">' + tabs.map(function (x) {
      return '<div class="tab' + (ui.sub === x[0] ? ' on' : '') + '" data-sub="' + x[0] + '">' + esc(x[1]) + ' <i>' + x[2] + '</i></div>';
    }).join('') + '<span class="sp"></span>' + chips + '</div>';

    if (ui.sub === 'photos') {
      return head + '<div class="work" style="grid-template-columns:1fr"><div class="bk"><div class="intro">' + esc(t('photosIntro')) + '</div><div class="photos">' +
        photos.map(function (url) { return '<div class="zoomable" data-act="zoom" data-src="' + esc(url) + '" style="' + bgUrl(url) + '"></div>'; }).join('') + '</div></div></div>';
    }
    var pts = points(run);
    if (ui.sub === 'backups' && !count) {
      ui.pointKey = 'now';
      return head + '<div class="work"><div class="bk">' + pointRow(pts[0], pts[0]) + howItWorks() + '</div>' + inspector(run, pts[0]) + '</div>';
    }
    var sel = pickedPoint(pts);
    return head + '<div class="work"><div class="bk">' + backupList(run, pts, sel) + '</div>' + inspector(run, sel) + '</div>';
  }

  function savesBody() {
    var run = selRun();
    if (!run) return '<div class="bd">' + rail() + '<div class="pane"><div class="empty-list">' + esc(t('noRuns')) + '</div></div></div>';
    return '<div class="bd">' + rail() + '<div class="pane">' + hero(run) + tools(run) + notices(run) + workArea(run) + '</div></div>';
  }

  // ---------- trash ----------

  function leftBar(days) {
    if (days < 0) return '<div class="left">' + esc(t('untilEmptied')) + '</div>';
    var max = Number(view.binDays) || 30;
    return '<div class="left' + (days <= 5 ? ' low' : '') + '">' + esc(t('daysLeft', days)) + '<div class="bar"><i style="width:' + Math.max(3, Math.min(100, days / max * 100)) + '%"></i></div></div>';
  }

  function trashBody() {
    var deleted = view.deleted || [];
    var bin = view.bin || [];
    var intro = view.binDays > 0 ? t('binIntro', view.binDays) : t('binIntroKeep');
    var html = '<div class="binp"><div class="inner"><div class="binhead"><div><div class="t">' + esc(t('tabBin')) + '</div><p>' + esc(intro) + '</p></div><span class="sp"></span>' +
      (trashCount() ? '<button class="btn danger" data-act="emptybin">' + esc(t('emptyBin', size(trashBytes()))) + '</button>' : '') + '</div>';
    if (!trashCount()) return html + '<div class="bin-empty">' + esc(t('binEmpty')) + '</div></div></div>';
    if (deleted.length) {
      html += '<div class="sec-t" style="margin-top:26px"><span>' + esc(t('binSaves')) + ' &middot; ' + deleted.length + '</span></div>';
      deleted.forEach(function (run) {
        var shot = newestShot(run.stem, run.backups);
        var newest = (run.backups || [])[0];
        html += '<div class="brow"><div class="th' + (shot ? ' zoomable"' + zoomAttrs(shot) + ' style="' + bgUrl(shot) + '"' : '"') + '>' + face(run) + '</div>' +
          '<div><div class="bt">' + esc(title(run)) + '</div><div class="bs">' + esc([survivorName(run), where(run), t('backupsN', (run.backups || []).length), size(backupBytes(run))].join(DOT)) + '</div><div class="bs2">' + esc(t('goneRun')) + '</div></div>' +
          leftBar(run.daysLeft) + '<div class="acts">' + (newest ? '<button class="btn blue" data-act="bringback" data-stem="' + esc(run.stem) + '" data-id="' + esc(newest.id) + '">' + esc(t('putBack')) + '</button>' : '') +
          '<button class="btn danger" data-act="binrundel" data-stem="' + esc(run.stem) + '">' + esc(t('deleteNow')) + '</button></div></div>';
      });
    }
    if (bin.length) {
      html += '<div class="sec-t" style="margin-top:26px"><span>' + esc(t('binBackups')) + ' &middot; ' + bin.length + '</span></div>';
      bin.forEach(function (e) {
        var shot = binShot(e);
        var ids = ' data-stem="' + esc(e.stem) + '" data-id="' + esc(e.id) + '"';
        html += '<div class="brow"><div class="th' + (shot ? ' zoomable"' + zoomAttrs(shot) + ' style="' + bgUrl(shot) + '"' : ' blank"') + '></div>' +
          '<div><div class="bt">' + esc(title(e)) + DOT + esc(whereAt(e)) + '</div><div class="bs">' + esc([kindName(e.kind), e.at, size(e.bytes), e.why === 'thinned' ? t('thinned') : t('deletedWhy')].join(DOT)) + '</div></div>' +
          leftBar(e.daysLeft) + '<div class="acts"><button class="btn blue" data-act="binback"' + ids + '>' + esc(t('putBack')) + '</button><button class="btn danger" data-act="bindel"' + ids + '>' + esc(t('deleteNow')) + '</button></div></div>';
      });
    }
    return html + '</div></div>';
  }

  // ---------- settings ----------

  function tog(id) {
    return '<div class="tog' + (isOn(id) ? ' on' : '') + '" data-act="tg" data-id="' + id + '"><div class="track"></div><div class="knob"></div></div>';
  }

  function num(id, width) {
    var value = drafts['set:' + id] != null ? drafts['set:' + id] : setting(id);
    return '<input class="num" data-set="' + id + '" data-focus="set:' + id + '" type="text" maxlength="7" spellcheck="false" value="' + esc(value) + '"' + (width ? ' style="width:' + width + 'px"' : '') + '>';
  }

  function lab(quad) {
    var main = isZh() ? quad[1] : quad[0];
    var hint = isZh() ? quad[3] : quad[2];
    return '<div><div class="lab">' + esc(main) + '</div>' + (hint ? '<div class="hint">' + esc(hint) + '</div>' : '') + '</div>';
  }

  function setNavSub(i) {
    if (i === 0) return fill(pick(TEXT.setNavSub[0]), [TRIGGERS.filter(function (x) { return x[1] && isOn(x[1]); }).length]);
    if (i === 1) return fill(pick(TEXT.setNavSub[1]), [setting('Keep.KeepAllDays')]);
    if (i === 2) return setting('Backups.Folder') || pick(TEXT.setNavSub[2]);
    return pick(TEXT.setNavSub[3]);
  }

  function settingsRail() {
    return '<div class="rail snav"><div style="height:12px"></div>' + TEXT.setNav.map(function (n, i) {
      return '<div class="item' + (ui.setSec === i ? ' sel' : '') + '" data-sec="' + i + '"><div class="no">0' + (i + 1) + '</div><div><div class="nm">' + esc(pick(n)) + '</div><div class="ns">' + esc(setNavSub(i)) + '</div></div></div>';
    }).join('') + '<div style="flex:1"></div>' + storeBlock() + '</div>';
  }

  function setHero(i, intro) {
    return '<div class="sets-hero"><div class="no">0' + (i + 1) + '</div><div><h2>' + esc(pick(TEXT.setNav[i])) + '</h2>' + (intro ? '<p>' + esc(intro) + '</p>' : '') + '</div></div>';
  }

  function whenSection() {
    return setHero(0, t('setWhenEx')) + TRIGGERS.map(function (x) {
      var on = !x[1] || isOn(x[1]);
      var ctl = '';
      if (x[0] === 'timer') ctl += '<span>' + esc(t('every')) + '</span>' + num('Backups.TimerMinutes', 60) + '<span>' + esc(t('minutes')) + '</span><span style="width:14px"></span>';
      ctl += '<span>' + esc(t('keepN')) + '</span>' + num(x[2], 60) + '<span class="all">' + (setting(x[2]) === '0' ? esc(tf('bkFilter', 'all')) : '') + '</span>';
      return '<div class="row' + (on ? '' : ' off') + '">' + (x[1] ? tog(x[1]) : '<span class="ctl"><span class="always">' + esc(t('always')) + '</span></span>') + lab(TEXT.trig[x[0]]) + '<div class="ctl">' + ctl + '</div></div>';
    }).join('');
  }

  // A strip of ticks for the thinning ladder: every backup kept in the newest days, one a day after, one per loop.
  function density() {
    var all = Number(setting('Keep.KeepAllDays')) || 0;
    var daily = Number(setting('Keep.DailyDays')) || 0;
    var span = Math.max(30, all + daily + 8);
    var x = function (day) { return (100 - day / span * 100).toFixed(2); };
    var w = function (n) { return (n / span * 100).toFixed(2) + '%'; };
    var ticks = '';
    for (var d = 0; d < all; d++) for (var k = 0; k < 4; k++) ticks += '<i style="left:' + x(d + k * 0.24) + '%;height:' + (60 + k * 10) + '%"></i>';
    for (var e = all; e < all + daily; e++) ticks += '<i style="left:' + x(e) + '%;height:55%"></i>';
    if (isOn('Keep.PerLoop')) [all + daily + 3, all + daily + 7, span - 1].forEach(function (p) { if (p < span) ticks += '<i class="k" style="left:' + x(p) + '%;height:100%"></i>'; });
    return '<div class="density"><div class="ticks">' + ticks + '</div>' +
      '<div class="zones"><span style="width:' + w(span - all - daily) + '">03</span><span style="width:' + w(daily) + '">02</span><span style="width:' + w(all) + '">01</span></div>' +
      '<div class="ends"><span>' + esc(t('ladOld')) + '</span><span>' + esc(t('ladNow')) + '</span></div></div>';
  }

  function keepSection() {
    var ladder = '<div class="ladder">' +
      '<div class="rung"><div class="no">01</div><div class="rt">' + esc(t('lad1a')) + num('Keep.KeepAllDays', 64) + esc(t('lad1b')) + '</div></div><div class="arrow">&rsaquo;</div>' +
      '<div class="rung"><div class="no">02</div><div class="rt">' + esc(t('lad2a')) + num('Keep.DailyDays', 64) + esc(t('lad2b')) + '</div></div><div class="arrow">&rsaquo;</div>' +
      '<div class="rung"><div class="no">03</div><div class="rt">' + esc(t('lad3a')) + tog('Keep.PerLoop') + '</div></div></div>';
    var limit = view.limit || 0;
    var used = (view.bytes || 0) + trashBytes();
    return setHero(1, t('ladderEx')) + ladder + density() +
      '<div class="info"><span class="ico">&#9733;</span><div>' + esc(t('ladKept')) + '</div></div>' +
      '<div class="sec-t" style="margin-top:28px"><span>' + esc(t('storage')) + '</span></div>' +
      '<div class="row notog">' + lab(TEXT.maxMb) + '<div class="ctl">' + num('Keep.MaxTotalMB', 90) + '<span>' + esc(t('mb')) + '</span></div></div>' +
      (limit ? '<div style="padding:4px 10px 12px"><div class="bar"><i class="b1" style="width:' + Math.min(100, used / limit * 100) + '%"></i></div><div class="store-l"><span style="--c:#4fa8ff">' + esc(t('storageOf', size(used), size(limit))) + '</span></div></div>' : '') +
      '<div class="row notog">' + lab(TEXT.binDays) + '<div class="ctl">' + num('Keep.TrashDays', 70) + '<span>' + esc(t('days')) + '</span></div></div>';
  }

  function pathRow(id, quad, placeholder) {
    var value = drafts['set:' + id] != null ? drafts['set:' + id] : setting(id);
    return '<div class="row notog" style="align-items:start">' + lab(quad) + '<div class="ctl" style="flex-direction:column;align-items:flex-end;gap:8px"><div style="display:flex;gap:8px">' +
      '<input class="path" data-path="' + id + '" data-focus="set:' + id + '" type="text" spellcheck="false" value="' + esc(value) + '" placeholder="' + esc(placeholder) + '">' +
      '<button class="btn" data-act="setpath" data-id="' + id + '">' + esc(t('setPath')) + '</button></div></div></div>';
  }

  function whereSection() {
    return setHero(2, '') + pathRow('Backups.Folder', TEXT.folder, view.folder || '') + pathRow('Backups.SecondFolder', TEXT.second, 'D:\\Backups\\Survival Log') +
      '<div class="row notog">' + lab(TEXT.openNow) + '<div class="ctl"><button class="btn" data-act="folder">' + esc(t('openFolder')) + '</button></div></div>' +
      '<div class="info"><span class="ico">i</span><div>' + esc(t('cloudNote')) + '</div></div>';
  }

  function extrasSection() {
    var key = setting('Controls.BackupKey');
    var keyText = ui.isListening ? t('keyPress') : !key || key === 'None' ? t('keyNone') : key;
    var rows = [['Backups.TakePictures', TEXT.pics], ['Backups.Verify', TEXT.verify], ['Screen.WarnWhenBehind', TEXT.warnBehind], ['Screen.ShowGameSnapshots', TEXT.snapsRow], ['Screen.ReplaceLoadScreen', TEXT.replaceScreen], ['Toasts.AutoBackupToasts', TEXT.toasts]];
    return setHero(3, '') + '<div class="row notog">' + lab(TEXT.keyRow) + '<div class="ctl"><button class="keycap' + (ui.isListening ? ' listen' : '') + '" data-act="listen" data-key="1">' + esc(keyText) + '</button></div></div>' +
      '<div class="row notog">' + lab(TEXT.uiScale) + '<div class="ctl">' + num('Interface.UiScale', 64) + '<span>' + esc(t('percent')) + '</span></div></div>' +
      rows.map(function (x) { return '<div class="row">' + tog(x[0]) + lab(x[1]) + '<div></div></div>'; }).join('');
  }

  function settingsBody() {
    var section = [whenSection, keepSection, whereSection, extrasSection][ui.setSec]();
    return '<div class="bd">' + settingsRail() + '<div class="sets">' + section + '</div></div>';
  }

  // ---------- header and footer ----------

  function header() {
    var count = trashCount();
    var tabs = [['saves', t('tabSaves'), ''], ['trash', t('tabBin'), count || ''], ['settings', t('tabSettings'), '']];
    var playing = view.inRun ? findRun(view.inRun) : null;
    var deco = '<span>' + esc(t('deco')) + '</span><span><b>' + esc(t('decoSaves', runs().length)) + '</b></span><span><b>' + esc(t('decoBackups', totalBackups())) + '</b></span>';
    if (playing) deco += '<span class="run">' + esc(t('decoPlaying', title(playing))) + '</span>';
    return '<div class="hd"><div class="hd-icon"></div><div class="hd-tabs">' + tabs.map(function (x) {
      return '<div class="hd-title' + (ui.tab === x[0] ? ' on' : '') + '" data-tab="' + x[0] + '">' + esc(x[1]) + (x[2] !== '' ? '<i>' + x[2] + '</i>' : '') + '</div>';
    }).join('') + '</div><div class="hd-deco">' + deco + '</div><span class="hd-sp"></span>' +
      '<span class="hint-k"><span class="kbd">ESC</span>' + esc(t('close')) + '</span><span class="close-btn" data-act="close">&times;</span></div>';
  }

  function footer() {
    var newest = null;
    var newestRun = null;
    var backed = 0;
    var warn = 0;
    runs().forEach(function (run) {
      var b = (run.backups || [])[0];
      if (b) backed++;
      if (run.behind) warn++;
      if (b && (!newest || b.at > newest.at)) { newest = b; newestRun = run; }
    });
    var text;
    var dot = 'status-dot';
    if (view.inRun) { text = esc(t('footRun')); dot += ' run'; }
    else if (!newest) text = esc(t('footNone', runs().length));
    else {
      var total = runs().length;
      text = esc(backed === total ? t('footOk', total, title(newestRun), mid(whereAt(newest))) : t('footSome', backed, total, title(newestRun), mid(whereAt(newest))));
      if (warn) { text += ' <em>' + esc(t('footWarn', warn)) + '</em>'; dot += ' warn'; }
    }
    return '<div class="ft"><span class="' + dot + '"></span><span class="ft-t">' + text + '</span>' +
      '<button class="btn" data-act="import">' + esc(t('importBtn')) + '</button>' +
      '<button class="btn" data-act="folder">' + esc(t('openFolder')) + '</button>' +
      (view.inRun ? '' : '<button class="btn" data-act="gamelist">' + esc(t('defaultLoad')) + '</button>') + '</div>';
  }

  // ---------- dialogs ----------

  function frame(heading, body, foot, isWide) {
    return '<div class="modal" data-act="backdrop"><div class="mbox' + (isWide ? ' wide' : '') + '"><div class="corner tl"></div><div class="corner tr"></div><div class="corner bl"></div><div class="corner br"></div>' +
      '<div class="mhd"><h2>' + esc(heading) + '</h2><span class="sp"></span><span class="close-btn" data-act="mclose">&times;</span></div>' +
      '<div class="mbd">' + body + '</div><div class="mft">' + foot + '</div></div></div>';
  }

  function btn(text, cls, act, attrs) {
    return '<button class="btn' + (cls ? ' ' + cls : '') + '" data-act="' + act + '"' + (attrs || '') + '>' + esc(text) + '</button>';
  }

  // A picture in a dialog; a still one sits inside a choice that is itself clicked, so it does not enlarge.
  function shotBox(url, isStill) {
    if (!url) return '<div class="shot blank"><div><b>' + esc(t('noPicture')) + '</b></div></div>';
    return '<div class="shot' + (isStill ? '"' : ' zoomable"' + zoomAttrs(url)) + ' style="' + bgUrl(url) + '"></div>';
  }

  function restoreModal(run, p) {
    var playing = isPlaying(run);
    var asNew = playing || ui.modal.asNew !== false;
    var strip = '<div class="strip"><div>' + shotBox(p.shot) + '<div class="cap">' + esc(kindName(p.kind)) + '<b>' + esc(whereAt(p)) + '</b></div></div><div class="arr">&larr;</div>' +
      '<div>' + shotBox(runPic(run)) + '<div class="cap">' + esc(t('restoreNow')) + '<b>' + esc(whereAt(run)) + '</b></div></div></div>';
    var body = strip + '<div class="choice">' +
      '<div class="opt' + (asNew ? ' on' : '') + '" data-act="asnew" data-v="1"><div class="ot"><span class="radio"></span>' + esc(t('asNew')) + '<span class="pip n">' + esc(t('safest')) + '</span></div><small>' + esc(t('asNewBody', whereAt(p), title(run))) + '</small></div>' +
      '<div class="opt' + (!asNew ? ' on' : '') + (playing ? ' off' : '') + '" data-act="asnew" data-v="0"><div class="ot"><span class="radio"></span>' + esc(t('replace')) + '</div><small>' + esc(playing ? t('replaceBlocked') : t('replaceBody', whereAt(p), whereAt(run))) + '</small></div></div>' +
      (p.backup.oldVersion ? '<div class="ow">' + esc(t('oldWarn', p.backup.version, view.version)) + '</div>' : '');
    return frame(t('restoreTitle'), body, btn(t('cancel'), '', 'mclose') + btn(asNew ? t('doAsNew') : t('doReplace', where(p)), 'pri', 'dorestore'), true);
  }

  function statValue(stats, key) {
    var v = stats ? stats[key] : null;
    return Array.isArray(v) ? v[0] : v;
  }

  function statRows(a, aStats, b, bStats, headA, headB) {
    var change = function (d) { return '<td class="' + (d > 0 ? 'up' : d < 0 ? 'dn' : '') + '">' + (d ? (d > 0 ? '+' : '') + d : esc(t('same'))) + '</td>'; };
    var rows = '<tr><th></th><th>' + esc(headA) + '</th><th>' + esc(headB) + '</th><th>' + esc(t('change')) + '</th></tr>';
    rows += '<tr><td>' + esc(tf('st', 'day')) + '</td><td>' + esc(where(a)) + '</td><td>' + esc(where(b)) + '</td>' + change(a.turn === b.turn ? b.day - a.day : 0) + '</tr>';
    if (aStats && bStats) {
      METERS.concat(TILES).forEach(function (k) {
        var x = statValue(aStats, k) || 0;
        var y = statValue(bStats, k) || 0;
        rows += '<tr><td>' + esc(tf('st', k)) + '</td><td>' + Math.round(x) + '</td><td>' + Math.round(y) + '</td>' + change(Math.round(y - x)) + '</tr>';
      });
    }
    return '<table class="cmp">' + rows + '</table>';
  }

  function itemDiff(thenItems, nowItems, names) {
    var ids = {};
    Object.keys(thenItems || {}).forEach(function (k) { ids[k] = 1; });
    Object.keys(nowItems || {}).forEach(function (k) { ids[k] = 1; });
    var changes = Object.keys(ids).map(function (id) { return { id: id, d: ((nowItems || {})[id] || 0) - ((thenItems || {})[id] || 0) }; })
      .filter(function (c) { return c.d; })
      .sort(function (a, b) { return Math.abs(b.d) - Math.abs(a.d); });
    if (!changes.length) return '<div class="intro">' + esc(t('noItemChange')) + '</div>';
    var card = function (c) {
      var it = (names || {})[c.id] || ['#' + c.id, '#' + c.id, ''];
      var icon = it[2] ? '<img src="' + esc(gamePic(it[2], 'Res/')) + '">' : '';
      return '<div class="icard">' + icon + '<span>' + esc(pick(it)) + '</span><b class="' + (c.d > 0 ? 'up' : 'dn') + '">' + (c.d > 0 ? '+' : '') + c.d + '</b></div>';
    };
    return '<div class="idiff"><div><h5>' + esc(t('gained')) + '</h5><div class="icards">' + changes.filter(function (c) { return c.d > 0; }).map(card).join('') + '</div></div>' +
      '<div><h5>' + esc(t('lost')) + '</h5><div class="icards">' + changes.filter(function (c) { return c.d < 0; }).map(card).join('') + '</div></div></div>';
  }

  function compareModal(run, p) {
    var d = detailOf(run);
    var now = d ? d.now : null;
    var then = p.backup && d && d.items ? d.items[p.backup.id] : null;
    var body = '<div class="vs"><div class="side">' + shotBox(p.shot) + '<div class="who">' + esc(t('then')) + '<b>' + esc(whereAt(p)) + '</b><small>' + esc(kindName(p.kind)) + '</small></div></div>' +
      '<div class="side">' + shotBox(runPic(run)) + '<div class="who">' + esc(t('nowCol')) + '<b>' + esc(whereAt(run)) + '</b><small>' + esc(title(run)) + '</small></div></div></div>' +
      '<div class="cmpgrid"><div>' + statRows(p, p.stats, run, now, t('then'), t('nowCol')) + '</div>' +
      (then && d.nowItems ? '<div><div class="sec-t" style="margin-top:16px"><span>' + esc(t('itemsChanged')) + '</span></div>' + itemDiff(then, d.nowItems, d.names) + '</div>' : '') + '</div>';
    var foot = btn(t('closeBtn'), '', 'mclose');
    if (p.backup) foot += btn(t('restore') + ' ' + where(p), 'pri', 'restore');
    else if (p.snap) foot += btn(t('playAsNew'), 'pri', 'snapnew');
    return frame(t('compareTitle'), body, foot, true);
  }

  function cloudModal(run) {
    var b = backupById(run, run.behind.id);
    if (!b) return '';
    var d = detailOf(run);
    var body = '<p>' + fill(pick(TEXT.cloudBody), [esc(title(run))]) + '</p><div class="vs" style="margin-top:16px">' +
      '<div class="side pick" data-act="dismiss">' + shotBox(runPic(run), true) + '<div class="who">' + esc(t('onDisk')) + '<b>' + esc(whereAt(run)) + '</b><small>' + esc(t('written', run.lastPlayed || '-')) + '</small></div></div>' +
      '<div class="side pick" data-act="usebackup" style="border-color:rgba(79,168,255,0.5)">' + shotBox(shotOf(run.stem, b), true) + '<div class="who" style="color:#4fa8ff">' + esc(t('newestBackup')) + '<b>' + esc(whereAt(b)) + '</b><small>' + esc(t('made', b.at) + DOT + kindName(b.kind)) + '</small></div></div></div>' +
      statRows(run, d ? d.now : null, b, b.stats, t('onDisk'), t('newestBackup'));
    var foot = '<span class="mh">' + esc(t('cloudFoot')) + '</span><span class="sp"></span>' + btn(t('keepDisk', where(run)), '', 'dismiss') + btn(t('useBackup', where(b)), 'pri', 'usebackup');
    return frame(t('cloudTitle'), body, foot, true);
  }

  function exportModal(run) {
    var all = !!ui.modal.all;
    var extras = ui.modal.extras !== false;
    var count = (run.backups || []).length;
    var body = '<p>' + fill(pick(TEXT.exportBody), [esc(title(run))]) + '</p><div style="margin-top:10px">' +
      '<div class="mrow"><span class="lb">' + esc(t('exportSave')) + '</span><div class="tog on lock"><div class="track"></div><div class="knob"></div></div></div>' +
      (count ? '<div class="mrow"><span class="lb">' + esc(t('exportAll', count)) + '<small>' + esc(t('exportAllSize', size((run.saveBytes || 0) + backupBytes(run)), size(run.saveBytes))) + '</small></span><div class="tog' + (all ? ' on' : '') + '" data-act="exall"><div class="track"></div><div class="knob"></div></div></div>' : '') +
      '<div class="mrow"><span class="lb">' + esc(t('exportExtras')) + '</span><div class="tog' + (extras ? ' on' : '') + '" data-act="exextras"><div class="track"></div><div class="knob"></div></div></div></div>' +
      '<div class="pathline"><span>' + esc(t('savedTo')) + ': ' + esc((view.folder || '') + '\\Exports') + '</span>' + btn(t('openFolder'), 'sm', 'openexports') + '</div>';
    return frame(t('exportTitle'), body, btn(t('cancel'), '', 'mclose') + btn(t('exportBtn'), 'pri', 'doexport'));
  }

  function importRow(f) {
    var facts = [];
    if (f.loose) facts.push(t('importLoose'));
    else if (!f.readable) facts.push(t('importBad'));
    else facts.push(survivorName(f), where(f), t('importPoints', f.points), t('game', f.version || '-'));
    facts.push(size(f.bytes), f.at);
    var icon = f.survivor ? face(f) : '<div class="face">' + (f.readable ? '?' : '&times;') + '</div>';
    return '<div class="ifile' + (ui.modal.file === f.file ? ' on' : '') + (f.readable ? '' : ' bad') + '"' + (f.readable ? ' data-act="ipick" data-file="' + esc(f.file) + '"' : '') + '>' + icon +
      '<div style="min-width:0"><div class="fn">' + esc(f.name ? f.name : f.file) + '</div><small>' + esc((f.name ? f.file + DOT : '') + facts.join(DOT)) + '</small></div><span></span></div>';
  }

  function importModal() {
    var folder = imports ? imports.folder : (view.folder || '') + '\\Import';
    var files = imports ? imports.files || [] : null;
    var list = files == null ? '<div class="intro">' + esc(t('looking')) + '</div>' : files.length ? files.map(importRow).join('') : '<div class="intro">' + esc(t('importNone')) + '</div>';
    var body = '<p>' + pick(TEXT.importBody) + '</p><div class="pathline"><span>' + esc(folder) + '</span>' + btn(t('openFolder'), 'sm', 'openimport') + btn(t('lookAgain'), 'sm', 'importlist') + '</div>' + list;
    var foot = '<span class="mh">' + esc(t('importFoot')) + '</span><span class="sp"></span>' + btn(t('cancel'), '', 'mclose') + '<button class="btn pri" data-act="doimport"' + (ui.modal.file ? '' : ' disabled') + '>' + esc(t('importBtn')) + '</button>';
    return frame(t('importTitle'), body, foot, true);
  }

  function confirmModal() {
    return frame(ui.modal.title, '<p>' + ui.modal.body + '</p>', btn(t('cancel'), '', 'mclose') + btn(ui.modal.label, ui.modal.cls, 'confirm'));
  }

  function textModal() {
    var value = drafts.modal != null ? drafts.modal : ui.modal.value;
    return frame(ui.modal.title, (ui.modal.hint ? '<div class="hint" style="margin:0 0 10px">' + esc(ui.modal.hint) + '</div>' : '') +
      '<input class="txt" data-modal="1" data-focus="modal" type="text" maxlength="' + (ui.modal.max || 120) + '" spellcheck="false" value="' + esc(value) + '">',
      btn(t('cancel'), '', 'mclose') + btn(t('save'), 'pri', 'savetext'));
  }

  function modalHtml() {
    if (!ui.modal) return '';
    var run = selRun();
    var p = currentPoint();
    switch (ui.modal.kind) {
      case 'restore': return run && p && p.backup ? restoreModal(run, p) : '';
      case 'compare': return run && p ? compareModal(run, p) : '';
      case 'cloud': return run && run.behind ? cloudModal(run) : '';
      case 'export': return run ? exportModal(run) : '';
      case 'import': return importModal();
      case 'confirm': return confirmModal();
      case 'text': return textModal();
      default: return '';
    }
  }

  // The enlarged picture, drawn over the whole screen rather than in the window. The full-size file may be missing
  // (backups made before 0.4), so the small one stands in.
  function lightbox() {
    var small = ui.modal.small ? ' onerror="this.onerror=null;this.src=this.getAttribute(\'data-small\')" data-small="' + esc(ui.modal.small) + '"' : '';
    return '<div class="lightbox" data-act="mclose"><img src="' + esc(ui.modal.src) + '"' + small + '><span class="lb-x">&times;</span></div>';
  }

  // ---------- drawing ----------

  function ensureSelected() {
    if (ui.selected && findRun(ui.selected)) return;
    var shown = sorted(runs().filter(matches));
    shown = shown.filter(function (run) { return run.pinned; }).concat(shown.filter(function (run) { return !run.pinned; }));
    ui.selected = view.inRun && findRun(view.inRun) ? view.inRun : shown.length ? shown[0].stem : runs().length ? runs()[0].stem : null;
    ui.pointKey = null;
  }

  // The detail (stats now, item counts, the game's snapshots, ending photos) is asked for again when the selected run
  // or its newest backup changes.
  function askDetail() {
    var run = selRun();
    if (!run || ui.tab !== 'saves') return;
    var newest = (run.backups || [])[0];
    var key = run.stem + '|' + (newest ? newest.id : '') + '|' + (run.backups || []).length + '|' + run.turn + '|' + run.day + '|' + run.clock;
    if (key === detailKey) return;
    detailKey = key;
    post(['detail', run.stem]);
  }

  function ensureStyle() {
    etk.addStyle(document, ROOT_ID + '-css', CSS);
  }

  function ensureRoot() {
    var root = document.getElementById(ROOT_ID);
    if (root) return root;
    root = document.createElement('div');
    root.id = ROOT_ID;
    root.innerHTML = '<div class="stage"><div id="' + WIN_ID + '"></div></div><div class="lbox"></div>';
    root.addEventListener('click', onClick);
    root.addEventListener('input', onInput);
    root.addEventListener('change', onChange);
    root.addEventListener('keydown', onKey);
    root.addEventListener('focusin', function (e) { if (isTextBox(e.target)) typing(true); });
    root.addEventListener('focusout', function () {
      setTimeout(function () {
        if (isTextBox(document.activeElement)) return;
        typing(false);
        if (ui.isListening) { ui.isListening = false; draw(); }
      }, 0);
    });
    (document.body || document.documentElement).appendChild(root);
    window.addEventListener('resize', fitStage);
    etk.size(root.querySelector('#' + WIN_ID), { mod: 'SaveBackups' });
    return root;
  }

  // The window is laid out for a 1920x1080 screen and scaled to the game's; the player's UI size applies on top.
  function fitStage() {
    var stage = document.querySelector('#' + ROOT_ID + ' .stage');
    if (!stage) return;
    var scale = Math.min((window.innerWidth || STAGE_WIDTH) / STAGE_WIDTH, (window.innerHeight || STAGE_HEIGHT) / STAGE_HEIGHT);
    stage.style.transform = 'translate(-50%,-50%) scale(' + scale + ')';
  }

  function draw() {
    if (!view) return;
    ensureStyle();
    var root = ensureRoot();
    var win = root.querySelector('#' + WIN_ID);
    var places = SCROLLERS.map(function (sel) { var n = win.querySelector(sel); return n ? n.scrollTop : 0; });
    var active = document.activeElement;
    var focusKey = active && win.contains(active) && active.getAttribute ? active.getAttribute('data-focus') : null;
    var caret = null;
    try { if (focusKey) caret = [active.selectionStart, active.selectionEnd]; } catch (e) { /* not a text box */ }

    if (ui.tab === 'saves') ensureSelected();
    var body = ui.tab === 'trash' ? '<div class="bd solo">' + trashBody() + '</div>' : ui.tab === 'settings' ? settingsBody() : savesBody();
    win.className = isBusy ? 'busy' : '';
    win.innerHTML = '<div class="corner tl"></div><div class="corner tr"></div><div class="corner bl"></div><div class="corner br"></div>' + header() + body + footer() + modalHtml();
    root.querySelector('.lbox').innerHTML = ui.modal && ui.modal.kind === 'zoom' ? lightbox() : '';

    SCROLLERS.forEach(function (sel, i) { var n = win.querySelector(sel); if (n) n.scrollTop = places[i]; });
    restoreFocus(win, focusKey, caret);
    fitStage();
    askDetail();
  }

  function restoreFocus(win, focusKey, caret) {
    var target = ui.isListening ? win.querySelector('[data-key]') : focusKey ? win.querySelector('[data-focus="' + focusKey + '"]') : ui.modal && ui.modal.kind === 'text' ? win.querySelector('[data-modal]') : null;
    if (target) {
      target.focus();
      try {
        if (caret && caret[0] != null) target.setSelectionRange(caret[0], caret[1]);
        else if (target.value) target.setSelectionRange(target.value.length, target.value.length);
      } catch (e) { /* not a text box */ }
    }
    if (!isTextBox(document.activeElement)) typing(false);
  }

  // Only the list is drawn again while the search box is typed in, so the box keeps its focus and caret.
  function drawList() {
    var list = document.querySelector('#' + WIN_ID + ' .list');
    if (list) list.innerHTML = listHtml();
  }

  // ---------- keyboard and typing ----------

  // The game sends keys to the page only while the plugin asks it to, so a text box tells the plugin when it has
  // focus. A redraw can drop a focused box without a blur, so the check waits a tick and looks at what has focus.
  function isTextBox(node) {
    return !!node && (node.tagName === 'INPUT' || node.tagName === 'TEXTAREA' || (node.getAttribute && node.getAttribute('data-key'))) && !!node.closest && !!node.closest('#' + ROOT_ID);
  }

  function typing(isOn) {
    if (isOn === isTyping) return;
    isTyping = isOn;
    post(['typing', isOn ? '1' : '0']);
  }

  // Esc handled by the page; the plugin's own Esc for the same key press is ignored.
  function pageEsc(e) {
    e.preventDefault();
    e.stopPropagation();
    pageEscAt = Date.now();
  }

  // A key press as Unity's KeyCode name, or '' for a key the setting does not take.
  function keyCode(e) {
    var code = e.code || '';
    var m;
    if ((m = /^F(\d{1,2})$/.exec(code)) && +m[1] <= 15) return code;
    if ((m = /^Key([A-Z])$/.exec(code))) return m[1];
    if ((m = /^Digit(\d)$/.exec(code))) return 'Alpha' + m[1];
    if ((m = /^Numpad(\d)$/.exec(code))) return 'Keypad' + m[1];
    var named = { Insert: 'Insert', Home: 'Home', End: 'End', PageUp: 'PageUp', PageDown: 'PageDown', Pause: 'Pause', ScrollLock: 'ScrollLock', Backquote: 'BackQuote' };
    return named[code] || '';
  }

  function onKey(e) {
    var node = e.target;
    if (ui.isListening && node.getAttribute('data-key')) {
      listenKey(e, node);
      return;
    }
    if (e.key === 'Escape' && isTextBox(node)) {
      pageEsc(e);
      if (node.getAttribute('data-modal')) { closeModal(); return; }
      if (node.getAttribute('data-in') === 'query' && ui.query) { ui.query = node.value = ''; delete drafts.query; drawList(); return; }
      node.blur();
      return;
    }
    if (e.key === 'Enter' && node.tagName === 'INPUT') {
      e.preventDefault();
      if (node.getAttribute('data-modal')) { ACT.savetext(); return; }
      if (node.getAttribute('data-path')) { commitPath(node.getAttribute('data-path')); return; }
      node.blur();
    }
  }

  function listenKey(e, node) {
    e.preventDefault();
    if (e.key === 'Escape') {
      pageEsc(e);
      ui.isListening = false;
      node.blur();
      draw();
      return;
    }
    var name = e.key === 'Backspace' || e.key === 'Delete' ? 'None' : keyCode(e);
    if (!name) return;
    ui.isListening = false;
    node.blur();
    step(['set', 'Controls.BackupKey', encodeURIComponent(name)]);
    draw();
  }

  function onInput(e) {
    var node = e.target;
    if (node.getAttribute('data-in') === 'query') { ui.query = node.value; drawList(); return; }
    if (node.getAttribute('data-note')) { drafts['note:' + node.getAttribute('data-note')] = node.value; return; }
    if (node.getAttribute('data-set')) { drafts['set:' + node.getAttribute('data-set')] = node.value; return; }
    if (node.getAttribute('data-path')) { drafts['set:' + node.getAttribute('data-path')] = node.value; return; }
    if (node.getAttribute('data-modal')) drafts.modal = node.value;
  }

  // A number or a note is sent once the box loses focus or Enter is pressed; nothing is sent when it did not change.
  function onChange(e) {
    var node = e.target;
    var stem = node.getAttribute('data-note');
    if (stem) {
      var run = findRun(stem);
      delete drafts['note:' + stem];
      if (run && node.value.trim() !== (run.note || '')) {
        run.note = node.value.trim();
        post(['runnote', stem, encodeURIComponent(run.note)]);
      }
      return;
    }
    var id = node.getAttribute('data-set');
    if (!id) return;
    delete drafts['set:' + id];
    if (node.value.trim() !== setting(id)) step(['set', id, encodeURIComponent(node.value.trim())]);
  }

  function commitPath(id) {
    var node = document.querySelector('#' + WIN_ID + ' [data-path="' + id + '"]');
    if (!node) return;
    delete drafts['set:' + id];
    var value = node.value.trim();
    node.blur();
    if (value !== setting(id)) step(['set', id, encodeURIComponent(value)]);
  }

  // ---------- clicks ----------

  function closeModal() {
    ui.modal = null;
    delete drafts.modal;
    draw();
  }

  function openModal(next) {
    ui.modal = next;
    delete drafts.modal;
    draw();
  }

  function confirmStep(heading, bodyHtml, labelText, parts) {
    openModal({ kind: 'confirm', title: heading, body: bodyHtml, label: labelText, cls: 'danger', parts: parts });
  }

  function sendAndClose(parts) {
    ui.modal = null;
    delete drafts.modal;
    step(parts);
    draw();
  }

  function deleteSaveBody(run) {
    var n = (run.backups || []).length;
    return view.binDays > 0 ? fill(pick(TEXT.deleteBody), [esc(title(run)), n, view.binDays]) : fill(pick(TEXT.deleteBodyKeep), [esc(title(run)), n]);
  }

  var ACT = {
    close: function () { post(['close']); },
    backdrop: function (node, e) { if (e.target === node) closeModal(); },
    mclose: function () { closeModal(); },
    sort: function () { ui.sortBy = SORTS[(SORTS.indexOf(ui.sortBy) + 1) % SORTS.length]; draw(); },
    newgame: function () { step(['newgame']); },
    gamelist: function () { step(['gamelist']); },
    folder: function () { post(['folder']); },
    runfolder: function () { if (ui.selected) post(['folder', ui.selected]); },
    openexports: function () { post(['folder', 'Exports']); },
    openimport: function () { post(['folder', 'Import']); },
    load: function () { var run = selRun(); if (run && !view.inRun) step(['load', run.file]); },
    backup: function () { if (ui.selected) step(['backup', ui.selected]); },
    duplicate: function () { if (ui.selected) step(['duplicate', ui.selected]); },
    runpin: function () { var run = selRun(); if (run) step(['runpin', run.stem, run.pinned ? '0' : '1']); },
    rename: function () {
      var run = selRun();
      if (run) openModal({ kind: 'text', title: t('renameTitle'), value: title(run), max: 60, parts: ['rename', run.stem], isRename: true });
    },
    delrun: function () {
      var run = selRun();
      if (run) confirmStep(t('deleteTitle'), deleteSaveBody(run), t('del'), ['delete', run.stem]);
    },
    export: function () { openModal({ kind: 'export', all: false, extras: true }); },
    exall: function () { ui.modal.all = !ui.modal.all; draw(); },
    exextras: function () { ui.modal.extras = ui.modal.extras === false; draw(); },
    doexport: function () { sendAndClose(['exportrun', ui.selected, ui.modal.all ? '1' : '0', ui.modal.extras !== false ? '1' : '0']); },
    import: function () { imports = null; openModal({ kind: 'import', file: null }); post(['importlist']); },
    importlist: function () { imports = null; ui.modal.file = null; draw(); post(['importlist']); },
    ipick: function (node) { ui.modal.file = node.getAttribute('data-file'); draw(); },
    doimport: function () { if (ui.modal.file) sendAndClose(['importfile', ui.modal.file]); },
    cloud: function () { openModal({ kind: 'cloud' }); },
    dismiss: function () { if (ui.selected) sendAndClose(['dismiss', ui.selected]); },
    usebackup: function () { var run = selRun(); if (run && run.behind) sendAndClose(['restore', run.stem, run.behind.id]); },
    restore: function () { var p = currentPoint(); if (p && p.backup) openModal({ kind: 'restore', asNew: true }); },
    asnew: function (node) {
      if (node.classList.contains('off')) return;
      ui.modal.asNew = node.getAttribute('data-v') === '1';
      draw();
    },
    dorestore: function () {
      var run = selRun();
      var p = currentPoint();
      if (!run || !p || !p.backup) return;
      var asNew = isPlaying(run) || ui.modal.asNew !== false;
      sendAndClose([asNew ? 'copy' : 'restore', run.stem, p.backup.id]);
    },
    compare: function () { openModal({ kind: 'compare' }); },
    keep: function () { var p = currentPoint(); if (p && p.backup) step(['pin', ui.selected, p.backup.id, p.backup.pinned ? '0' : '1']); },
    ptnote: function () {
      var p = currentPoint();
      if (p && p.backup) openModal({ kind: 'text', title: t('noteTitle'), hint: t('noteHint'), value: p.backup.note || '', max: 200, parts: ['note', ui.selected, p.backup.id] });
    },
    ptexport: function () { var p = currentPoint(); if (p && p.backup) step(['export', ui.selected, p.backup.id]); },
    delpt: function () {
      var p = currentPoint();
      if (!p || !p.backup) return;
      var body = view.binDays > 0 ? t('removeBody', whereAt(p), kindName(p.kind), view.binDays) : t('removeBodyKeep', whereAt(p), kindName(p.kind));
      confirmStep(t('removeTitle'), esc(body), t('del'), ['remove', ui.selected, p.backup.id]);
    },
    snapnew: function () { var p = currentPoint(); if (p && p.snap) sendAndClose(['snapnew', ui.selected, p.snap.file]); },
    savetext: function () {
      if (!ui.modal || ui.modal.kind !== 'text') return;
      var node = document.querySelector('#' + WIN_ID + ' [data-modal]');
      var value = (node ? node.value : drafts.modal || '').trim();
      if (ui.modal.isRename && !value) return;
      sendAndClose(ui.modal.parts.concat([encodeURIComponent(value)]));
    },
    confirm: function () { if (ui.modal && ui.modal.parts) sendAndClose(ui.modal.parts); },
    tg: function (node) { var id = node.getAttribute('data-id'); step(['set', id, isOn(id) ? 'false' : 'true']); },
    setpath: function (node) { commitPath(node.getAttribute('data-id')); },
    listen: function () { ui.isListening = true; draw(); },
    bringback: function (node) { step(['bringback', node.getAttribute('data-stem'), node.getAttribute('data-id')]); },
    binrundel: function (node) {
      var stem = node.getAttribute('data-stem');
      var run = (view.deleted || []).filter(function (r) { return r.stem === stem; })[0];
      confirmStep(t('deleteTitle'), esc(t('deleteForGood', run ? title(run) : stem)), t('deleteNow'), ['binrundel', stem]);
    },
    binback: function (node) { step(['binback', node.getAttribute('data-stem'), node.getAttribute('data-id')]); },
    bindel: function (node) { step(['bindel', node.getAttribute('data-stem'), node.getAttribute('data-id')]); },
    emptybin: function () { confirmStep(t('emptyTitle'), esc(t('emptyBody', size(trashBytes()))), t('emptyBin', size(trashBytes())), ['binempty']); },
    zoom: function (node) { openModal({ kind: 'zoom', src: node.getAttribute('data-src'), small: node.getAttribute('data-small') || '' }); }
  };

  function onClick(e) {
    if (isBusy) return;
    var node = e.target.closest('[data-act],[data-run],[data-pt],[data-tab],[data-filter],[data-sub],[data-bkf],[data-sec]');
    if (!node) return;
    if (node.hasAttribute('data-act')) {
      var act = ACT[node.getAttribute('data-act')];
      if (act) act(node, e);
      return;
    }
    if (node.hasAttribute('data-tab')) { ui.tab = node.getAttribute('data-tab'); ui.modal = null; ui.isListening = false; draw(); return; }
    if (node.hasAttribute('data-run')) { ui.selected = node.getAttribute('data-run'); ui.pointKey = null; ui.sub = 'backups'; ui.bkFilter = 'all'; draw(); return; }
    if (node.hasAttribute('data-pt')) { ui.pointKey = node.getAttribute('data-pt'); draw(); return; }
    if (node.hasAttribute('data-filter')) { ui.filter = node.getAttribute('data-filter'); draw(); return; }
    if (node.hasAttribute('data-sub')) { ui.sub = node.getAttribute('data-sub'); ui.pointKey = null; draw(); return; }
    if (node.hasAttribute('data-bkf')) { ui.bkFilter = node.getAttribute('data-bkf'); ui.pointKey = null; draw(); return; }
    if (node.hasAttribute('data-sec')) { ui.setSec = Number(node.getAttribute('data-sec')); draw(); }
  }

  // ---------- the plugin's side ----------

  function parse(json) {
    try { return JSON.parse(json); } catch (e) { return null; }
  }

  function remove() {
    var root = document.getElementById(ROOT_ID);
    if (root) root.parentNode.removeChild(root);
    window.removeEventListener('resize', fitStage);
  }

  window.__slsavebackups = {
    open: function (json) {
      var next = parse(json);
      if (!next) return;
      view = next;
      ui = freshUi();
      ui.selected = view.inRun || null;
      detail = null;
      detailKey = '';
      imports = null;
      drafts = {};
      isBusy = false;
      isTyping = false;
      draw();
    },
    show: function (json) {
      var next = parse(json);
      if (!next || !document.getElementById(ROOT_ID)) return;
      view = next;
      isBusy = false;
      draw();
    },
    detail: function (json) {
      var next = parse(json);
      if (!next || !view) return;
      detail = next;
      if (next.stem === ui.selected && ui.tab === 'saves') draw();
    },
    imports: function (json) {
      var next = parse(json);
      if (!next || !view) return;
      imports = next;
      if (ui.modal && ui.modal.kind === 'import') draw();
    },
    close: function () {
      view = null;
      ui = freshUi();
      isBusy = false;
      isTyping = false;
      remove();
    },
    escape: function () {
      // The same Esc press the page already used in a text box.
      if (isBusy || Date.now() - pageEscAt < 250) return;
      if (isTextBox(document.activeElement)) {
        document.activeElement.blur();
        return;
      }
      if (ui.modal) {
        closeModal();
        return;
      }
      post(['close']);
    },
    done: function () {
      isBusy = false;
      var win = document.getElementById(WIN_ID);
      if (win) win.classList.remove('busy');
    }
  };

  // Runs once the Toolkit's page API is there: at once when its script ran first, else when it runs.
  (window.ExiledToolkitReady = window.ExiledToolkitReady || []).push(function (api) {
    etk = api;
    post(['ready']);
  });
})();
