// Save Backups' web half. Runs in Root.html, the document every game page iframe sits in. Draws the save manager over
// the title screen (or over a run, from the pause menu) when the plugin opens it, and puts "Backups of this run" and
// "Back up now" in the game's pause menu. Talks to the plugin through vuplex.postMessage("slmod|savebackups|<verb>")
// and is driven by window.__slsavebackups. Every step that changes a save waits for the plugin: the page takes no
// clicks until the plugin sends the new view (show) or says the step is over (done).
(function () {
  if (window.__slsavebackups) return;

  var PREFIX = 'slmod|savebackups|';
  var ROOT_ID = 'slsb-root';
  var POLL_MS = 400;
  var DOT = ' \u00b7 ';

  var TEXT = {
    title: ['Saves', '存档'],
    titleRun: ['Backups of this run', '本局备份'],
    tabSaves: ['Saves', '存档'],
    tabSettings: ['Backup settings', '备份设置'],
    tabBin: ['Recycle bin', '回收站'],
    subRuns: ['{0} saves', '{0} 个存档'],
    subPoints: ['{0} restore points', '{0} 个还原点'],
    inRunNote: ['In a run: loading a save and new games wait for the title screen', '游戏进行中：读取存档和新游戏需回到标题画面'],
    search: ['Search saves, survivors, endings, notes', '搜索存档、角色、结局、备注'],
    importBtn: ['Import', '导入'],
    sortBy: ['Sort: {0}', '排序：{0}'],
    sorts: { played: ['Last played', '最近游玩'], days: ['Most days', '天数最多'], survivor: ['Survivor', '角色'], name: ['Name', '名称'] },
    filters: { all: ['All', '全部'], live: ['Playing', '进行中'], end: ['Finished', '已结束'], endless: ['Endless', '无尽'], warn: ['Needs a look', '需要查看'] },
    noRuns: ['No saves yet.', '还没有存档。'],
    noMatch: ['No saves match.', '没有符合条件的存档。'],
    newGame: ['+ New game', '+ 新游戏'],
    gameList: ["Game's own list", '游戏原版列表'],
    openFolder: ['Open folder', '打开文件夹'],
    diskOf: ['Backups use {0} of the {1} limit', '备份占用 {0}，上限 {1}'],
    disk: ['Backups use {0}', '备份占用 {0}'],
    diskSaves: ['saves {0}', '存档 {0}'],
    badge: { live: ['Alive', '存活'], end: ['Ending', '结局'], endless: ['Endless', '无尽'], warn: ['Check', '待查看'], old: ['Older', '旧版本'], playing: ['Playing now', '正在游玩'] },
    day: ['Day {0}', '第 {0} 天'],
    dayAt: ['Day {0}, {1}', '第 {0} 天 {1}'],
    loop: ['loop {0}', '第 {0} 轮'],
    loopDay: ['Loop {0}, day {1}', '第 {0} 轮第 {1} 天'],
    endless: ['Endless', '无尽'],
    survival: ['Survival', '生存'],
    lastPicture: ['Last picture: {0}', '最近的截图：{0}'],
    noPicture: ['No picture yet. One is taken with each backup made in the run.', '还没有截图。游戏中每次备份都会截一张图。'],
    rename: ['rename', '改名'],
    pin: ['pin', '置顶'],
    unpin: ['unpin', '取消置顶'],
    kvSurvivor: ['Survivor', '角色'],
    kvDiff: ['Difficulty', '难度'],
    kvMode: ['Mode', '模式'],
    kvReached: ['Reached', '进度'],
    kvPlayed: ['Played', '游玩时长'],
    kvLast: ['Last played', '上次游玩'],
    kvVersion: ['Game version', '游戏版本'],
    kvSize: ['Size with backups', '含备份大小'],
    kvStatus: ['Ending', '结局'],
    notePlaceholder: ['Your note for this save', '这个存档的备注'],
    cont: ['Continue', '继续'],
    lookEnding: ['Look at the ending', '查看结局'],
    backUp: ['Back up now', '立即备份'],
    duplicate: ['Duplicate', '复制'],
    exportBtn: ['Export', '导出'],
    del: ['Delete', '删除'],
    playingHint: ['You are playing this save. Loading, replacing or deleting it waits for the title screen; backing up and copying work now.', '你正在游玩这个存档。读取、替换或删除需回到标题画面；备份和复制现在就可以。'],
    otherInRunHint: ['Loading another save waits for the title screen.', '读取其他存档需回到标题画面。'],
    behindTitle: ['This save went back in time.', '这个存档回到了过去。'],
    behindBody: ['It is at {0} now, but your backup from {1} is at {2}. That is what a Steam Cloud download of an older copy, or a crash, looks like. The newer copy is safe in your backups.', '它现在在{0}，但你 {1} 的备份在{2}。Steam 云下载了旧存档或游戏崩溃时就会这样。较新的那份在备份里，很安全。'],
    lookBoth: ['Look at both', '对比两者'],
    dismiss: ['Dismiss', '忽略'],
    points: ['Restore points', '还原点'],
    showSnaps: ["Show the game's own snapshots", '显示游戏自带的快照'],
    noPoints: ['No backups of this save yet. One is made each morning, when you quit, before a rebirth, an expedition or a zombie wave, and at an ending{0}.', '这个存档还没有备份。每天早上、退出时、重生前、探索前、尸潮前和到达结局时都会自动备份{0}。'],
    keyHint: [', or press {0} in the run', '，也可以在游戏中按 {0}'],
    pauseHint: [', or use Back up now in the pause menu', '，也可以在暂停菜单点「立即备份」'],
    now: ['NOW', '当前'],
    newest: ['NEWEST', '最新'],
    nowRow: ['The save as it is now', '存档当前状态'],
    madeAuto: ['automatic', '自动'],
    madeYou: ['made by you', '手动'],
    byGame: ['kept by the game, not the mod', '由游戏保存，非本模组'],
    nowWhere: ['This is where the save is now.', '这就是存档现在的状态。'],
    loading: ['Reading the save...', '正在读取存档...'],
    st: { health: ['Health', '健康'], fullness: ['Fullness', '饱腹'], mood: ['Mood', '心情'], stamina: ['Stamina', '体力'], money: ['Money', '金钱'], points: ['Planning points', '规划点'], items: ['Items', '物品'], day: ['Day', '天数'] },
    loadPoint: ['Load this point', '读取这个还原点'],
    loadAsNew: ['Load as a new save', '读取为新存档'],
    compare: ['Compare with now', '与当前对比'],
    keep: ['Keep forever', '永久保留'],
    unkeep: ['Stop keeping forever', '取消永久保留'],
    noteBtn: ['Note', '备注'],
    kept: ['Kept forever', '永久保留'],
    oldWarn: ['Made on game version {0}, and you are playing {1}. It may not load as it did.', '这是游戏版本 {0} 时的备份，当前版本为 {1}，读取时可能和原来不一样。'],
    cancel: ['Cancel', '取消'],
    close: ['Close', '关闭'],
    save: ['Save', '保存'],
    restoreTitle: ['Load a restore point', '读取还原点'],
    restoreBody: ['Load <b>{0}</b> ({1}) of <b>{2}</b>?', '读取「{2}」的<b>{0}</b>（{1}）？'],
    asNew: ['As a new save', '作为新存档'],
    asNewBody: ['A copy from {0} is added next to it. {1} stays exactly as it is.', '会在旁边新增一个{0}的副本。{1}保持原样。'],
    replace: ['Replace this save', '替换这个存档'],
    replaceBody: ['The save goes back to {0}. {1} is backed up first, so you can come back to it from this list.', '存档回到{0}。{1}会先备份，可以从列表回到现在。'],
    replaceBlocked: ['Not while you are playing it. Quit to the title screen first.', '正在游玩时不能替换，请先回到标题画面。'],
    loadDay: ['Load {0}', '读取{0}'],
    compareTitle: ['Compare', '对比'],
    then: ['THEN', '当时'],
    nowCol: ['NOW', '现在'],
    change: ['CHANGE', '变化'],
    what: ['WHAT', '项目'],
    same: ['same', '不变'],
    itemsChanged: ['What changed in your stores', '物品变化'],
    noItemChange: ['No change in items.', '物品没有变化。'],
    noStats: ['This point has no stats. Backups made by this version of the mod keep them.', '这个还原点没有数据。本版本模组的备份会记录数据。'],
    nowLabel: ['Now: {0}', '现在：{0}'],
    cloudTitle: ['A save went back in time', '存档回到了过去'],
    cloudBody: ['<b>{0}</b> is older than your newest backup of it. It may have been replaced by an older copy from Steam Cloud or another computer.', '「{0}」比它的最新备份还旧，可能被 Steam 云或其他电脑上的旧存档替换了。'],
    onDisk: ['On disk now: {0}', '当前存档：{0}'],
    onDiskAt: ['written {0}', '写入于 {0}'],
    newestBackup: ['Your newest backup: {0}', '最新备份：{0}'],
    newestAt: ['made {0}', '备份于 {0}'],
    cloudFoot: ['Whichever you pick, the other one is kept in the backups.', '无论选哪个，另一个都会保留在备份中。'],
    keepDisk: ['Keep {0}', '保留{0}'],
    useBackup: ['Use {0}', '使用{0}'],
    exportTitle: ['Export', '导出'],
    exportBody: ['Export <b>{0}</b> as one .slsave file you can move to another computer, keep somewhere safe, or send with a bug report.', '把「{0}」导出为一个 .slsave 文件，可以拿到别的电脑、妥善保存，或随问题报告一起发送。'],
    exportSave: ['The save as it is now', '存档当前状态'],
    exportAll: ['All {0} restore points', '全部 {0} 个还原点'],
    exportAllSize: ['{0} instead of {1}', '{0}，只导出当前为 {1}'],
    exportExtras: ['Pictures and your notes', '截图和备注'],
    savedTo: ['Saved to', '保存到'],
    importTitle: ['Import a save', '导入存档'],
    importBody: ['Put a <b>.slsave</b> file, or a loose <b>Save_*.bytes</b> file from another computer, in this folder, then pick it here.', '把 <b>.slsave</b> 文件或其他电脑上的 <b>Save_*.bytes</b> 存档放进这个文件夹，再在这里选择。'],
    importNone: ['Nothing in the folder yet.', '文件夹里还没有文件。'],
    importLoading: ['Looking in the folder...', '正在查看文件夹...'],
    importLoose: ['A loose save file', '单独的存档文件'],
    importBad: ['Could not be read', '无法读取'],
    importPoints: ['{0} restore points', '{0} 个还原点'],
    importNewer: ['from game version {0}', '游戏版本 {0}'],
    importFoot: ['It is added as a new save. Nothing you have is replaced.', '会作为新存档加入，不会替换已有的存档。'],
    refresh: ['Look again', '刷新'],
    deleteTitle: ['Delete save', '删除存档'],
    deleteBody: ['Delete <b>{0}</b> and its {1} restore points? They go to the recycle bin for {2} days first.', '删除「{0}」及其 {1} 个还原点？它们会先在回收站保留 {2} 天。'],
    deleteBodyKeep: ['Delete <b>{0}</b> and its {1} restore points? They go to the recycle bin until you empty it.', '删除「{0}」及其 {1} 个还原点？它们会留在回收站，直到你清空。'],
    renameTitle: ['Name this save', '给存档改名'],
    noteTitle: ['Note on this restore point', '这个还原点的备注'],
    noteHint: ['A few words to tell it apart. Leave it empty to remove the note.', '写几个字方便区分。留空即删除备注。'],
    removeTitle: ['Delete this restore point?', '删除这个还原点？'],
    removeBody: ['{0} ({1}) goes to the recycle bin.', '{0}（{1}）会移到回收站。'],
    binIntro: ['Deleting a save, here or in the game, or a backup being thinned out, moves it here first. After {0} days it is gone for good.', '在这里或游戏中删除存档，或备份被精简时，都会先移到这里。{0} 天后永久删除。'],
    binIntroKeep: ['Deleting a save, here or in the game, or a backup being thinned out, moves it here first. It stays until you empty the bin.', '在这里或游戏中删除存档，或备份被精简时，都会先移到这里，直到你清空回收站。'],
    binSaves: ['Deleted saves', '已删除的存档'],
    binPoints: ['Restore points', '还原点'],
    binEmptyList: ['The bin is empty.', '回收站是空的。'],
    putBack: ['Put back', '还原'],
    deleteNow: ['Delete now', '立即删除'],
    emptyBin: ['Empty the bin ({0})', '清空回收站（{0}）'],
    emptyTitle: ['Empty the recycle bin?', '清空回收站？'],
    emptyBody: ['Everything in the bin is deleted for good: {0}.', '回收站里的所有内容将被永久删除：{0}。'],
    daysLeft: ['{0} days left', '还剩 {0} 天'],
    dayLeft: ['1 day left', '还剩 1 天'],
    lastDay: ['goes today', '今天删除'],
    thinned: ['thinned out', '已精简'],
    deletedWhy: ['deleted', '已删除'],
    goneRun: ['Its newest backup goes back into your saves.', '最新的备份会回到存档列表。'],
    setWhen: ['When to back up', '何时备份'],
    setWhenEx: ['Each kind keeps its own number, so many expeditions never push out your mornings. 0 keeps them all.', '每种备份单独计数，探索再多也不会挤掉早上的备份。0 表示全部保留。'],
    setKeep: ['How long to keep them', '保留多久'],
    setKeepEx: ['Older backups thin out on their own. Points you keep forever are never removed.', '较旧的备份会自动精简。永久保留的还原点不会被删除。'],
    setWhere: ['Where and how', '位置与方式'],
    keepN: ['keep', '保留'],
    trig: {
      morning: ['Every morning', '每天早上', 'When a new day starts.', '新的一天开始时。'],
      expedition: ['Before each expedition', '每次探索前', 'When you leave home for a map.', '离家前往地图时。'],
      wave: ['Before a zombie wave', '尸潮前', 'The last save before it hits.', '尸潮来临前的最后一次存档。'],
      rebirth: ['Before a rebirth', '重生前', 'Never overwritten by the next loop.', '不会被下一轮覆盖。'],
      ending: ['At an ending', '到达结局时', 'So another ending can be tried.', '方便尝试其他结局。'],
      quit: ['When you quit to the title screen', '退出到标题画面时', '', ''],
      timer: ['Every few minutes of play', '每隔几分钟', 'Real minutes, only while playing.', '现实时间，仅在游玩时计时。'],
      manual: ['Your own backups', '手动备份', 'Back up now, here or in the pause menu.', '这里或暂停菜单中的「立即备份」。'],
      safety: ['Before a restore or delete', '恢复或删除前', 'So the step can be undone.', '方便撤销。']
    },
    minutes: ['minutes', '分钟'],
    keepAll: ['Everything from the last', '保留最近', 'days of play, not real days', '游戏天数，不是现实天数'],
    daily: ['Then one per in-game day for', '之后每个游戏日保留一个，持续', '', ''],
    perLoop: ['Then one per loop, forever', '之后每轮永久保留一个', '', ''],
    maxMb: ['Space limit for all backups', '所有备份的空间上限', 'oldest automatic ones go first; 0 for no limit', '先删除最旧的自动备份；0 表示不限'],
    binDays: ['Recycle bin keeps deleted things for', '回收站保留时间', '0 keeps them until you empty it', '0 表示保留到手动清空'],
    days: ['days', '天'],
    mb: ['MB', 'MB'],
    folder: ['Backup folder', '备份文件夹', "Outside the game's save folder, so Steam Cloud never touches it. Empty for the default.", '在游戏存档文件夹之外，Steam 云不会碰它。留空为默认位置。'],
    second: ['Second copy in another folder', '另存一份到其他文件夹', 'A USB stick, OneDrive, Dropbox. Empty for none.', 'U 盘、OneDrive、Dropbox 等。留空则不另存。'],
    setPath: ['Set', '设置'],
    keyRow: ['Back up now key', '立即备份按键', 'Click, then press a key. Backspace clears it.', '点击后按下按键。退格键清除。'],
    keyNone: ['Not set', '未设置'],
    keyPress: ['Press a key', '请按键'],
    pics: ['Take a picture with each backup', '每次备份时截图', 'Only backups made in a run.', '仅限游戏中的备份。'],
    verify: ['Check every backup can be read after writing it', '写入后检查备份能否读取', '', ''],
    warnBehind: ['Warn me when a save goes back in time', '存档回到过去时提醒我', 'Steam Cloud downloads, a copy from another computer', 'Steam 云下载、其他电脑的旧存档'],
    snapsRow: ["Show the game's own day snapshots on the timeline", '在时间线上显示游戏自带的每日快照', '', ''],
    replaceScreen: ["Open this screen instead of the game's save list", '用这个界面代替游戏的存档列表', '', ''],
    toasts: ['A message for each automatic backup', '自动备份时显示提示', '', ''],
    pauseOpen: ['Backups of this run', '本局备份'],
    pauseNow: ['Back up now', '立即备份'],
    ago: { now: ['just now', '刚刚'], min: ['{0} min ago', '{0} 分钟前'], hour: ['{0} h ago', '{0} 小时前'], day: ['yesterday', '昨天'], days: ['{0} days ago', '{0} 天前'] },
    playTime: ['{0} h {1} m', '{0} 小时 {1} 分']
  };

  // The kinds of restore point, with the dot colour each has on the timeline and in the legend.
  var KINDS = {
    now: { c: '#8fc7bf', n: ['Now', '当前'] },
    morning: { c: '#FFC83A', n: ['Morning', '早上'] },
    expedition: { c: '#8fc7bf', n: ['Before expedition', '探索前'] },
    wave: { c: '#e06a5a', n: ['Before zombie wave', '尸潮前'] },
    rebirth: { c: '#c9a4ff', n: ['Before rebirth', '重生前'] },
    ending: { c: '#EBA308', n: ['Ending', '结局'] },
    manual: { c: '#ffffff', n: ['Made by you', '手动'] },
    quit: { c: '#9a9a9a', n: ['Quit', '退出'] },
    timer: { c: '#d8b070', n: ['Timer', '定时'] },
    safety: { c: '#6f6a60', n: ['Before a restore or delete', '恢复或删除前'] },
    imported: { c: '#7fb0e0', n: ['Imported', '导入'] },
    snap: { c: '#6f8fb8', n: ["Game's day snapshot", '游戏每日快照'] },
    round: { c: '#6f8fb8', n: ['Start of the loop', '本轮开始'] }
  };

  var LEGEND = ['morning', 'expedition', 'wave', 'rebirth', 'ending', 'manual', 'quit', 'timer', 'safety', 'imported', 'snap', 'round'];
  var SORTS = ['played', 'days', 'survivor', 'name'];
  var FILTERS = ['all', 'live', 'end', 'endless', 'warn'];

  var CSS = [
    '#' + ROOT_ID + '{position:fixed;inset:0;z-index:2147483000;background:rgba(0,0,0,.6);display:flex;align-items:center;justify-content:center;color:#ddd}',
    '#' + ROOT_ID + ' *{box-sizing:border-box}',
    '.slsb{--amber:#EBA308;--gold:#FFC83A;--teal:#8fc7bf;--red:#e06a5a;--dim:#8a7f6c;--line:rgba(220,188,120,.22);--panel:rgba(14,12,10,.86);position:relative;width:1600px;height:900px;overflow:hidden;background:#0c0b0a;font-family:"SLSB Sans","Microsoft YaHei","Segoe UI",sans-serif;box-shadow:0 0 0 1px #222,0 24px 70px rgba(0,0,0,.85)}',
    '.slsb button,.slsb input,.slsb textarea{font-family:inherit}',
    '.slsb.busy{cursor:progress}.slsb.busy button,.slsb.busy [data-act],.slsb.busy [data-run],.slsb.busy [data-pt]{pointer-events:none}.slsb.busy .btn{opacity:.6}',
    '.slsb ::-webkit-scrollbar{width:8px}.slsb ::-webkit-scrollbar-track{background:transparent}.slsb ::-webkit-scrollbar-button{display:none}',
    '.slsb ::-webkit-scrollbar-thumb{background:rgba(235,163,8,.28);border-radius:4px}.slsb ::-webkit-scrollbar-thumb:hover{background:rgba(235,163,8,.5)}',
    '.slsb .bg{position:absolute;inset:0;background:#0c0b0a center/cover;filter:blur(3px) brightness(.35) saturate(.7);transform:scale(1.04)}',
    '.slsb .vign{position:absolute;inset:0;background:radial-gradient(ellipse at 50% 40%,transparent 30%,rgba(0,0,0,.75))}',
    '.slsb .scr{position:absolute;inset:0;display:flex;flex-direction:column;padding:26px 34px 22px}',
    '.slsb .hd{display:flex;align-items:flex-end;gap:22px;padding-bottom:14px;border-bottom:1px solid var(--line)}',
    '.slsb .hd h1{margin:0;font-weight:800;font-size:34px;line-height:1;color:#fff;letter-spacing:4px;text-transform:uppercase}',
    '.slsb .hd .sub{color:var(--dim);font-size:14px;padding-bottom:2px}',
    '.slsb .hd .sub .ir{color:var(--teal)}',
    '.slsb .sp{flex:1}',
    '.slsb .tabs{display:flex;gap:4px}',
    '.slsb .tab{background:none;border:0;border-bottom:2px solid transparent;color:#aaa;font-size:17px;padding:6px 14px;cursor:pointer;letter-spacing:1px}',
    '.slsb .tab:hover{color:#fff}.slsb .tab.on{color:var(--gold);border-color:var(--gold)}',
    '.slsb .tab .n{color:var(--dim);font-size:13px;margin-left:5px}',
    '.slsb .x{width:38px;height:38px;border:1px solid #555;background:rgba(0,0,0,.4);color:#ccc;font-size:20px;cursor:pointer;line-height:1}',
    '.slsb .x:hover{border-color:var(--gold);color:#fff}',
    '.slsb .body{flex:1;display:flex;gap:18px;min-height:0;padding-top:16px}',
    '.slsb .btn{background:rgba(0,0,0,.45);color:#ddd;border:1px solid #5a5040;padding:8px 16px;font-size:15px;cursor:pointer;letter-spacing:1px;line-height:1.2}',
    '.slsb .btn:hover{border-color:var(--gold);color:#fff}',
    '.slsb .btn.pri{background:rgba(235,163,8,.2);border-color:var(--amber);color:var(--gold);font-weight:800}',
    '.slsb .btn.pri:hover{background:rgba(235,163,8,.35);color:#fff}',
    '.slsb .btn.bad{border-color:rgba(224,106,90,.6);color:#f0a094}.slsb .btn.bad:hover{border-color:var(--red);color:#fff}',
    '.slsb .btn.sm{padding:5px 10px;font-size:13px}',
    '.slsb .btn[disabled]{opacity:.35;pointer-events:none}',
    '.slsb .runs{width:470px;display:flex;flex-direction:column;min-height:0}',
    '.slsb .tools{display:flex;gap:8px;margin-bottom:10px}',
    '.slsb .search{flex:1;min-width:0;background:rgba(0,0,0,.5);border:1px solid #4a4236;color:#eee;padding:8px 12px;font-size:14px;outline:none}',
    '.slsb .search:focus,.slsb .num:focus,.slsb .path:focus,.slsb .note:focus,.slsb .mbd input:focus{border-color:var(--gold);outline:none}',
    '.slsb .chips{display:flex;gap:6px;margin-bottom:10px;flex-wrap:wrap}',
    '.slsb .chip{border:1px solid #4a4236;background:none;color:#aaa;padding:3px 11px;font-size:13px;cursor:pointer;border-radius:12px;white-space:nowrap}',
    '.slsb .chip:hover{color:#fff;border-color:#7a6a4a}',
    '.slsb .chip.on{border-color:var(--gold);color:var(--gold);background:rgba(255,200,58,.08)}',
    '.slsb .chip .w{color:var(--red);margin-left:3px}',
    '.slsb .list{flex:1;overflow-y:auto;padding-right:4px}',
    '.slsb .run{display:flex;gap:12px;padding:10px;margin-bottom:8px;border:1px solid #3a342a;background:var(--panel);cursor:pointer;position:relative}',
    '.slsb .run:hover{border-color:#7a6a4a}',
    '.slsb .run.on{border-color:var(--gold);background:rgba(40,32,18,.92)}',
    '.slsb .th{width:128px;height:72px;background:#1a1712 center/cover;flex:none;position:relative;border:1px solid #222}',
    '.slsb .pt{position:absolute;left:-6px;bottom:-6px;width:34px;height:34px;border-radius:50%;border:2px solid #111;background:#3a342a center top/cover;display:flex;align-items:center;justify-content:center;font-weight:800;color:#111;font-size:15px}',
    '.slsb .run .meta{flex:1;min-width:0}',
    '.slsb .run .nm{color:#fff;font-size:16px;font-weight:800;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.slsb .run .nm .pin{color:var(--gold);font-size:13px;margin-right:4px}',
    '.slsb .run .l2{color:#bbb;font-size:13px;margin-top:2px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.slsb .run .l3{color:var(--dim);font-size:12px;margin-top:4px;display:flex;gap:10px;white-space:nowrap;overflow:hidden}',
    '.slsb .badge{display:inline-block;font-size:11px;padding:1px 7px;border:1px solid;letter-spacing:1px;margin-right:6px;vertical-align:1px;text-transform:uppercase;white-space:nowrap}',
    '.slsb .b-live{color:var(--teal);border-color:rgba(143,199,191,.5)}',
    '.slsb .b-end{color:var(--gold);border-color:rgba(255,200,58,.5)}',
    '.slsb .b-old{color:#aaa;border-color:#666}',
    '.slsb .b-warn{color:var(--red);border-color:rgba(224,106,90,.6)}',
    '.slsb .b-endless{color:#c9a4ff;border-color:rgba(201,164,255,.5)}',
    '.slsb .b-playing{color:#fff;border-color:var(--teal);background:rgba(143,199,191,.18)}',
    '.slsb .newrun{border:1px dashed #5a5040;padding:12px;text-align:center;color:var(--dim);font-size:14px;cursor:pointer}',
    '.slsb .newrun:hover{color:#fff;border-color:var(--gold)}',
    '.slsb .newrun.plain{cursor:default}.slsb .newrun.plain:hover{color:var(--dim);border-color:#5a5040}',
    '.slsb .foot{display:flex;gap:8px;margin-top:10px}',
    '.slsb .disk{margin-top:8px;font-size:12px;color:var(--dim)}',
    '.slsb .disk .bar{height:6px;background:#222;margin-top:4px;display:flex}',
    '.slsb .disk .bar i{display:block;height:100%}',
    '.slsb .det{flex:1;display:flex;flex-direction:column;min-width:0;min-height:0;border:1px solid #3a342a;background:var(--panel)}',
    '.slsb .dtop{display:flex;gap:18px;padding:14px 18px;border-bottom:1px solid var(--line)}',
    '.slsb .big{width:272px;height:153px;background:#1a1712 center/cover;border:1px solid #222;flex:none;position:relative}',
    '.slsb .big .cap{position:absolute;left:0;right:0;bottom:0;padding:4px 8px;font-size:12px;color:#ddd;background:linear-gradient(transparent,rgba(0,0,0,.85))}',
    '.slsb .info{flex:1;min-width:0}',
    '.slsb .ttl{display:flex;align-items:center;gap:10px}',
    '.slsb .ttl h2{margin:0;font-size:24px;color:#fff;font-weight:800;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;min-width:0}',
    '.slsb .ttl .ed{color:var(--dim);cursor:pointer;font-size:14px;white-space:nowrap}',
    '.slsb .ttl .ed:hover{color:var(--gold)}',
    '.slsb .kv{display:grid;grid-template-columns:repeat(4,auto);gap:2px 18px;margin:6px 0 8px;font-size:14px;justify-content:start}',
    '.slsb .kv span{color:var(--dim);font-size:12px;display:block}',
    '.slsb .kv b{color:#eee;font-weight:400;white-space:nowrap}',
    '.slsb .note{width:100%;background:rgba(0,0,0,.35);border:1px solid #3a342a;color:#ccc;font-size:13px;padding:6px 9px;resize:none;height:34px;display:block}',
    '.slsb .acts{display:flex;gap:8px;margin-top:10px;flex-wrap:wrap}',
    '.slsb .dtop .acts .btn{padding:7px 13px;font-size:14px}',
    '.slsb .hint{color:var(--dim);font-size:13px;margin-top:10px;line-height:1.5}',
    '.slsb .hint.ir{color:var(--teal)}',
    '.slsb .warnbox{margin:10px 18px 0;padding:8px 14px;border-left:3px solid var(--red);background:rgba(224,106,90,.08);font-size:14px;color:#f2c6be;line-height:1.5}',
    '.slsb .warnbox b{color:#fff}.slsb .warnbox .acts{margin-top:8px}',
    '.slsb .tlhd{display:flex;align-items:center;flex-wrap:wrap;gap:4px 12px;padding:10px 18px 4px}',
    '.slsb .tlhd h3{margin:0;font-size:15px;color:var(--gold);letter-spacing:2px;text-transform:uppercase;white-space:nowrap}',
    '.slsb .lg{font-size:12px;color:var(--dim);display:flex;gap:4px 14px;flex-wrap:wrap;order:9;width:100%}',
    '.slsb .lg span{white-space:nowrap}',
    '.slsb .dot{display:inline-block;width:9px;height:9px;border-radius:50%;margin-right:4px;flex:none}',
    '.slsb .tl{flex:1;display:flex;min-height:0}',
    '.slsb .pts{width:54%;overflow-y:auto;padding:4px 8px 12px 18px}',
    '.slsb .dayh{color:var(--dim);font-size:12px;letter-spacing:2px;margin:10px 0 4px;display:flex;align-items:center;gap:8px;text-transform:uppercase}',
    '.slsb .dayh::after{content:"";flex:1;height:1px;background:#2e2a22}',
    '.slsb .pt2{display:flex;align-items:center;gap:10px;padding:6px 8px;cursor:pointer;border:1px solid transparent;font-size:14px}',
    '.slsb .pt2:hover{background:rgba(255,255,255,.03)}',
    '.slsb .pt2.on{border-color:var(--gold);background:rgba(255,200,58,.06)}',
    '.slsb .pt2 .tm{width:46px;color:#bbb;font-family:Consolas,monospace;font-size:13px;flex:none}',
    '.slsb .pt2 .rs{flex:1;color:#ddd;min-width:0;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.slsb .pt2 .rs small{color:var(--dim);margin-left:6px}',
    '.slsb .pt2 .rs i{color:#d8c9a6;margin-left:6px;font-size:12px}',
    '.slsb .pt2 .ic{color:var(--gold);font-size:13px}',
    '.slsb .pt2 .cur{color:var(--teal);font-size:12px;white-space:nowrap}',
    '.slsb .pt2 .old{color:#aaa;font-size:11px;white-space:nowrap}',
    '.slsb .pv{flex:1;min-width:0;border-left:1px solid var(--line);padding:12px 18px;overflow-y:auto}',
    '.slsb .shot{width:100%;height:170px;background:#1a1712 center/cover;border:1px solid #222;display:flex;align-items:center;justify-content:center;color:var(--dim);font-size:13px;text-align:center;padding:10px}',
    '.slsb .pv h4{margin:10px 0 2px;color:#fff;font-size:17px}',
    '.slsb .when{color:var(--dim);font-size:13px}',
    '.slsb .stats{display:grid;grid-template-columns:repeat(4,1fr);gap:6px;margin:10px 0}',
    '.slsb .st{background:rgba(0,0,0,.35);border:1px solid #2e2a22;padding:6px 8px;font-size:12px;color:var(--dim);white-space:nowrap;overflow:hidden}',
    '.slsb .st b{display:block;color:#eee;font-size:15px;font-weight:400}',
    '.slsb .st b small{color:var(--dim);font-size:11px}',
    '.slsb .st .d{font-size:12px;margin-left:4px}',
    '.slsb .up{color:#8fd08f}.slsb .dn{color:var(--red)}',
    '.slsb .journal{font-size:13px;color:#c8bca6;line-height:1.6;border-left:2px solid #4a4236;padding-left:10px;margin:8px 0 12px;font-style:italic}',
    '.slsb .nb{margin:8px 0;padding:8px 10px;border:1px solid #3a342a;background:rgba(0,0,0,.3);color:#e6d8b8;font-size:13px;word-break:break-word}',
    '.slsb .ow{margin:8px 0;padding:8px 12px;border-left:3px solid #e0a060;background:rgba(224,160,96,.08);color:#f0d2b0;font-size:13px;line-height:1.5}',
    '.slsb .pv .acts{margin-top:8px;gap:6px}.slsb .pv .acts .btn{padding:6px 11px;font-size:14px}',
    '.slsb .photos{display:grid;grid-template-columns:1fr 1fr;gap:6px;margin-top:10px}',
    '.slsb .photos div{aspect-ratio:16/9;background:#1a1712 center/cover;border:1px solid #222}',
    '.slsb .empty{border:1px dashed #5a5040;padding:14px;text-align:center;color:var(--dim);font-size:14px;line-height:1.6;margin:6px 18px 16px}',
    '.slsb .set{flex:1;overflow-y:auto;display:grid;grid-template-columns:1fr 1fr;gap:18px;align-content:start;padding-right:4px}',
    '.slsb .card{border:1px solid #3a342a;background:var(--panel);padding:14px 18px}',
    '.slsb .card+.card{margin-top:18px}',
    '.slsb .card h3{margin:0 0 4px;color:var(--gold);font-size:15px;letter-spacing:2px;text-transform:uppercase}',
    '.slsb .card .ex{color:var(--dim);font-size:13px;margin:0 0 10px;line-height:1.5}',
    '.slsb .row{display:flex;align-items:center;gap:10px;padding:7px 0;border-bottom:1px solid #25221c;font-size:14px}',
    '.slsb .row:last-child{border-bottom:0}',
    '.slsb .row .lb{flex:1;min-width:0}',
    '.slsb .row .lb small{display:block;color:var(--dim);font-size:12px}',
    '.slsb .row .u{color:var(--dim);font-size:12px}',
    '.slsb .tg{width:40px;height:20px;border-radius:10px;background:#333;position:relative;cursor:pointer;flex:none}',
    '.slsb .tg::after{content:"";position:absolute;left:2px;top:2px;width:16px;height:16px;border-radius:50%;background:#888;transition:.15s}',
    '.slsb .tg.on{background:rgba(235,163,8,.45)}.slsb .tg.on::after{left:22px;background:var(--gold)}',
    '.slsb .tgx{width:40px;flex:none}',
    '.slsb .num{width:64px;background:rgba(0,0,0,.5);border:1px solid #4a4236;color:#eee;padding:4px 6px;font-size:13px;text-align:right}',
    '.slsb .path{flex:1;min-width:0;background:rgba(0,0,0,.5);border:1px solid #4a4236;color:#ccc;padding:5px 8px;font:12px Consolas,monospace}',
    '.slsb .keycap{border:1px solid #777;padding:3px 10px;font:13px Consolas,monospace;color:#fff;background:#1b1b1b;cursor:pointer;min-width:60px;text-align:center}',
    '.slsb .keycap.on{border-color:var(--gold);color:var(--gold)}',
    '.slsb .keyin{position:absolute;opacity:0;width:1px;height:1px;pointer-events:none}',
    '.slsb .trash{flex:1;border:1px solid #3a342a;background:var(--panel);padding:14px 18px;overflow-y:auto}',
    '.slsb .trash .card{margin-bottom:12px;background:rgba(0,0,0,.25)}',
    '.slsb .sec{color:var(--dim);font-size:12px;letter-spacing:2px;margin:14px 0 4px;display:flex;align-items:center;gap:8px;text-transform:uppercase}',
    '.slsb .sec::after{content:"";flex:1;height:1px;background:#2e2a22}',
    '.slsb .trow{display:flex;align-items:center;gap:14px;padding:10px 0;border-bottom:1px solid #25221c;font-size:14px}',
    '.slsb .trow .th{width:96px;height:54px;filter:grayscale(.6)}',
    '.slsb .trow .m{flex:1;min-width:0}.slsb .trow .m small{display:block;color:var(--dim)}',
    '.slsb .modal{position:absolute;inset:0;background:rgba(0,0,0,.72);display:flex;align-items:center;justify-content:center;z-index:20}',
    '.slsb .mbox{width:720px;max-height:860px;display:flex;flex-direction:column;border:1px solid var(--amber);background:#12100c;box-shadow:0 20px 60px rgba(0,0,0,.8)}',
    '.slsb .mbox.wide{width:980px}',
    '.slsb .mhd{padding:14px 20px;border-bottom:1px solid var(--line);font-size:20px;font-weight:800;color:#fff;letter-spacing:2px;display:flex;align-items:center;text-transform:uppercase}',
    '.slsb .mhd .x{width:30px;height:30px;font-size:16px}',
    '.slsb .mbd{padding:16px 20px;font-size:15px;line-height:1.6;color:#ccc;overflow-y:auto}',
    '.slsb .mbd b{color:#fff}',
    '.slsb .mbd .mh{color:var(--dim);font-size:13px;margin-top:10px}',
    '.slsb .mbd input.txt{width:100%;background:rgba(0,0,0,.5);border:1px solid #4a4236;color:#eee;padding:8px 12px;font-size:15px;margin-top:8px}',
    '.slsb .mft{padding:12px 20px;border-top:1px solid var(--line);display:flex;gap:10px;justify-content:flex-end}',
    '.slsb .choice{display:flex;gap:12px;margin-top:12px}',
    '.slsb .opt{flex:1;border:1px solid #4a4236;padding:12px 14px;cursor:pointer}',
    '.slsb .opt.on{border-color:var(--gold);background:rgba(255,200,58,.07)}',
    '.slsb .opt.off{opacity:.45;cursor:default}',
    '.slsb .opt b{display:block;margin-bottom:4px}',
    '.slsb .opt small{color:var(--dim);font-size:13px;line-height:1.5;display:block}',
    '.slsb .vs{display:grid;grid-template-columns:1fr 1fr;gap:16px}',
    '.slsb .vs .shot{height:250px}.slsb .vs .who{margin:6px 0 0;color:#fff;font-weight:800}',
    '.slsb .vs .who small{display:block;color:var(--dim);font-weight:400;font-size:13px}',
    '.slsb table.cmp{width:100%;border-collapse:collapse;font-size:14px;margin-top:12px}',
    '.slsb table.cmp th,.slsb table.cmp td{padding:5px 10px;border-bottom:1px solid #25221c;text-align:right}',
    '.slsb table.cmp th:first-child,.slsb table.cmp td:first-child{text-align:left;color:#bbb}',
    '.slsb table.cmp th{color:var(--dim);font-weight:400;font-size:12px;letter-spacing:1px}',
    '.slsb .ihd{margin-top:12px;color:var(--gold);font-size:13px;letter-spacing:2px;text-transform:uppercase}',
    '.slsb .itemdiff{display:grid;grid-template-columns:1fr 1fr;gap:4px 24px;font-size:13px;margin-top:8px}',
    '.slsb .mrow{display:flex;align-items:center;gap:10px;padding:7px 0;border-bottom:1px solid #25221c;font-size:14px}',
    '.slsb .mrow .lb{flex:1}.slsb .mrow .lb small{display:block;color:var(--dim);font-size:12px}',
    '.slsb .ifile{border:1px solid #3a342a;padding:8px 12px;margin-top:6px;cursor:pointer;font-size:14px}',
    '.slsb .ifile:hover{border-color:#7a6a4a}.slsb .ifile.on{border-color:var(--gold);background:rgba(255,200,58,.06)}',
    '.slsb .ifile.bad{opacity:.5;cursor:default}',
    '.slsb .ifile small{display:block;color:var(--dim);font-size:12px}'
  ].join('');

  var view = null;
  var detail = null;
  var detailKey = '';
  var imports = null;
  var tab = 'saves';
  var selected = null;
  var pointKey = null;
  var filter = 'all';
  var sortBy = 'played';
  var query = '';
  var showSnaps = true;
  var modal = null;
  var isBusy = false;
  var isTyping = false;
  var isListening = false;
  var pageEscAt = 0;
  var drafts = {};

  function post(parts) {
    if (window.vuplex) window.vuplex.postMessage(PREFIX + parts.join('|'));
  }

  function step(parts) {
    isBusy = true;
    var box = document.querySelector('#' + ROOT_ID + ' .slsb');
    if (box) box.classList.add('busy');
    post(parts);
  }

  // The Toolkit's shared sizing, or a queue it picks up once it loads.
  function uiSize(node, options) {
    var ui = window.ExiledUi;
    if (ui) { ui.size(node, options); return; }
    var waiting = window.__exiledUiWait = window.__exiledUiWait || [];
    waiting.push([node, options]);
    if (waiting.length > 32) waiting.shift();
  }

  function isZh() {
    return !!(view && view.zh);
  }

  function pick(pair) {
    return pair ? pair[isZh() ? 1 : 0] : '';
  }

  function fill(text, args) {
    for (var i = 0; i < args.length; i++) text = text.split('{' + i + '}').join(String(args[i]));
    return text;
  }

  // A text by key, with {0}, {1}... filled from the remaining arguments.
  function t(key) {
    return fill(TEXT[key] ? pick(TEXT[key]) : key, Array.prototype.slice.call(arguments, 1));
  }

  function tf(group, key) {
    return fill(pick(TEXT[group][key]), Array.prototype.slice.call(arguments, 2));
  }

  function esc(s) {
    return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
    });
  }

  // A file:// URL as a CSS background, safe inside a style attribute.
  function bgUrl(url) {
    if (!url) return '';
    return 'background-image:url(&quot;' + esc(String(url).replace(/\\/g, '/').replace(/"/g, '%22')) + '&quot;)';
  }

  function size(bytes) {
    bytes = bytes || 0;
    if (bytes >= 1073741824) return (bytes / 1073741824).toFixed(1) + ' GB';
    if (bytes >= 1048576) return (bytes / 1048576).toFixed(1) + ' MB';
    return Math.max(1, Math.round(bytes / 1024)) + ' KB';
  }

  function ago(seconds) {
    if (seconds == null) return '';
    if (seconds < 60) return tf('ago', 'now');
    if (seconds < 3600) return tf('ago', 'min', Math.floor(seconds / 60));
    if (seconds < 86400) return tf('ago', 'hour', Math.floor(seconds / 3600));
    if (seconds < 172800) return tf('ago', 'day');
    return tf('ago', 'days', Math.floor(seconds / 86400));
  }

  function playTime(seconds) {
    if (!seconds) return '-';
    return t('playTime', Math.floor(seconds / 3600), Math.floor(seconds % 3600 / 60));
  }

  function where(item) {
    var at = item.clock ? t('dayAt', item.day, item.clock) : t('day', item.day);
    return item.turn > 1 ? at + DOT + t('loop', item.turn) : at;
  }

  function dayGroup(item) {
    return item.turn > 1 ? t('loopDay', item.turn, item.day) : t('day', item.day);
  }

  // The game's WebUI folder, from this page's own address; the survivors' pictures are in it.
  function webRoot() {
    var at = location.href.indexOf('/WebUI/');
    return at >= 0 ? location.href.slice(0, at + 7) : '';
  }

  // The game writes a survivor's portrait as "../../Res/NPC/Play01.png" (from a page two folders down) and the
  // background as a bare file name in UI/SelectPeople.
  function gamePic(path, folder) {
    if (!path) return '';
    if (/^(\.\.\/)+/.test(path)) return webRoot() + path.replace(/^(\.\.\/)+/, '');
    return path.indexOf('/') >= 0 ? webRoot() + path : webRoot() + folder + path;
  }

  function survivor(run) {
    return (view.survivors || {})[String(run.survivor)] || null;
  }

  function survivorName(run) {
    var s = survivor(run);
    if (!s) return run.role || '';
    return (isZh() ? s.zh || s.en : s.en || s.zh) || run.role || '';
  }

  function survivorFull(run) {
    var s = survivor(run);
    if (!s || isZh() || !s.zh || s.zh === s.en) return survivorName(run);
    return s.en + ' 「' + s.zh + '」';
  }

  function shotOf(stem, backup) {
    return backup && backup.shot && view.shots ? view.shots + encodeURIComponent(stem) + '/' + encodeURIComponent(backup.id) + '/shot.jpg' : '';
  }

  function binShot(entry) {
    return entry.shot && view.shots ? view.shots + 'Bin/' + encodeURIComponent(entry.stem) + '/' + encodeURIComponent(entry.id) + '/shot.jpg' : '';
  }

  function newestShot(run) {
    var backups = run.backups || [];
    for (var i = 0; i < backups.length; i++) if (backups[i].shot) return backups[i];
    return null;
  }

  // The picture a run is shown with: its newest backup picture, else the survivor's new game background.
  function runPic(run) {
    var withShot = newestShot(run);
    if (withShot) return shotOf(run.stem, withShot);
    var s = survivor(run);
    return s ? gamePic(s.bg, 'UI/SelectPeople/') : '';
  }

  function portrait(run) {
    var s = survivor(run);
    var pic = s ? gamePic(s.pic, 'Res/NPC/') : '';
    if (pic) return '<div class="pt" style="' + bgUrl(pic) + '"></div>';
    var name = survivorName(run);
    return '<div class="pt" style="background:#d8b070">' + esc(name ? name.charAt(0).toUpperCase() : '?') + '</div>';
  }

  // The game names a save after the moment it was started: 2026_10_07_10_20_23.
  function isStampName(name) {
    return /^\d{4}_\d\d_\d\d_\d\d_\d\d/.test(name || '');
  }

  function title(run) {
    if (run.name && !isStampName(run.name)) return run.name;
    var m = /(\d{4})_(\d\d)_(\d\d)_(\d\d)_(\d\d)/.exec(run.name || run.stem || '');
    var started = m ? m[1] + '-' + m[2] + '-' + m[3] + ' ' + m[4] + ':' + m[5] : '';
    return survivorName(run) ? survivorName(run) + (started ? ' ' + started : '') : started || run.stem;
  }

  function stateOf(run) {
    if (run.behind) return 'warn';
    if (run.finished) return 'end';
    if (run.endless) return 'endless';
    return 'live';
  }

  function badge(kind) {
    return '<span class="badge b-' + kind + '">' + esc(tf('badge', kind)) + '</span>';
  }

  function isPlaying(run) {
    return !!view.inRun && run.stem === view.inRun;
  }

  function backupBytes(run) {
    var total = 0;
    (run.backups || []).forEach(function (b) { total += b.bytes || 0; });
    return total;
  }

  function kindName(kind) {
    return KINDS[kind] ? pick(KINDS[kind].n) : kind;
  }

  function kindColor(kind) {
    return KINDS[kind] ? KINDS[kind].c : '#888';
  }

  function setting(id) {
    return (view.settings || {})[id];
  }

  function findRun(stem) {
    var runs = view.runs || [];
    for (var i = 0; i < runs.length; i++) if (runs[i].stem === stem) return runs[i];
    return null;
  }

  // ---------- the saves tab ----------

  function matches(run) {
    var state = stateOf(run);
    if (filter === 'live' && state !== 'live' && state !== 'warn') return false;
    if (filter === 'end' && !run.finished) return false;
    if (filter === 'endless' && !run.endless) return false;
    if (filter === 'warn' && !run.behind) return false;
    var q = query.trim().toLowerCase();
    if (!q) return true;
    var s = survivor(run) || {};
    var text = [run.name, run.stem, run.role, s.en, s.zh, run.diff, run.status, run.note, title(run)].join(' ').toLowerCase();
    (run.backups || []).forEach(function (b) { text += ' ' + (b.note || '').toLowerCase(); });
    return text.indexOf(q) >= 0;
  }

  function sorted(runs) {
    var by = {
      played: function (a, b) { return (a.lastPlayedAgo == null ? 1e12 : a.lastPlayedAgo) - (b.lastPlayedAgo == null ? 1e12 : b.lastPlayedAgo); },
      days: function (a, b) { return (b.turn * 1000 + b.day) - (a.turn * 1000 + a.day); },
      survivor: function (a, b) { return survivorName(a).localeCompare(survivorName(b)); },
      name: function (a, b) { return title(a).localeCompare(title(b)); }
    }[sortBy];
    return runs.slice().sort(function (a, b) { return (b.pinned ? 1 : 0) - (a.pinned ? 1 : 0) || by(a, b); });
  }

  function shownRuns() {
    return sorted((view.runs || []).filter(matches));
  }

  function runRow(run) {
    var state = stateOf(run);
    var l3 = [where(run)];
    if (run.finished && run.status) l3.push(run.status);
    else l3.push(run.endless ? t('endless') : t('survival'));
    if (run.lastPlayedAgo != null) l3.push(ago(run.lastPlayedAgo));
    return '<div class="run' + (run.stem === selected ? ' on' : '') + '" data-run="' + esc(run.stem) + '">' +
      '<div class="th" style="' + bgUrl(runPic(run)) + '">' + portrait(run) + '</div>' +
      '<div class="meta"><div class="nm">' + (run.pinned ? '<span class="pin">&#9733;</span>' : '') + esc(title(run)) + '</div>' +
      '<div class="l2">' + (isPlaying(run) ? badge('playing') : badge(state)) + esc(survivorName(run)) + (run.diff ? DOT + esc(run.diff) : '') + '</div>' +
      '<div class="l3">' + l3.map(function (x) { return '<span>' + esc(x) + '</span>'; }).join('') + '</div></div></div>';
  }

  function listHtml() {
    var runs = shownRuns();
    var html = runs.map(runRow).join('');
    if (!html) html = '<div class="newrun plain">' + esc((view.runs || []).length ? t('noMatch') : t('noRuns')) + '</div>';
    if (!view.inRun) html += '<div class="newrun" data-act="newgame">' + esc(t('newGame')) + '</div>';
    return html;
  }

  function diskHtml() {
    var bytes = view.bytes || 0;
    var limit = view.limit || 0;
    var line = (limit ? t('diskOf', size(bytes), size(limit)) : t('disk', size(bytes))) + (view.saveBytes ? DOT + t('diskSaves', size(view.saveBytes)) : '');
    var bar = '';
    if (limit) {
      var full = bytes / limit >= 0.95;
      bar = '<div class="bar"><i style="width:' + Math.min(100, Math.max(bytes ? 1 : 0, bytes / limit * 100)).toFixed(1) + '%;background:' + (full ? '#e06a5a' : '#8fc7bf') + '"></i>' +
        '<i style="width:' + Math.min(100, (view.saveBytes || 0) / limit * 100).toFixed(1) + '%;background:#EBA308"></i></div>';
    }
    return '<div class="disk">' + esc(line) + bar + '</div>';
  }

  function runsPane() {
    var warned = (view.runs || []).filter(function (r) { return r.behind; }).length;
    var chips = FILTERS.map(function (k) {
      return '<button class="chip' + (filter === k ? ' on' : '') + '" data-filter="' + k + '">' + esc(tf('filters', k)) +
        (k === 'warn' && warned ? '<span class="w">' + warned + '</span>' : '') + '</button>';
    }).join('') + '<span class="sp"></span><button class="chip" data-act="sort">' + esc(t('sortBy', tf('sorts', sortBy))) + '</button>';
    return '<div class="runs">' +
      '<div class="tools"><input class="search" data-focus="q" data-in="query" type="text" maxlength="60" spellcheck="false" placeholder="' + esc(t('search')) + '" value="' + esc(query) + '">' +
      '<button class="btn sm" data-act="import">' + esc(t('importBtn')) + '</button></div>' +
      '<div class="chips">' + chips + '</div>' +
      '<div class="list">' + listHtml() + '</div>' +
      '<div class="foot">' + (view.inRun ? '' : '<button class="btn sm" data-act="gamelist">' + esc(t('gameList')) + '</button>') +
      '<button class="btn sm" data-act="folder">' + esc(t('openFolder')) + '</button></div>' +
      diskHtml() + '</div>';
  }

  function kv(label, value) {
    return '<div><span>' + esc(label) + '</span><b>' + value + '</b></div>';
  }

  function detailPane(run) {
    if (!run) return '<div class="det"><div class="empty">' + esc(t('noRuns')) + '</div></div>';
    var withShot = newestShot(run);
    var newest = (run.backups || [])[0];
    var playing = isPlaying(run);
    var cap = withShot ? t('lastPicture', where(withShot)) : t('noPicture');
    var version = newest && newest.version ? esc(newest.version) + (newest.oldVersion ? ' ' + badge('old') : '') : esc(view.version || '-');
    var mode = (run.endless ? t('endless') : t('survival')) + (run.turn > 1 ? ', ' + t('loop', run.turn) : '');
    var note = drafts['note:' + run.stem] != null ? drafts['note:' + run.stem] : run.note || '';

    var acts = '';
    if (!view.inRun) acts += '<button class="btn pri" data-act="load">' + esc(run.finished ? t('lookEnding') : t('cont')) + '</button>';
    acts += '<button class="btn' + (view.inRun ? ' pri' : '') + '" data-act="backup">' + esc(t('backUp')) + '</button>' +
      '<button class="btn" data-act="duplicate">' + esc(t('duplicate')) + '</button>' +
      '<button class="btn" data-act="export">' + esc(t('exportBtn')) + '</button>' +
      '<button class="btn" data-act="runfolder">' + esc(t('openFolder')) + '</button>';
    if (!playing) acts += '<button class="btn bad" data-act="delrun">' + esc(t('del')) + '</button>';

    var html = '<div class="det"><div class="dtop">' +
      '<div class="big" style="' + bgUrl(runPic(run)) + '"><div class="cap">' + esc(cap) + '</div></div>' +
      '<div class="info"><div class="ttl"><h2>' + esc(title(run)) + '</h2>' + (playing ? badge('playing') : '') +
      (playing ? '' : '<span class="ed" data-act="rename">' + esc(t('rename')) + '</span>') +
      '<span class="ed" data-act="runpin">' + esc(run.pinned ? t('unpin') : t('pin')) + '</span></div>' +
      '<div class="kv">' +
      kv(t('kvSurvivor'), esc(survivorFull(run) || '-')) +
      kv(t('kvDiff'), esc(run.diff || '-')) +
      kv(t('kvMode'), esc(mode)) +
      kv(t('kvReached'), esc(run.clock ? t('dayAt', run.day, run.clock) : t('day', run.day))) +
      kv(run.finished ? t('kvStatus') : t('kvPlayed'), esc(run.finished ? run.status || '-' : playTime(run.played))) +
      kv(t('kvLast'), esc(run.lastPlayedAgo != null ? ago(run.lastPlayedAgo) : '-')) +
      kv(t('kvVersion'), version) +
      kv(t('kvSize'), esc(size((run.saveBytes || 0) + backupBytes(run)))) +
      '</div>' +
      '<textarea class="note" data-focus="note" data-note="' + esc(run.stem) + '" maxlength="300" spellcheck="false" placeholder="' + esc(t('notePlaceholder')) + '">' + esc(note) + '</textarea>' +
      '<div class="acts">' + acts + '</div>' +
      (playing ? '<div class="hint ir">' + esc(t('playingHint')) + '</div>' : view.inRun ? '<div class="hint">' + esc(t('otherInRunHint')) + '</div>' : '') +
      '</div></div>';

    if (run.behind) html += behindBox(run);
    html += timeline(run) + '</div>';
    return html;
  }

  function backupById(run, id) {
    var backups = run.backups || [];
    for (var i = 0; i < backups.length; i++) if (backups[i].id === id) return backups[i];
    return null;
  }

  function behindBox(run) {
    var b = backupById(run, run.behind.id);
    return '<div class="warnbox"><b>' + esc(t('behindTitle')) + '</b> ' +
      esc(t('behindBody', where(run), b ? b.at : run.behind.id, where(run.behind))) +
      '<div class="acts">' + (b ? '<button class="btn sm pri" data-act="cloud">' + esc(t('lookBoth')) + '</button>' : '') +
      '<button class="btn sm" data-act="dismiss">' + esc(t('dismiss')) + '</button></div></div>';
  }

  function detailFor(run) {
    return detail && detail.stem === run.stem ? detail : null;
  }

  // Every point on a run's timeline, newest first: the save as it is now, its backups, and the game's own snapshots.
  function points(run) {
    var list = [{ key: 'now', kind: 'now', day: run.day, turn: run.turn, clock: run.clock, at: run.lastPlayed || '', isNow: true, progress: true }];
    (run.backups || []).forEach(function (b, i) {
      list.push({ key: 'b:' + b.id, kind: b.kind, day: b.day, turn: b.turn, clock: b.clock, at: b.at, backup: b, isNewest: i === 0, progress: b.progress });
    });
    var d = detailFor(run);
    if (d && showSnaps) {
      (d.snaps || []).forEach(function (s) {
        list.push({ key: 's:' + s.file, kind: s.round ? 'round' : 'snap', day: s.day, turn: s.turn, clock: s.clock, at: s.at, snap: s, progress: true });
      });
    }
    var rest = list.slice(1).sort(function (a, b) {
      return a.at < b.at ? 1 : a.at > b.at ? -1 : (a.backup ? -1 : 1) - (b.backup ? -1 : 1);
    });
    return [list[0]].concat(rest);
  }

  function pickedPoint(pts) {
    for (var i = 0; i < pts.length; i++) if (pts[i].key === pointKey) return pts[i];
    pointKey = pts.length > 1 ? pts[1].key : pts[0].key;
    return pts.length > 1 ? pts[1] : pts[0];
  }

  function timeline(run) {
    var pts = points(run);
    var hasSnapsSetting = setting('Screen.ShowGameSnapshots') !== 'false';
    var head = '<div class="tlhd"><h3>' + esc(t('points')) + '</h3>';
    var seen = {};
    pts.forEach(function (p) { seen[p.kind] = true; });
    head += '<span class="lg">' + LEGEND.filter(function (k) { return seen[k]; }).map(function (k) {
      return '<span><i class="dot" style="background:' + kindColor(k) + '"></i>' + esc(kindName(k)) + '</span>';
    }).join('') + '</span><span class="sp"></span>';
    if (hasSnapsSetting) head += '<button class="chip' + (showSnaps ? ' on' : '') + '" data-act="snaps">' + esc(t('showSnaps')) + '</button>';
    head += '</div>';

    if (pts.length === 1) {
      var key = view.key && view.key !== 'None' ? t('keyHint', view.key) : t('pauseHint');
      return head + '<div class="empty">' + esc(t('noPoints', key)) + '</div>';
    }

    var sel = pickedPoint(pts);
    var rows = '';
    var lastGroup = null;
    pts.forEach(function (p) {
      var group = p.progress ? dayGroup(p) : (p.at || '').slice(0, 10);
      if (group !== lastGroup) rows += '<div class="dayh">' + esc(group) + '</div>';
      lastGroup = group;
      var b = p.backup;
      var extra = p.isNow ? t('nowRow') : b && b.note ? '' : p.at ? p.at.slice(5) : '';
      rows += '<div class="pt2' + (p.key === sel.key ? ' on' : '') + '" data-pt="' + esc(p.key) + '">' +
        '<span class="tm">' + esc(p.clock || (p.at || '').slice(11)) + '</span><span class="dot" style="background:' + kindColor(p.kind) + '"></span>' +
        '<span class="rs">' + esc(kindName(p.kind)) + (extra ? '<small>' + esc(extra) + '</small>' : '') + (b && b.note ? '<i>' + esc(b.note) + '</i>' : '') + '</span>' +
        (p.isNow ? '<span class="cur">' + esc(t('now')) + '</span>' : '') +
        (p.isNewest ? '<span class="cur">' + esc(t('newest')) + '</span>' : '') +
        (b && b.oldVersion ? '<span class="old">' + esc(tf('badge', 'old')) + '</span>' : '') +
        (b && b.pinned ? '<span class="ic" title="' + esc(t('kept')) + '">&#9733;</span>' : '') + '</div>';
    });
    return head + '<div class="tl"><div class="pts">' + rows + '</div>' + preview(run, sel) + '</div>';
  }

  function statsOf(run, p) {
    if (p.isNow) { var d = detailFor(run); return d ? d.now || null : null; }
    if (p.backup) return p.backup.stats || null;
    return p.snap ? p.snap.stats || null : null;
  }

  function statValue(stats, key) {
    var v = stats[key];
    return Array.isArray(v) ? v[0] : v;
  }

  function statCell(label, stats, key, nowStats) {
    var v = stats[key];
    var shown = Array.isArray(v) ? Math.round(v[0]) + '<small> / ' + Math.round(v[1]) + '</small>' : esc(v);
    var delta = '';
    if (nowStats && nowStats !== stats && nowStats[key] != null) {
      var d = Math.round(statValue(stats, key) - statValue(nowStats, key));
      if (d) delta = '<span class="d ' + (d > 0 ? 'up' : 'dn') + '">' + (d > 0 ? '+' : '') + d + '</span>';
    }
    return '<div class="st">' + esc(label) + '<b>' + shown + delta + '</b></div>';
  }

  function statsGrid(run, p) {
    var stats = statsOf(run, p);
    if (!stats) return detailFor(run) || !p.isNow ? '' : '<div class="when">' + esc(t('loading')) + '</div>';
    var d = detailFor(run);
    var nowStats = d ? d.now : null;
    return '<div class="stats">' + ['health', 'fullness', 'mood', 'stamina', 'money', 'points', 'items'].map(function (k) {
      return stats[k] == null ? '' : statCell(tf('st', k), stats, k, nowStats);
    }).join('') + '</div>' + (stats.journal ? '<div class="journal">' + esc(stats.journal) + '</div>' : '');
  }

  function preview(run, p) {
    var b = p.backup;
    var pic = p.isNow ? runPic(run) : b ? shotOf(run.stem, b) : '';
    var html = '<div class="pv"><div class="shot" style="' + bgUrl(pic) + '">' + (pic ? '' : esc(t('noPicture'))) + '</div>';
    html += '<h4>' + esc(where(p)) + DOT + esc(kindName(p.kind)) + '</h4>';
    var when = [];
    if (p.at) when.push(p.at);
    if (b) { when.push(b.auto ? t('madeAuto') : t('madeYou')); when.push(size(b.bytes)); if (b.version) when.push(b.version); }
    if (p.snap) { when.push(size(p.snap.bytes)); when.push(t('byGame')); }
    html += '<div class="when">' + esc(when.join(DOT)) + '</div>';

    // The buttons sit right under the heading so they never scroll out of sight.
    if (p.isNow) {
      html += '<div class="when">' + esc(t('nowWhere')) + '</div>';
    } else if (b) {
      html += '<div class="acts"><button class="btn pri" data-act="restore">' + esc(t('loadPoint')) + '</button>' +
        (b.stats ? '<button class="btn" data-act="compare">' + esc(t('compare')) + '</button>' : '') +
        '<button class="btn" data-act="keep">' + esc(b.pinned ? t('unkeep') : t('keep')) + '</button>' +
        '<button class="btn sm" data-act="ptnote">' + esc(t('noteBtn')) + '</button>' +
        '<button class="btn sm" data-act="ptexport">' + esc(t('exportBtn')) + '</button>' +
        '<button class="btn sm bad" data-act="delpt">' + esc(t('del')) + '</button></div>';
    } else if (p.snap) {
      html += '<div class="acts"><button class="btn pri" data-act="snapnew">' + esc(t('loadAsNew')) + '</button>' +
        (p.snap.stats ? '<button class="btn" data-act="compare">' + esc(t('compare')) + '</button>' : '') + '</div>';
    }

    if (b && b.note) html += '<div class="nb">' + esc(b.note) + '</div>';
    if (b && b.oldVersion) html += '<div class="ow">' + esc(t('oldWarn', b.version, view.version)) + '</div>';
    html += statsGrid(run, p);
    if (b && !b.stats && b.dayText) html += '<div class="journal">' + esc(b.dayText) + '</div>';
    if (p.isNow) {
      var d = detailFor(run);
      if (d && d.photos && d.photos.length) {
        html += '<div class="photos">' + d.photos.slice(0, 6).map(function (url) { return '<div style="' + bgUrl(url) + '"></div>'; }).join('') + '</div>';
      }
    }
    return html + '</div>';
  }

  // ---------- settings ----------

  function toggle(id) {
    if (setting(id) == null) return '<span class="tgx"></span>';
    return '<span class="tg' + (setting(id) === 'true' ? ' on' : '') + '" data-act="tg" data-id="' + esc(id) + '"></span>';
  }

  function numBox(id) {
    if (setting(id) == null) return '';
    var value = drafts['set:' + id] != null ? drafts['set:' + id] : setting(id);
    return '<input class="num" type="text" inputmode="numeric" maxlength="7" spellcheck="false" data-focus="set:' + esc(id) + '" data-set="' + esc(id) + '" value="' + esc(value) + '">';
  }

  function label(quad) {
    var main = isZh() ? quad[1] : quad[0];
    var small = isZh() ? quad[3] : quad[2];
    return '<span class="lb">' + esc(main) + (small ? '<small>' + esc(small) + '</small>' : '') + '</span>';
  }

  var TRIGGERS = [
    ['morning', 'Backups.OnMorning', 'Keep.Morning'],
    ['expedition', 'Backups.BeforeExpedition', 'Keep.Expedition'],
    ['wave', 'Backups.BeforeWave', 'Keep.Wave'],
    ['rebirth', 'Backups.BeforeRebirth', 'Keep.Rebirth'],
    ['ending', 'Backups.AtEnding', 'Keep.Ending'],
    ['quit', 'Backups.OnQuit', 'Keep.Quit'],
    ['timer', 'Backups.OnTimer', 'Keep.Timer'],
    ['manual', '', 'Keep.Manual'],
    ['safety', '', 'Keep.Safety']
  ];

  function pathRow(id) {
    var value = drafts['set:' + id] != null ? drafts['set:' + id] : setting(id) || '';
    return '<div class="row"><input class="path" type="text" maxlength="240" spellcheck="false" data-focus="set:' + esc(id) + '" data-path="' + esc(id) + '" value="' + esc(value) + '"' +
      (id === 'Backups.Folder' ? ' placeholder="' + esc(view.folder || '') + '"' : '') + '>' +
      '<button class="btn sm" data-act="setpath" data-id="' + esc(id) + '">' + esc(t('setPath')) + '</button></div>';
  }

  function keyText() {
    if (isListening) return t('keyPress');
    var key = setting('Controls.BackupKey');
    return !key || key === 'None' ? t('keyNone') : key;
  }

  function settingsPane() {
    var trig = TRIGGERS.map(function (row) {
      var extra = row[0] === 'timer' ? numBox('Backups.TimerMinutes') + '<span class="u">' + esc(t('minutes')) + '</span>' : '';
      return '<div class="row">' + label(TEXT.trig[row[0]]) + extra + '<span class="u">' + esc(t('keepN')) + '</span>' + numBox(row[2]) + (row[1] ? toggle(row[1]) : '<span class="tgx"></span>') + '</div>';
    }).join('');
    var keep = '<div class="row">' + label(TEXT.keepAll) + numBox('Keep.KeepAllDays') + '<span class="u">' + esc(t('days')) + '</span></div>' +
      '<div class="row">' + label(TEXT.daily) + numBox('Keep.DailyDays') + '<span class="u">' + esc(t('days')) + '</span></div>' +
      '<div class="row">' + label(TEXT.perLoop) + toggle('Keep.PerLoop') + '</div>' +
      '<div class="row">' + label(TEXT.maxMb) + numBox('Keep.MaxTotalMB') + '<span class="u">' + esc(t('mb')) + '</span></div>' +
      '<div class="row">' + label(TEXT.binDays) + numBox('Keep.BinDays') + '<span class="u">' + esc(t('days')) + '</span></div>';
    var how = '<div class="row">' + label(TEXT.folder) + '</div>' + pathRow('Backups.Folder') +
      '<div class="row">' + label(TEXT.second) + '</div>' + pathRow('Backups.SecondFolder') +
      '<div class="row">' + label(TEXT.keyRow) + '<span class="keycap' + (isListening ? ' on' : '') + '" data-act="listen">' + esc(keyText()) + '</span>' +
      '<input class="keyin" data-focus="key" data-key="1" type="text" readonly></div>' +
      '<div class="row">' + label(TEXT.pics) + toggle('Backups.TakePictures') + '</div>' +
      '<div class="row">' + label(TEXT.verify) + toggle('Backups.Verify') + '</div>' +
      '<div class="row">' + label(TEXT.warnBehind) + toggle('Screen.WarnWhenBehind') + '</div>' +
      '<div class="row">' + label(TEXT.snapsRow) + toggle('Screen.ShowGameSnapshots') + '</div>' +
      '<div class="row">' + label(TEXT.replaceScreen) + toggle('Screen.ReplaceLoadScreen') + '</div>' +
      '<div class="row">' + label(TEXT.toasts) + toggle('Toasts.AutoBackupToasts') + '</div>';
    return '<div class="set"><div class="card"><h3>' + esc(t('setWhen')) + '</h3><p class="ex">' + esc(t('setWhenEx')) + '</p>' + trig + '</div>' +
      '<div><div class="card"><h3>' + esc(t('setKeep')) + '</h3><p class="ex">' + esc(t('setKeepEx')) + '</p>' + keep + '</div>' +
      '<div class="card"><h3>' + esc(t('setWhere')) + '</h3>' + how + '</div></div></div>';
  }

  // ---------- the bin ----------

  function left(days) {
    if (days < 0) return '';
    if (days === 0) return t('lastDay');
    return days === 1 ? t('dayLeft') : t('daysLeft', days);
  }

  function binPane() {
    var gone = view.deleted || [];
    var bin = view.bin || [];
    var html = '<div class="trash"><div class="card"><h3>' + esc(t('tabBin')) + '</h3><p class="ex" style="margin:0">' +
      esc(view.binDays > 0 ? t('binIntro', view.binDays) : t('binIntroKeep')) + '</p></div>';
    if (!gone.length && !bin.length) html += '<div class="empty" style="margin:6px 0">' + esc(t('binEmptyList')) + '</div>';
    if (gone.length) {
      html += '<div class="sec">' + esc(t('binSaves')) + '</div>';
      gone.forEach(function (run) {
        var newest = (run.backups || [])[0];
        var facts = [where(run), size(backupBytes(run)), t('subPoints', (run.backups || []).length), left(run.daysLeft)].filter(Boolean);
        html += '<div class="trow"><div class="th" style="' + bgUrl(runPic(run)) + '">' + portrait(run) + '</div>' +
          '<div class="m">' + esc(title(run)) + '<small>' + esc(facts.join(DOT)) + '</small><small>' + esc(t('goneRun')) + '</small></div>' +
          (newest ? '<button class="btn sm pri" data-act="bringback" data-stem="' + esc(run.stem) + '" data-id="' + esc(newest.id) + '">' + esc(t('putBack')) + '</button>' : '') +
          '<button class="btn sm bad" data-act="binrundel" data-stem="' + esc(run.stem) + '">' + esc(t('deleteNow')) + '</button></div>';
      });
    }
    if (bin.length) {
      html += '<div class="sec">' + esc(t('binPoints')) + '</div>';
      bin.forEach(function (entry) {
        var facts = [kindName(entry.kind), entry.at, size(entry.bytes), entry.why === 'thinned' ? t('thinned') : t('deletedWhy'), left(entry.daysLeft)].filter(Boolean);
        html += '<div class="trow"><div class="th" style="' + bgUrl(binShot(entry)) + '"></div>' +
          '<div class="m">' + esc((entry.name ? title(entry) + DOT : '') + where(entry)) + '<small>' + esc(facts.join(DOT)) + '</small></div>' +
          '<button class="btn sm pri" data-act="binback" data-stem="' + esc(entry.stem) + '" data-id="' + esc(entry.id) + '">' + esc(t('putBack')) + '</button>' +
          '<button class="btn sm bad" data-act="bindel" data-stem="' + esc(entry.stem) + '" data-id="' + esc(entry.id) + '">' + esc(t('deleteNow')) + '</button></div>';
      });
    }
    var total = (view.binBytes || 0);
    gone.forEach(function (run) { total += backupBytes(run); });
    if (bin.length || gone.length) {
      html += '<div style="margin-top:14px;display:flex;justify-content:flex-end"><button class="btn bad sm" data-act="emptybin">' + esc(t('emptyBin', size(total))) + '</button></div>';
    }
    return html + '</div>';
  }

  // ---------- dialogs ----------

  function frame(heading, body, foot, isWide) {
    return '<div class="modal" data-act="backdrop"><div class="mbox' + (isWide ? ' wide' : '') + '"><div class="mhd">' + esc(heading) +
      '<span class="sp"></span><button class="x" data-act="mclose">&times;</button></div>' +
      '<div class="mbd">' + body + '</div><div class="mft">' + foot + '</div></div></div>';
  }

  function btn(text, cls, act, attrs) {
    return '<button class="btn' + (cls ? ' ' + cls : '') + '" data-act="' + act + '"' + (attrs || '') + '>' + esc(text) + '</button>';
  }

  function selectedRun() {
    return selected ? findRun(selected) : null;
  }

  function currentPoint() {
    var run = selectedRun();
    if (!run) return null;
    var pts = points(run);
    for (var i = 0; i < pts.length; i++) if (pts[i].key === pointKey) return pts[i];
    return null;
  }

  function restoreModal(run, p) {
    var playing = isPlaying(run);
    var asNew = playing || modal.asNew !== false;
    var then = where(p);
    var now = where(run);
    var body = t('restoreBody', esc(then), esc(kindName(p.kind).toLowerCase()), esc(title(run))) +
      '<div class="choice"><div class="opt' + (asNew ? ' on' : '') + '" data-act="asnew" data-v="1"><b>' + esc(t('asNew')) + '</b><small>' + esc(t('asNewBody', then, now)) + '</small></div>' +
      '<div class="opt' + (!asNew ? ' on' : '') + (playing ? ' off' : '') + '" data-act="asnew" data-v="0"><b>' + esc(t('replace')) + '</b><small>' +
      esc(playing ? t('replaceBlocked') : t('replaceBody', then, now)) + '</small></div></div>' +
      (p.backup.oldVersion ? '<div class="ow">' + esc(t('oldWarn', p.backup.version, view.version)) + '</div>' : '');
    return frame(t('restoreTitle'), body, btn(t('cancel'), '', 'mclose') + btn(t('loadDay', t('day', p.day)), 'pri', 'dorestore'));
  }

  function itemDiff(thenItems, nowItems, names) {
    var ids = {};
    Object.keys(thenItems || {}).forEach(function (k) { ids[k] = true; });
    Object.keys(nowItems || {}).forEach(function (k) { ids[k] = true; });
    var changes = Object.keys(ids).map(function (id) {
      return { id: id, d: ((nowItems || {})[id] || 0) - ((thenItems || {})[id] || 0) };
    }).filter(function (c) { return c.d; }).sort(function (a, b) { return Math.abs(b.d) - Math.abs(a.d); }).slice(0, 40);
    if (!changes.length) return '<div class="when" style="margin-top:6px">' + esc(t('noItemChange')) + '</div>';
    return '<div class="itemdiff">' + changes.map(function (c) {
      var name = names && names[c.id] ? pick(names[c.id]) || names[c.id][0] : '#' + c.id;
      return '<span class="' + (c.d > 0 ? 'up' : 'dn') + '">' + (c.d > 0 ? '+ ' : '- ') + esc(name) + ' x' + Math.abs(c.d) + '</span>';
    }).join('') + '</div>';
  }

  function compareRows(thenP, thenStats, nowP, nowStats, thenHead, nowHead) {
    var rows = [['day', thenP.turn * 1000 + thenP.day, nowP.turn * 1000 + nowP.day]];
    ['health', 'fullness', 'mood', 'stamina', 'money', 'points', 'items'].forEach(function (k) {
      if (thenStats && nowStats && thenStats[k] != null && nowStats[k] != null) rows.push([k, statValue(thenStats, k), statValue(nowStats, k)]);
    });
    return '<table class="cmp"><tr><th>' + esc(t('what')) + '</th><th>' + esc(thenHead) + '</th><th>' + esc(nowHead) + '</th><th>' + esc(t('change')) + '</th></tr>' +
      rows.map(function (r) {
        var isDay = r[0] === 'day';
        var a = isDay ? thenP.day : Math.round(r[1]);
        var b = isDay ? nowP.day : Math.round(r[2]);
        var d = isDay ? (thenP.turn === nowP.turn ? b - a : 0) : b - a;
        var shownA = isDay && thenP.turn > 1 ? t('loopDay', thenP.turn, a) : a;
        var shownB = isDay && nowP.turn > 1 ? t('loopDay', nowP.turn, b) : b;
        return '<tr><td>' + esc(tf('st', r[0])) + '</td><td>' + esc(shownA) + '</td><td>' + esc(shownB) + '</td><td class="' + (d > 0 ? 'up' : d < 0 ? 'dn' : '') + '">' +
          esc(d ? (d > 0 ? '+' : '') + d : t('same')) + '</td></tr>';
      }).join('') + '</table>';
  }

  function compareModal(run, p) {
    var d = detailFor(run);
    var thenStats = statsOf(run, p);
    var nowStats = d ? d.now : null;
    var nowP = { day: run.day, turn: run.turn, clock: run.clock };
    var thenPic = p.backup ? shotOf(run.stem, p.backup) : '';
    var body = '<div class="vs"><div><div class="shot" style="' + bgUrl(thenPic) + '">' + (thenPic ? '' : esc(t('noPicture'))) + '</div><div class="who">' + esc(where(p)) + '<small>' + esc(kindName(p.kind)) + '</small></div></div>' +
      '<div><div class="shot" style="' + bgUrl(runPic(run)) + '"></div><div class="who">' + esc(t('nowLabel', where(run))) + '<small>' + esc(title(run)) + '</small></div></div></div>';
    body += nowStats ? compareRows(p, thenStats, nowP, nowStats, t('then'), t('nowCol')) : '<div class="when" style="margin-top:10px">' + esc(t('loading')) + '</div>';
    if (d && p.backup && d.items && d.items[p.backup.id]) {
      body += '<div class="ihd">' + esc(t('itemsChanged')) + '</div>' + itemDiff(d.items[p.backup.id], d.nowItems, d.names);
    }
    var foot = btn(t('close'), '', 'mclose');
    if (p.backup) foot += btn(t('loadDay', t('day', p.day)), 'pri', 'restore');
    else if (p.snap) foot += btn(t('loadAsNew'), 'pri', 'snapnew');
    return frame(t('compareTitle'), body, foot, true);
  }

  function cloudModal(run) {
    var b = backupById(run, run.behind.id);
    if (!b) return '';
    var d = detailFor(run);
    var body = t('cloudBody', esc(title(run))) +
      '<div class="vs" style="margin-top:14px"><div><div class="shot" style="' + bgUrl(runPic(run)) + '"></div><div class="who">' + esc(t('onDisk', where(run))) +
      '<small>' + esc(run.lastPlayed ? t('onDiskAt', run.lastPlayed) : '') + '</small></div></div>' +
      '<div><div class="shot" style="' + bgUrl(shotOf(run.stem, b)) + '">' + (b.shot ? '' : esc(t('noPicture'))) + '</div><div class="who">' + esc(t('newestBackup', where(b))) +
      '<small>' + esc(t('newestAt', b.at) + DOT + kindName(b.kind)) + '</small></div></div></div>' +
      compareRows({ day: run.day, turn: run.turn }, d ? d.now : null, b, b.stats, t('onDisk', '').replace(/[:：]\s*$/, ''), t('newestBackup', '').replace(/[:：]\s*$/, '')) +
      '<div class="mh">' + esc(t('cloudFoot')) + '</div>';
    return frame(t('cloudTitle'), body, btn(t('keepDisk', t('day', run.day)), '', 'dismiss') + btn(t('useBackup', t('day', b.day)), 'pri', 'usebackup'), true);
  }

  function exportModal(run) {
    var all = !!modal.all;
    var extras = modal.extras !== false;
    var count = (run.backups || []).length;
    var body = t('exportBody', esc(title(run))) +
      '<div class="mrow"><span class="lb">' + esc(t('exportSave')) + '</span><span class="tg on" style="opacity:.5;cursor:default"></span></div>' +
      (count ? '<div class="mrow"><span class="lb">' + esc(t('exportAll', count)) + '<small>' + esc(t('exportAllSize', size((run.saveBytes || 0) + backupBytes(run)), size(run.saveBytes || 0))) + '</small></span>' +
        '<span class="tg' + (all ? ' on' : '') + '" data-act="exall"></span></div>' : '') +
      '<div class="mrow"><span class="lb">' + esc(t('exportExtras')) + '</span><span class="tg' + (extras ? ' on' : '') + '" data-act="exextras"></span></div>' +
      '<div class="mrow"><span class="lb">' + esc(t('savedTo')) + '<small>' + esc((view.folder || '') + '/Exports') + '</small></span>' + btn(t('openFolder'), 'sm', 'openexports') + '</div>';
    return frame(t('exportTitle'), body, btn(t('cancel'), '', 'mclose') + btn(t('exportBtn'), 'pri', 'doexport'));
  }

  function importModal() {
    var body = t('importBody') + '<div class="mrow"><span class="lb"><small>' + esc((imports && imports.folder) || (view.folder || '') + '/Import') + '</small></span>' +
      btn(t('openFolder'), 'sm', 'openimport') + btn(t('refresh'), 'sm', 'importlist') + '</div>';
    if (!imports) {
      body += '<div class="mh">' + esc(t('importLoading')) + '</div>';
    } else if (!(imports.files || []).length) {
      body += '<div class="mh">' + esc(t('importNone')) + '</div>';
    } else {
      body += imports.files.map(function (f) {
        var facts = [];
        if (f.loose) facts.push(t('importLoose'));
        else if (!f.readable) facts.push(t('importBad'));
        else {
          if (f.name) facts.push(title(f));
          facts.push(where(f));
          if (f.points) facts.push(t('importPoints', f.points));
          if (f.version) facts.push(t('importNewer', f.version));
        }
        facts.push(size(f.bytes));
        facts.push(f.at);
        return '<div class="ifile' + (modal.file === f.file ? ' on' : '') + (f.readable ? '' : ' bad') + '"' + (f.readable ? ' data-act="ipick" data-file="' + esc(f.file) + '"' : '') + '>' +
          esc(f.file) + '<small>' + esc(facts.join(DOT)) + '</small></div>';
      }).join('');
    }
    body += '<div class="mh">' + esc(t('importFoot')) + '</div>';
    return frame(t('importTitle'), body, btn(t('cancel'), '', 'mclose') + '<button class="btn pri" data-act="doimport"' + (modal.file ? '' : ' disabled') + '>' + esc(t('importBtn')) + '</button>', true);
  }

  function confirmModal() {
    return frame(modal.title, modal.body, btn(t('cancel'), '', 'mclose') + btn(modal.label, modal.cls, 'confirm'));
  }

  function textModal() {
    var value = drafts.modal != null ? drafts.modal : modal.value || '';
    var body = (modal.hint ? '<div class="mh" style="margin-top:0">' + esc(modal.hint) + '</div>' : '') +
      '<input class="txt" type="text" maxlength="' + (modal.max || 200) + '" spellcheck="false" data-focus="modal" data-modal="1" value="' + esc(value) + '">';
    return frame(modal.title, body, btn(t('cancel'), '', 'mclose') + btn(t('save'), 'pri', 'savetext'));
  }

  function modalHtml() {
    if (!modal) return '';
    var run = selectedRun();
    var p = currentPoint();
    switch (modal.kind) {
      case 'restore': return run && p && p.backup ? restoreModal(run, p) : '';
      case 'compare': return run && p ? compareModal(run, p) : '';
      case 'cloud': return run && run.behind ? cloudModal(run) : '';
      case 'export': return run ? exportModal(run) : '';
      case 'import': return importModal();
      case 'confirm': return confirmModal();
      case 'text': return textModal();
    }
    return '';
  }

  // ---------- the whole screen ----------

  function header() {
    var runs = view.runs || [];
    var pts = 0;
    runs.forEach(function (r) { pts += (r.backups || []).length; });
    var binCount = (view.bin || []).length + (view.deleted || []).length;
    var tabs = [['saves', t('tabSaves'), runs.length], ['settings', t('tabSettings'), ''], ['bin', t('tabBin'), binCount]];
    var sub = [t('subRuns', runs.length), t('subPoints', pts), size((view.bytes || 0) + (view.saveBytes || 0))].join(DOT);
    return '<div class="hd"><h1>' + esc(view.inRun ? t('titleRun') : t('title')) + '</h1><div class="sub">' + esc(sub) +
      (view.inRun ? '<br><span class="ir">' + esc(t('inRunNote')) + '</span>' : '') + '</div><span class="sp"></span>' +
      '<div class="tabs">' + tabs.map(function (x) {
        return '<button class="tab' + (tab === x[0] ? ' on' : '') + '" data-tab="' + x[0] + '">' + esc(x[1]) + (x[2] !== '' ? '<span class="n">' + x[2] + '</span>' : '') + '</button>';
      }).join('') + '</div><button class="x" data-act="close">&times;</button></div>';
  }

  function ensureSelected() {
    var runs = shownRuns();
    if (selected && findRun(selected)) return findRun(selected);
    selected = view.inRun && findRun(view.inRun) ? view.inRun : runs.length ? runs[0].stem : (view.runs || []).length ? view.runs[0].stem : null;
    pointKey = null;
    return selected ? findRun(selected) : null;
  }

  // The detail (stats now, item counts, the game's snapshots, ending photos) is read again when the selected run or
  // its newest backup changes.
  function askDetail(run) {
    if (!run || tab !== 'saves') return;
    var newest = (run.backups || [])[0];
    var key = run.stem + '|' + (newest ? newest.id : '') + '|' + (run.backups || []).length + '|' + run.turn + '|' + run.day + '|' + run.clock;
    if (key === detailKey) return;
    detailKey = key;
    post(['detail', run.stem]);
  }

  function fontFaces() {
    var res = webRoot() + 'Res/GameResult/';
    return '@font-face{font-family:"SLSB Sans";src:url("' + res + 'SourceHanSansSC-Regular-2.otf");font-weight:400 600}' +
      '@font-face{font-family:"SLSB Sans";src:url("' + res + 'SourceHanSansSC-Heavy-2.otf");font-weight:700 900}';
  }

  function ensureStyle() {
    if (document.getElementById(ROOT_ID + '-css')) return;
    var style = document.createElement('style');
    style.id = ROOT_ID + '-css';
    style.textContent = fontFaces() + CSS;
    (document.head || document.documentElement).appendChild(style);
  }

  var SCROLLERS = ['.list', '.pts', '.pv', '.set', '.trash', '.mbd'];

  function ensureRoot() {
    var root = document.getElementById(ROOT_ID);
    if (root) return root;
    root = document.createElement('div');
    root.id = ROOT_ID;
    root.addEventListener('click', onClick);
    root.addEventListener('input', onInput);
    root.addEventListener('change', onChange);
    root.addEventListener('keydown', onKey);
    root.addEventListener('focusin', function (e) { if (isTextBox(e.target)) typing(true); });
    root.addEventListener('focusout', function () {
      setTimeout(function () {
        if (!isTextBox(document.activeElement)) {
          typing(false);
          if (isListening) { isListening = false; draw(); }
        }
      }, 0);
    });
    (document.body || document.documentElement).appendChild(root);
    return root;
  }

  function draw() {
    if (!view) return;
    ensureStyle();
    var root = ensureRoot();
    var places = SCROLLERS.map(function (sel) { var n = root.querySelector(sel); return n ? n.scrollTop : 0; });
    var active = document.activeElement;
    var focusKey = active && root.contains(active) && active.getAttribute ? active.getAttribute('data-focus') : null;
    var caret = null;
    try { if (focusKey) caret = [active.selectionStart, active.selectionEnd]; } catch (e) {}

    var run = tab === 'saves' ? ensureSelected() : selectedRun();
    var body = tab === 'settings' ? settingsPane() : tab === 'bin' ? binPane() : runsPane() + detailPane(run);
    root.innerHTML = '<div class="slsb' + (isBusy ? ' busy' : '') + '"><div class="bg" style="' + bgUrl(run ? runPic(run) : '') + '"></div><div class="vign"></div>' +
      '<div class="scr">' + header() + '<div class="body">' + body + '</div></div>' + modalHtml() + '</div>';

    SCROLLERS.forEach(function (sel, i) { var n = root.querySelector(sel); if (n) n.scrollTop = places[i]; });
    var target = isListening ? root.querySelector('[data-key]') : focusKey ? root.querySelector('[data-focus="' + focusKey + '"]') : modal && modal.kind === 'text' ? root.querySelector('[data-modal]') : null;
    if (target) {
      target.focus();
      try {
        if (caret && caret[0] != null) target.setSelectionRange(caret[0], caret[1]);
        else if (target.value) target.setSelectionRange(target.value.length, target.value.length);
      } catch (e) {}
    }
    if (!isTextBox(document.activeElement)) typing(false);
    uiSize(root, { fit: root.querySelector('.slsb') });
    if (tab === 'saves') askDetail(run);
  }

  // Only the list is drawn again while the search box is typed in, so the box keeps its focus and caret.
  function drawList() {
    var list = document.querySelector('#' + ROOT_ID + ' .list');
    if (list) list.innerHTML = listHtml();
  }

  // ---------- keyboard and typing ----------

  // The game sends keys to the page only while the plugin asks it to, so a text box tells the plugin when it has
  // focus. A redraw can drop a focused box without a blur, so the check waits a tick and looks at what has focus.
  function isTextBox(node) {
    return !!node && (node.tagName === 'INPUT' || node.tagName === 'TEXTAREA') && !!node.closest && !!node.closest('#' + ROOT_ID);
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
    if (isListening && node.getAttribute('data-key')) {
      e.preventDefault();
      if (e.key === 'Escape') { pageEsc(e); isListening = false; node.blur(); draw(); return; }
      var name = e.key === 'Backspace' || e.key === 'Delete' ? 'None' : keyCode(e);
      if (!name) return;
      isListening = false;
      node.blur();
      step(['set', 'Controls.BackupKey', encodeURIComponent(name)]);
      draw();
      return;
    }
    if (e.key === 'Escape' && isTextBox(node)) {
      pageEsc(e);
      if (node.getAttribute('data-modal')) { closeModal(); return; }
      if (node.getAttribute('data-in') === 'query' && query) { query = node.value = ''; drawList(); return; }
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

  function onInput(e) {
    var node = e.target;
    if (node.getAttribute('data-in') === 'query') { query = node.value; drawList(); return; }
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
    if (id) {
      delete drafts['set:' + id];
      if (node.value.trim() !== setting(id)) step(['set', id, encodeURIComponent(node.value.trim())]);
    }
  }

  function commitPath(id) {
    var node = document.querySelector('#' + ROOT_ID + ' [data-path="' + id + '"]');
    if (!node) return;
    delete drafts['set:' + id];
    var value = node.value.trim();
    node.blur();
    if (value !== (setting(id) || '')) step(['set', id, encodeURIComponent(value)]);
  }

  // ---------- clicks ----------

  function closeModal() {
    modal = null;
    delete drafts.modal;
    draw();
  }

  function openModal(next) {
    modal = next;
    delete drafts.modal;
    draw();
  }

  function confirmStep(heading, bodyHtml, labelText, cls, parts) {
    openModal({ kind: 'confirm', title: heading, body: bodyHtml, label: labelText, cls: cls, parts: parts });
  }

  function sendAndClose(parts) {
    modal = null;
    delete drafts.modal;
    step(parts);
    draw();
  }

  var ACT = {
    close: function () { post(['close']); },
    backdrop: function (node, e) { if (e.target === node) closeModal(); },
    mclose: closeModal,
    newgame: function () { step(['newgame']); },
    gamelist: function () { step(['gamelist']); },
    folder: function () { post(['folder']); },
    runfolder: function () { post(['folder', selected]); },
    openexports: function () { post(['folder', 'Exports']); },
    openimport: function () { post(['folder', 'Import']); },
    sort: function () { sortBy = SORTS[(SORTS.indexOf(sortBy) + 1) % SORTS.length]; draw(); },
    load: function () { var run = selectedRun(); if (run) step(['load', run.file]); },
    backup: function () { if (selected) step(['backup', selected]); },
    duplicate: function () { if (selected) step(['duplicate', selected]); },
    runpin: function () { var run = selectedRun(); if (run) step(['runpin', run.stem, run.pinned ? '0' : '1']); },
    rename: function () {
      var run = selectedRun();
      if (run) openModal({ kind: 'text', title: t('renameTitle'), value: title(run), max: 60, parts: ['rename', run.stem], isRename: true });
    },
    delrun: function () {
      var run = selectedRun();
      if (!run) return;
      var n = (run.backups || []).length;
      confirmStep(t('deleteTitle'), view.binDays > 0 ? t('deleteBody', esc(title(run)), n, view.binDays) : t('deleteBodyKeep', esc(title(run)), n), t('del'), 'bad', ['delete', run.stem]);
    },
    export: function () { openModal({ kind: 'export', all: false, extras: true }); },
    exall: function () { modal.all = !modal.all; draw(); },
    exextras: function () { modal.extras = modal.extras === false; draw(); },
    doexport: function () { sendAndClose(['exportrun', selected, modal.all ? '1' : '0', modal.extras !== false ? '1' : '0']); },
    import: function () { imports = null; openModal({ kind: 'import', file: null }); post(['importlist']); },
    importlist: function () { imports = null; modal.file = null; draw(); post(['importlist']); },
    ipick: function (node) { modal.file = node.getAttribute('data-file'); draw(); },
    doimport: function () { if (modal.file) sendAndClose(['importfile', modal.file]); },
    cloud: function () { openModal({ kind: 'cloud' }); },
    dismiss: function () { if (selected) sendAndClose(['dismiss', selected]); },
    usebackup: function () { var run = selectedRun(); if (run && run.behind) sendAndClose(['restore', run.stem, run.behind.id]); },
    snaps: function () { showSnaps = !showSnaps; draw(); },
    restore: function () { var p = currentPoint(); if (p && p.backup) openModal({ kind: 'restore', asNew: true }); },
    asnew: function (node) {
      var run = selectedRun();
      if (node.getAttribute('data-v') === '0' && run && isPlaying(run)) return;
      modal.asNew = node.getAttribute('data-v') === '1';
      draw();
    },
    dorestore: function () {
      var run = selectedRun();
      var p = currentPoint();
      if (!run || !p || !p.backup) return;
      var asNew = isPlaying(run) || modal.asNew !== false;
      sendAndClose([asNew ? 'copy' : 'restore', run.stem, p.backup.id]);
    },
    compare: function () { openModal({ kind: 'compare' }); },
    keep: function () { var p = currentPoint(); if (p && p.backup) step(['pin', selected, p.backup.id, p.backup.pinned ? '0' : '1']); },
    ptnote: function () {
      var p = currentPoint();
      if (p && p.backup) openModal({ kind: 'text', title: t('noteTitle'), hint: t('noteHint'), value: p.backup.note || '', max: 200, parts: ['note', selected, p.backup.id] });
    },
    ptexport: function () { var p = currentPoint(); if (p && p.backup) step(['export', selected, p.backup.id]); },
    delpt: function () {
      var p = currentPoint();
      if (p && p.backup) confirmStep(t('removeTitle'), esc(t('removeBody', where(p), kindName(p.kind))), t('del'), 'bad', ['remove', selected, p.backup.id]);
    },
    snapnew: function () { var p = currentPoint(); if (p && p.snap) sendAndClose(['snapnew', selected, p.snap.file]); },
    savetext: function () {
      if (!modal || modal.kind !== 'text') return;
      var node = document.querySelector('#' + ROOT_ID + ' [data-modal]');
      var value = (node ? node.value : drafts.modal || '').trim();
      if (modal.isRename && !value) return;
      sendAndClose(modal.parts.concat([encodeURIComponent(value)]));
    },
    confirm: function () { if (modal && modal.parts) sendAndClose(modal.parts); },
    tg: function (node) { var id = node.getAttribute('data-id'); step(['set', id, setting(id) === 'true' ? 'false' : 'true']); },
    setpath: function (node) { commitPath(node.getAttribute('data-id')); },
    listen: function () {
      isListening = true;
      draw();
    },
    bringback: function (node) { step(['bringback', node.getAttribute('data-stem'), node.getAttribute('data-id')]); },
    binrundel: function (node) {
      var stem = node.getAttribute('data-stem');
      var run = null;
      (view.deleted || []).forEach(function (r) { if (r.stem === stem) run = r; });
      confirmStep(t('deleteTitle'), esc(t('emptyBody', run ? title(run) : stem)), t('deleteNow'), 'bad', ['binrundel', stem]);
    },
    binback: function (node) { step(['binback', node.getAttribute('data-stem'), node.getAttribute('data-id')]); },
    bindel: function (node) { step(['bindel', node.getAttribute('data-stem'), node.getAttribute('data-id')]); },
    emptybin: function () {
      var total = view.binBytes || 0;
      (view.deleted || []).forEach(function (run) { total += backupBytes(run); });
      confirmStep(t('emptyTitle'), esc(t('emptyBody', size(total))), t('deleteNow'), 'bad', ['binempty']);
    }
  };

  function onClick(e) {
    if (isBusy) return;
    var node = e.target.closest('[data-act],[data-run],[data-pt],[data-tab],[data-filter]');
    if (!node) return;
    if (node.hasAttribute('data-act')) {
      var act = ACT[node.getAttribute('data-act')];
      if (act) act(node, e);
      return;
    }
    if (node.hasAttribute('data-run')) { selected = node.getAttribute('data-run'); pointKey = null; draw(); return; }
    if (node.hasAttribute('data-pt')) { pointKey = node.getAttribute('data-pt'); draw(); return; }
    if (node.hasAttribute('data-tab')) { tab = node.getAttribute('data-tab'); modal = null; isListening = false; draw(); return; }
    if (node.hasAttribute('data-filter')) { filter = node.getAttribute('data-filter'); draw(); }
  }

  // ---------- the pause menu ----------

  // Two buttons in the game's pause menu (OutSetting) while a run is open, above Save & Exit and anything another mod
  // puts there: the save manager for this run, and a backup right now. Vue re-renders the menu list whenever it likes
  // and drops foreign nodes, so they are put back on every poll and on every change to the menu.
  var PAUSE_CSS = '#slsb-pm-open,#slsb-pm-now{border-left-color:rgba(143,199,191,.45)}';

  function pauseButton(doc, id, text, verb) {
    var node = doc.getElementById(id);
    if (!node) {
      node = doc.createElement('div');
      node.id = id;
      node.className = 'main-btn';
      node.addEventListener('click', function () { post([verb]); });
    }
    if (node.textContent !== text) node.textContent = text;
    return node;
  }

  function menuIsChinese(list) {
    var exit = list.querySelector(':scope > .btn-exit');
    return !!exit && /[一-鿿]/.test(exit.textContent || '');
  }

  function placePauseButtons(doc) {
    var list = doc.querySelector('.menu-list');
    var exit = list && list.querySelector(':scope > .btn-exit');
    var open = doc.getElementById('slsb-pm-open');
    var now = doc.getElementById('slsb-pm-now');
    if (!exit) {
      if (open) open.remove();
      if (now) now.remove();
      return;
    }
    if (!doc.getElementById('slsb-pm-style')) {
      var style = doc.createElement('style');
      style.id = 'slsb-pm-style';
      style.textContent = PAUSE_CSS;
      doc.head.appendChild(style);
    }
    var lang = menuIsChinese(list) ? 1 : 0;
    var anchor = list.querySelector(':scope > #slnlp-end') || list.querySelector(':scope > .btn-restart') || exit;
    open = pauseButton(doc, 'slsb-pm-open', TEXT.pauseOpen[lang], 'openinrun');
    now = pauseButton(doc, 'slsb-pm-now', TEXT.pauseNow[lang], 'backupnow');
    if (now.nextElementSibling !== anchor) list.insertBefore(now, anchor);
    if (open.nextElementSibling !== now) list.insertBefore(open, now);
  }

  function patchPauseMenu() {
    var frames = document.querySelectorAll('iframe');
    for (var i = 0; i < frames.length; i++) {
      if (!/OutSetting\.html/.test(frames[i].src || '')) continue;
      var doc = null;
      try { doc = frames[i].contentDocument; } catch (e) {}
      if (!doc || !doc.body || !doc.head) continue;
      if (!doc.slsbWatched && doc.defaultView && typeof doc.defaultView.MutationObserver !== 'undefined') {
        doc.slsbWatched = true;
        new doc.defaultView.MutationObserver(patchSafely).observe(doc.body, { childList: true, subtree: true });
      }
      placePauseButtons(doc);
    }
  }

  function patchSafely() {
    try { patchPauseMenu(); } catch (e) {}
  }

  setInterval(patchSafely, POLL_MS);

  // ---------- the plugin's side ----------

  function parse(json) {
    try { return JSON.parse(json); } catch (e) { return null; }
  }

  function remove() {
    var root = document.getElementById(ROOT_ID);
    if (root) root.parentNode.removeChild(root);
  }

  window.__slsavebackups = {
    open: function (json) {
      var next = parse(json);
      if (!next) return;
      view = next;
      tab = 'saves';
      modal = null;
      detail = null;
      detailKey = '';
      imports = null;
      isBusy = false;
      isTyping = false;
      isListening = false;
      drafts = {};
      selected = view.inRun || null;
      pointKey = null;
      showSnaps = setting('Screen.ShowGameSnapshots') !== 'false';
      draw();
    },
    show: function (json) {
      var next = parse(json);
      if (!next || !document.getElementById(ROOT_ID)) return;
      view = next;
      isBusy = false;
      if (setting('Screen.ShowGameSnapshots') === 'false') showSnaps = false;
      draw();
    },
    detail: function (json) {
      var next = parse(json);
      if (!next || !view) return;
      detail = next;
      if (next.stem === selected && tab === 'saves') draw();
    },
    imports: function (json) {
      var next = parse(json);
      if (!next || !view) return;
      imports = next;
      if (modal && modal.kind === 'import') draw();
    },
    close: function () {
      view = null;
      modal = null;
      isBusy = false;
      isTyping = false;
      isListening = false;
      remove();
    },
    escape: function () {
      // The same Esc press the page already used in a text box.
      if (isBusy || Date.now() - pageEscAt < 250) return;
      if (isTextBox(document.activeElement)) {
        document.activeElement.blur();
        return;
      }
      if (modal) {
        closeModal();
        return;
      }
      post(['close']);
    },
    done: function () {
      isBusy = false;
      var box = document.querySelector('#' + ROOT_ID + ' .slsb');
      if (box) box.classList.remove('busy');
    }
  };

  post(['ready']);
})();
