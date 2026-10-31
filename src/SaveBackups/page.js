// Save Backups' web half. Runs in Root.html, the document every game page iframe sits in, and draws the save manager
// over the title screen when the plugin opens it. Talks to the plugin through vuplex.postMessage("slmod|savebackups|<verb>")
// and is driven by window.__slsavebackups. Every step that changes a save waits for the plugin: the page takes no
// clicks until the plugin sends the new view (show) or says the step is over (done).
(function () {
  if (window.__slsavebackups) return;

  var PREFIX = 'slmod|savebackups|';
  var ROOT_ID = 'slsb-root';

  var TEXT = {
    title: ['Saves', '存档'],
    sub: ['Backups are kept outside Steam Cloud', '备份保存在 Steam 云同步之外'],
    saveCount: ['{0} saves', '{0} 个存档'],
    saveOne: ['1 save', '1 个存档'],
    backupCount: ['{0} backups', '{0} 个备份'],
    backupOne: ['1 backup', '1 个备份'],
    noBackupsShort: ['No backups yet', '还没有备份'],
    lastAt: ['last {0}', '最近 {0}'],
    noRuns: ['No saves yet.', '还没有存档。'],
    deleted: ['Deleted saves', '已删除的存档'],
    gameList: ['Default list', '默认列表'],
    folder: ['Open folder', '打开文件夹'],
    used: ['Backups use {0}', '备份共占用 {0}'],
    usedOf: ['Backups use {0} of the {1} limit', '备份共占用 {0}，上限 {1}'],
    load: ['Load', '读取'],
    backUp: ['Back up now', '立即备份'],
    del: ['Delete save', '删除存档'],
    restore: ['Restore', '恢复'],
    bringBack: ['Restore', '恢复'],
    remove: ['Remove', '移除'],
    pin: ['Pin', '固定'],
    unpin: ['Unpin', '取消固定'],
    pinned: ['Pinned', '已固定'],
    latest: ['Latest', '最新'],
    cancel: ['Cancel', '取消'],
    dismiss: ['Dismiss', '忽略'],
    backups: ['Backups', '备份'],
    noBackups: ['No backups of this save yet. One is made each morning, when you quit, before a rebirth, an expedition or a zombie wave, and at an ending{0}.', '这个存档还没有备份。每天早上、退出时、重生前、探索前、尸潮前和到达结局时都会自动备份{0}。'],
    keyHint: [', or press {0} in the run', '，也可以在游戏中按 {0}'],
    loop: ['Loop {0}', '第 {0} 轮'],
    day: ['Day {0}', '第 {0} 天'],
    loopDay: ['loop {0}, day {1}', '第 {0} 轮第 {1} 天'],
    loopDayTitle: ['Loop {0}, day {1}', '第 {0} 轮第 {1} 天'],
    progress: ['Progress', '进度'],
    role: ['Survivor', '角色'],
    difficulty: ['Difficulty', '难度'],
    status: ['Status', '状态'],
    started: ['Started', '开始于'],
    lastBackup: ['Last backup', '最近备份'],
    backupsHeld: ['Backups', '备份数'],
    backupSize: ['Backup size', '备份大小'],
    behind: ['Behind its backup', '比备份旧'],
    behindBody: ['This save is at {0}, but its backup from {1} is at {2}. Steam Cloud or a crash may have put back an older copy. Restore that backup to get your progress back, or dismiss this if you rolled it back on purpose.', '这个存档在{0}，但 {1} 的备份在{2}。可能是 Steam 云同步或崩溃换回了旧存档。恢复这个备份即可找回进度；如果是你自己回退的，点「忽略」即可。'],
    states: {
      live: ['In progress', '进行中'],
      end: ['Finished', '已结束'],
      endless: ['Endless', '无尽'],
      warn: ['Behind its backup', '比备份旧'],
      gone: ['Deleted', '已删除']
    },
    kinds: {
      morning: ['Morning', '早上'],
      quit: ['Quit', '退出'],
      rebirth: ['Before rebirth', '重生前'],
      manual: ['Manual', '手动'],
      safety: ['Before a change', '改动前'],
      expedition: ['Before expedition', '探索前'],
      wave: ['Before zombie wave', '尸潮前'],
      ending: ['Ending', '结局'],
      imported: ['Imported', '导入']
    },
    search: ['Search saves, survivors, notes', '搜索存档、角色、备注'],
    filters: {
      all: ['All', '全部'],
      active: ['In progress', '进行中'],
      finished: ['Finished', '已结束'],
      backed: ['Has backups', '有备份']
    },
    noMatch: ['No saves match.', '没有符合条件的存档。'],
    importBtn: ['Import', '导入'],
    copy: ['Copy as new save', '复制为新存档'],
    note: ['Note', '备注'],
    exportBtn: ['Export', '导出'],
    save: ['Save', '保存'],
    noteTitle: ['Note on this backup', '这个备份的备注'],
    noteHint: ['A few words to tell this backup apart. Leave it empty to remove the note.', '写几个字方便区分这个备份。留空即删除备注。'],
    oldVersion: ['Older game version', '旧版本游戏'],
    oldVersionTip: ['Made on game version {0}, you are playing {1}.', '在游戏版本 {0} 时创建，当前版本为 {1}。'],
    oldVersionWarn: ['This backup was made on game version {0}, and you are playing {1}. It may not load as it did.', '这个备份是在游戏版本 {0} 时创建的，当前版本为 {1}，读取时可能和原来不一样。'],
    copyTitle: ['Copy as a new save?', '复制为新存档？'],
    copyBody: ['A new save is made from this backup: {0} at {1}, as it was on {2}. It shows in your saves as its own run, and {0} itself is not changed.', '将用这个备份创建一个新存档：「{0}」{1}（{2} 时的状态）。它会作为独立的存档出现在列表中，「{0}」本身不会改变。'],
    restoreTitle: ['Restore this backup?', '恢复这个备份？'],
    restoreBody: ['{0} goes back to {1}, as it was on {2}. What it is now is backed up first, so this can be undone.', '「{0}」将回到{1}（{2} 时的状态）。当前状态会先备份，可以撤销。'],
    restoreHint: ['Restore puts the save back to this point. What it is now is backed up first, so it can be undone.', '「恢复」会让存档回到这个时间点。当前状态会先备份，可以撤销。'],
    bringBackTitle: ['Bring this save back?', '恢复这个存档？'],
    bringBackBody: ['{0} goes back into your saves at {1}, as it was on {2}.', '「{0}」将以{1}（{2} 时的状态）回到存档列表。'],
    goneHint: ['This save was deleted. Pick a backup below and press Restore to put it back in your saves.', '这个存档已删除。在下面选一个备份并点「恢复」，即可把它放回存档列表。'],
    deleteTitle: ['Delete this save?', '删除这个存档？'],
    deleteBody: ['{0} is removed from your saves. Its backups stay, under Deleted saves, and it can be brought back from there.', '「{0}」将从存档列表中删除。它的备份会保留在「已删除的存档」中，可以从那里恢复。'],
    removeTitle: ['Remove this backup?', '移除这个备份？'],
    removeBody: ['The backup from {0} ({1}) is deleted for good.', '{0} 的备份（{1}）将被永久删除。'],
    safetyNote: ['made before a restore or a delete', '恢复或删除前自动创建']
  };

  var CSS = [
    '#' + ROOT_ID + '{position:fixed;inset:0;z-index:2147483000;background:rgba(0,0,0,.78);display:flex;align-items:center;justify-content:center;color:#ddd}',
    '#' + ROOT_ID + ' *{box-sizing:border-box}',
    '.slsb{--amber:#EBA308;--gold:#FFC83A;--teal:#8fc7bf;--red:#e06a5a;--dim:#8a7f6c;--line:rgba(220,188,120,.22);--panel:rgba(20,17,13,.92);--edge:#3a342a;position:relative;width:1500px;height:820px;display:flex;flex-direction:column;padding:22px 30px 20px;font-family:"SLSB Sans","Microsoft YaHei","Segoe UI",sans-serif;background:linear-gradient(180deg,#15120e 0%,#0d0b09 100%);border:1px solid #4a4030;box-shadow:0 0 0 1px #000,0 24px 70px rgba(0,0,0,.85),inset 0 1px 0 rgba(255,200,58,.12)}',
    '.slsb button{font-family:inherit}',
    '.slsb.busy{cursor:progress}.slsb.busy button{pointer-events:none;opacity:.6}',
    '.slsb ::-webkit-scrollbar{width:8px}.slsb ::-webkit-scrollbar-track{background:transparent}.slsb ::-webkit-scrollbar-button{display:none}',
    '.slsb ::-webkit-scrollbar-thumb{background:rgba(235,163,8,.28);border-radius:4px}.slsb ::-webkit-scrollbar-thumb:hover{background:rgba(235,163,8,.5)}',
    '.slsb .sep>*+*::before{content:"\\00b7";margin:0 8px;color:#5a5040}',
    '.slsb .sp{flex:1}',
    '.slsb-hd{display:flex;align-items:flex-end;gap:22px;padding-bottom:14px;border-bottom:1px solid var(--line)}',
    '.slsb-hd h1{margin:0;font-weight:800;font-size:34px;line-height:1;color:#fff;letter-spacing:5px;text-transform:uppercase}',
    '.slsb-hd .sub{color:var(--dim);font-size:14px;padding-bottom:3px;display:flex}',
    '.slsb .x{width:38px;height:38px;border:1px solid #555;background:rgba(0,0,0,.4);color:#ccc;font-size:18px;cursor:pointer;line-height:1}',
    '.slsb .x:hover{border-color:var(--gold);color:#fff}',
    '.slsb-body{flex:1;display:flex;gap:18px;min-height:0;padding-top:16px}',
    '.slsb .btn{background:rgba(0,0,0,.45);color:#ddd;border:1px solid #5a5040;padding:8px 16px;font-size:15px;cursor:pointer;letter-spacing:1px;line-height:1.2}',
    '.slsb .btn:hover{border-color:var(--gold);color:#fff}',
    '.slsb .btn.pri{background:rgba(235,163,8,.2);border-color:var(--amber);color:var(--gold);font-weight:800}',
    '.slsb .btn.pri:hover{background:rgba(235,163,8,.35);color:#fff}',
    '.slsb .btn.bad{border-color:rgba(224,106,90,.6);color:#f0a094}',
    '.slsb .btn.bad:hover{border-color:var(--red);color:#fff}',
    '.slsb .btn.sm{padding:5px 11px;font-size:13px}',
    '.slsb .badge{display:inline-block;font-size:11px;padding:1px 7px;border:1px solid;letter-spacing:1px;margin-right:6px;vertical-align:1px;white-space:nowrap;text-transform:uppercase;font-weight:400}',
    '.slsb .b-live{color:var(--teal);border-color:rgba(143,199,191,.5)}',
    '.slsb .b-end{color:var(--gold);border-color:rgba(255,200,58,.5)}',
    '.slsb .b-endless{color:#c9a4ff;border-color:rgba(201,164,255,.5)}',
    '.slsb .b-warn{color:var(--red);border-color:rgba(224,106,90,.6)}',
    '.slsb .b-gone{color:#aaa;border-color:#666}',
    '.slsb .b-old{color:#e0a060;border-color:rgba(224,160,96,.6)}',
    '.slsb .b-pin{color:var(--gold);border-color:rgba(255,200,58,.5)}',
    '.slsb .av{width:44px;height:44px;border-radius:50%;flex:none;display:flex;align-items:center;justify-content:center;font-weight:800;font-size:19px;color:#14110d;border:2px solid rgba(0,0,0,.6);box-shadow:0 0 0 1px rgba(255,255,255,.08)}',
    '.slsb .av.big{width:64px;height:64px;font-size:28px}',
    '.slsb .av.gone{filter:grayscale(1) brightness(.7)}',
    '.slsb .dot{display:inline-block;width:9px;height:9px;border-radius:50%;margin-right:5px;flex:none}',
    /* save list */
    '.slsb-runs{width:470px;display:flex;flex-direction:column;min-height:0}',
    '.slsb-tools{display:flex;gap:8px;margin-bottom:10px}',
    '.slsb input.q,.slsb input.nt{width:100%;background:rgba(0,0,0,.5);border:1px solid #4a4236;color:#eee;padding:8px 12px;font-size:14px;font-family:inherit;outline:none}',
    '.slsb input.q{flex:1;width:auto;min-width:0}',
    '.slsb input.q:focus,.slsb input.nt:focus{border-color:var(--gold)}',
    '.slsb-chips{display:flex;gap:6px;flex-wrap:wrap;margin-bottom:10px}',
    '.slsb .chip{background:none;border:1px solid #4a4236;color:#aaa;padding:3px 12px;font-size:13px;cursor:pointer;border-radius:12px}',
    '.slsb .chip:hover{color:#fff;border-color:#7a6a4a}',
    '.slsb .chip.on{border-color:var(--gold);color:var(--gold);background:rgba(255,200,58,.08)}',
    '.slsb-list{flex:1;overflow-y:auto;padding-right:6px}',
    '.slsb-run{display:flex;gap:12px;align-items:center;padding:10px 12px;margin-bottom:8px;border:1px solid var(--edge);border-left:3px solid var(--edge);background:var(--panel);cursor:pointer}',
    '.slsb-run:hover{border-color:#7a6a4a}',
    '.slsb-run.on{border-color:var(--gold);background:rgba(44,35,19,.94)}',
    '.slsb-run.s-live{border-left-color:rgba(143,199,191,.7)}.slsb-run.s-end{border-left-color:rgba(255,200,58,.7)}.slsb-run.s-endless{border-left-color:rgba(201,164,255,.7)}.slsb-run.s-warn{border-left-color:var(--red)}.slsb-run.s-gone{border-left-color:#555}',
    '.slsb-run.gone{opacity:.75}',
    '.slsb-run .meta{flex:1;min-width:0}',
    '.slsb-run .top{display:flex;align-items:baseline;gap:8px}',
    '.slsb-run .nm{flex:1;min-width:0;color:#fff;font-size:16px;font-weight:800;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.slsb-run .dt{color:var(--dim);font-size:12px;white-space:nowrap}',
    '.slsb-run .l2{color:#bbb;font-size:13px;margin-top:4px;display:flex;align-items:center;white-space:nowrap;overflow:hidden}',
    '.slsb-run .l3{color:var(--dim);font-size:12px;margin-top:3px;display:flex}',
    '.slsb-sec{color:var(--dim);font-size:12px;letter-spacing:2px;margin:14px 0 8px;display:flex;align-items:center;gap:8px;text-transform:uppercase}',
    '.slsb-sec::after{content:"";flex:1;height:1px;background:#2e2a22}',
    '.slsb-empty{border:1px dashed #5a5040;padding:14px;text-align:center;color:var(--dim);font-size:14px;line-height:1.6}',
    '.slsb-foot{margin-top:10px}',
    '.slsb-foot .acts{margin-top:0}',
    '.slsb .disk{margin-top:10px;font-size:12px;color:var(--dim)}',
    '.slsb .bar{height:6px;background:#221f1a;margin-top:5px;border-radius:3px;overflow:hidden}',
    '.slsb .bar i{display:block;height:100%;background:linear-gradient(90deg,var(--teal),var(--amber))}',
    '.slsb .bar.full i{background:var(--red)}',
    /* the selected save */
    '.slsb-det{flex:1;display:flex;flex-direction:column;min-width:0;min-height:0;border:1px solid var(--edge);background:var(--panel)}',
    '.slsb-dtop{display:flex;gap:18px;align-items:flex-start;padding:18px 20px;border-bottom:1px solid var(--line);background:linear-gradient(180deg,rgba(235,163,8,.05),transparent)}',
    '.slsb-dtop .info{flex:1;min-width:0}',
    '.slsb-dtop .ttl{display:flex;align-items:baseline;gap:12px;flex-wrap:wrap}',
    '.slsb-dtop h2{margin:0;font-size:26px;color:#fff;font-weight:800;line-height:1.15}',
    '.slsb-dtop .ttl .dt{color:var(--dim);font-size:14px}',
    '.slsb .kv{display:grid;grid-template-columns:repeat(4,max-content);gap:8px 34px;margin:12px 0 2px;font-size:14px}',
    '.slsb .kv span{color:var(--dim);font-size:12px;display:block}',
    '.slsb .kv b{color:#eee;font-weight:400;white-space:nowrap}',
    '.slsb .acts{display:flex;gap:8px;margin-top:14px;flex-wrap:wrap}',
    '.slsb-dtop .hint{color:var(--dim);font-size:13px;margin-top:12px}',
    '.slsb-warn{margin:12px 20px 0;padding:10px 14px;border-left:3px solid var(--red);background:rgba(224,106,90,.08);font-size:14px;color:#f2c6be;line-height:1.5}',
    '.slsb-warn b{color:#fff}',
    '.slsb-warn .acts{margin-top:8px}',
    '.slsb-tlhd{display:flex;align-items:center;gap:18px;padding:14px 20px 6px}',
    '.slsb-tlhd h3{margin:0;font-size:15px;color:var(--gold);letter-spacing:3px;text-transform:uppercase;font-weight:800}',
    '.slsb-tlhd .lg{display:flex;gap:14px;flex-wrap:wrap;font-size:12px;color:var(--dim)}',
    '.slsb-tlhd .lg span{display:flex;align-items:center}',
    '.slsb-tl{flex:1;display:flex;min-height:0}',
    '.slsb-pts{width:55%;overflow-y:auto;padding:2px 10px 14px 20px}',
    '.slsb .day{color:var(--dim);font-size:12px;letter-spacing:2px;margin:12px 0 4px;display:flex;align-items:center;gap:8px;text-transform:uppercase}',
    '.slsb .day::after{content:"";flex:1;height:1px;background:#2e2a22}',
    '.slsb .dtx{color:#a99c80;font-size:12px;font-style:italic;margin:0 0 4px 10px;line-height:1.45;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.slsb-pt{display:flex;align-items:center;gap:10px;padding:7px 10px;font-size:14px;border:1px solid transparent;cursor:pointer}',
    '.slsb-pt:hover{background:rgba(255,255,255,.03);border-color:#2e2a22}',
    '.slsb-pt.on{border-color:var(--gold);background:rgba(255,200,58,.06)}',
    '.slsb-pt .tm{width:92px;color:#bbb;font-family:Consolas,monospace;font-size:13px;flex:none}',
    '.slsb-pt .rs{flex:1;color:#ddd;min-width:0;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.slsb-pt .rs small{color:var(--dim);margin-left:8px;font-size:12px}',
    '.slsb-pt .rs i{color:#d8c9a6;margin-left:8px;font-size:12px}',
    '.slsb-pt .tag{font-size:11px;letter-spacing:1px;text-transform:uppercase;white-space:nowrap}',
    '.slsb-pt .tag.new{color:var(--teal)}.slsb-pt .tag.pin{color:var(--gold)}.slsb-pt .tag.old{color:#e0a060}',
    '.slsb-pv{flex:1;min-width:0;border-left:1px solid var(--line);padding:14px 20px;overflow-y:auto}',
    '.slsb-pv h4{margin:0 0 6px;color:#fff;font-size:19px;font-weight:800;display:flex;align-items:center}',
    '.slsb-pv h4 .dot{width:11px;height:11px;margin-right:9px}',
    '.slsb-pv .when{color:var(--dim);font-size:13px;display:flex;flex-wrap:wrap}',
    '.slsb-pv .bdg{margin-top:10px}',
    '.slsb-pv .journal{font-size:13px;color:#c8bca6;line-height:1.6;border-left:2px solid #4a4236;padding-left:10px;margin:12px 0 0;font-style:italic}',
    '.slsb-pv .nb{margin-top:12px;padding:8px 10px;border:1px solid var(--edge);background:rgba(0,0,0,.3);color:#e6d8b8;font-size:13px;word-break:break-word}',
    '.slsb-pv .nb span{display:block;color:var(--dim);font-size:11px;letter-spacing:1px;margin-bottom:2px;text-transform:uppercase}',
    '.slsb-pv .ow{margin-top:12px;padding:8px 12px;border-left:3px solid #e0a060;background:rgba(224,160,96,.08);color:#f0d2b0;font-size:13px;line-height:1.5}',
    '.slsb-pv .acts{margin-top:16px}',
    '.slsb-pv .acts2{display:flex;gap:6px;flex-wrap:wrap;margin-top:8px}',
    '.slsb-pv .hint{color:var(--dim);font-size:12px;margin-top:14px;line-height:1.5}',
    '.slsb-none{padding:6px 20px 16px}',
    /* dialogs */
    '.slsb-modal{position:absolute;inset:0;background:rgba(0,0,0,.72);display:flex;align-items:center;justify-content:center;z-index:5}',
    '.slsb-mbox{width:640px;border:1px solid var(--amber);background:#12100c;box-shadow:0 20px 60px rgba(0,0,0,.8)}',
    '.slsb-mhd{padding:14px 20px;border-bottom:1px solid var(--line);font-size:20px;font-weight:800;color:#fff;letter-spacing:2px}',
    '.slsb-mbd{padding:16px 20px;font-size:15px;line-height:1.6;color:#ccc}',
    '.slsb-mft{padding:12px 20px;border-top:1px solid var(--line);display:flex;gap:10px;justify-content:flex-end}',
    '.slsb-mbd .mw{margin-top:10px;padding:8px 12px;border-left:3px solid #e0a060;background:rgba(224,160,96,.08);color:#f0d2b0}',
    '.slsb-mbd .mh{color:var(--dim);font-size:13px;margin-bottom:8px}'
  ].join('');

  // Dot colours for the backup kinds, in the order the legend lists them.
  var KIND_COLORS = {
    morning: '#FFC83A',
    quit: '#9a9a9a',
    manual: '#ffffff',
    expedition: '#8fc7bf',
    wave: '#e06a5a',
    rebirth: '#c9a4ff',
    ending: '#EBA308',
    safety: '#6f6a60',
    imported: '#6f8fb8'
  };

  // Each survivor's initial gets a steady colour, picked from the survivor's name.
  var AVATAR_COLORS = ['#d8b070', '#8fc7bf', '#c9a4ff', '#e0a060', '#9fc48a', '#7fb0e0', '#e08a9a'];

  var view = null;
  var selected = null;
  var picked = null;
  var dialog = null;
  var isBusy = false;
  var query = '';
  var filter = 'all';
  var isTyping = false;
  var pageEscAt = 0;

  function post(parts) {
    if (window.vuplex) window.vuplex.postMessage(PREFIX + parts.join('|'));
  }

  // The Toolkit's shared sizing, or a queue it picks up once it loads.
  function uiSize(el, options) {
    var ui = window.ExiledUi;
    if (ui) { ui.size(el, options); return; }
    var waiting = window.__exiledUiWait = window.__exiledUiWait || [];
    waiting.push([el, options]);
    if (waiting.length > 32) waiting.shift();
  }

  function pick(pair) {
    return pair[view && view.zh ? 1 : 0];
  }

  function t(key) {
    var pair = TEXT[key];
    return pair ? pick(pair) : key;
  }

  function f(key) {
    var text = t(key);
    for (var i = 1; i < arguments.length; i++) text = text.split('{' + (i - 1) + '}').join(String(arguments[i]));
    return text;
  }

  function kindName(kind) {
    var pair = TEXT.kinds[kind];
    return pair ? pick(pair) : kind;
  }

  // The game sends keys to the page only while the plugin asks it to, so a text box tells the plugin when it has
  // focus. A redraw can drop a focused box without a blur, so the check waits a tick and looks at what has focus.
  function isTextBox(node) {
    return !!node && node.tagName === 'INPUT' && !!node.closest && !!node.closest('#' + ROOT_ID);
  }

  function typing(isOn) {
    if (isOn === isTyping) return;
    isTyping = isOn;
    post(['typing', isOn ? '1' : '0']);
  }

  function watchTyping(input) {
    input.addEventListener('focus', function () { typing(true); });
    input.addEventListener('blur', function () {
      setTimeout(function () { if (!isTextBox(document.activeElement)) typing(false); }, 0);
    });
  }

  // Esc handled by the page; the plugin's own Esc for the same key press is ignored.
  function pageEsc(e) {
    e.preventDefault();
    e.stopPropagation();
    pageEscAt = Date.now();
  }

  function oldVersionWarn(backup) {
    return backup.oldVersion ? f('oldVersionWarn', backup.version, view.version) : '';
  }

  function el(tag, cls, text) {
    var node = document.createElement(tag);
    if (cls) node.className = cls;
    if (text != null) node.textContent = text;
    return node;
  }

  // A line of short facts the stylesheet separates with dots.
  function facts(cls, items) {
    var line = el('div', cls + ' sep');
    items.forEach(function (item) { if (item) line.appendChild(el('span', null, item)); });
    return line;
  }

  function button(label, cls, onClick) {
    var node = el('button', 'btn' + (cls ? ' ' + cls : ''), label);
    node.type = 'button';
    node.addEventListener('click', function (e) {
      e.stopPropagation();
      if (!isBusy) onClick();
    });
    return node;
  }

  function size(bytes) {
    if (bytes >= 1073741824) return (bytes / 1073741824).toFixed(1) + ' GB';
    if (bytes >= 1048576) return (bytes / 1048576).toFixed(1) + ' MB';
    return Math.max(1, Math.round(bytes / 1024)) + ' KB';
  }

  function where(item) {
    return f('loopDay', item.turn, item.day);
  }

  function counted(n, oneKey, manyKey) {
    return n === 1 ? t(oneKey) : f(manyKey, n);
  }

  // The game names a save after the moment it was started: 2026_10_07_10_20_23.
  function startedAt(run) {
    var m = /(\d{4})_(\d\d)_(\d\d)_(\d\d)_(\d\d)/.exec(run.name || run.stem || '');
    return m ? m[1] + '-' + m[2] + '-' + m[3] + ' ' + m[4] + ':' + m[5] : '';
  }

  function runTitle(run) {
    return run.role || startedAt(run) || run.name || run.stem;
  }

  function shortAt(at) {
    return at.slice(5);
  }

  function stateOf(entry) {
    if (entry.isGone) return 'gone';
    if (entry.run.behind) return 'warn';
    if (entry.run.finished) return 'end';
    if (entry.run.endless) return 'endless';
    return 'live';
  }

  function stateBadge(state) {
    return el('span', 'badge b-' + state, pick(TEXT.states[state]));
  }

  function avatar(entry, isBig) {
    var name = entry.run.role || '';
    var hash = 0;
    for (var i = 0; i < name.length; i++) hash = (hash * 31 + name.charCodeAt(i)) >>> 0;
    var node = el('div', 'av' + (isBig ? ' big' : '') + (entry.isGone ? ' gone' : ''), name ? name.charAt(0).toUpperCase() : '?');
    node.style.background = name ? AVATAR_COLORS[hash % AVATAR_COLORS.length] : '#6a6358';
    return node;
  }

  function backupBytes(run) {
    var total = 0;
    (run.backups || []).forEach(function (b) { total += b.bytes || 0; });
    return total;
  }

  function busy() {
    isBusy = true;
    var box = document.querySelector('#' + ROOT_ID + ' .slsb');
    if (box) box.classList.add('busy');
  }

  // The game's own font, from its web UI folder; the page falls back to Windows fonts without it.
  function fontFaces() {
    var at = location.href.indexOf('/WebUI/');
    var res = (at >= 0 ? location.href.slice(0, at + 7) : '') + 'Res/GameResult/';
    return '@font-face{font-family:"SLSB Sans";src:url("' + res + 'SourceHanSansSC-Regular-2.otf");font-weight:400 600}' +
      '@font-face{font-family:"SLSB Sans";src:url("' + res + 'SourceHanSansSC-Heavy-2.otf");font-weight:700 900}';
  }

  function ensureStyle() {
    if (document.getElementById(ROOT_ID + '-css')) return;
    var style = el('style');
    style.id = ROOT_ID + '-css';
    style.textContent = fontFaces() + CSS;
    (document.head || document.documentElement).appendChild(style);
  }

  function allRuns() {
    return (view.runs || []).map(function (run) { return { run: run, isGone: false }; })
      .concat((view.deleted || []).map(function (run) { return { run: run, isGone: true }; }));
  }

  function matches(entry) {
    var run = entry.run;
    var backups = run.backups || [];
    if (filter === 'active' && (entry.isGone || run.finished)) return false;
    if (filter === 'finished' && (entry.isGone || !run.finished)) return false;
    if (filter === 'backed' && !backups.length) return false;
    var q = query.trim().toLowerCase();
    if (!q) return true;
    var text = [run.name, run.stem, run.role, run.diff, run.status, startedAt(run)].join(' ').toLowerCase();
    for (var i = 0; i < backups.length; i++) text += ' ' + (backups[i].note || '').toLowerCase();
    return text.indexOf(q) >= 0;
  }

  function fillList(list) {
    list.textContent = '';
    var runs = (view.runs || []).map(function (run) { return { run: run, isGone: false }; }).filter(matches);
    var gone = (view.deleted || []).map(function (run) { return { run: run, isGone: true }; }).filter(matches);
    if (!(view.runs || []).length && !gone.length) list.appendChild(el('div', 'slsb-empty', t('noRuns')));
    else if (!runs.length && !gone.length) list.appendChild(el('div', 'slsb-empty', t('noMatch')));
    runs.forEach(function (entry) { list.appendChild(runCard(entry)); });
    if (gone.length) {
      list.appendChild(el('div', 'slsb-sec', t('deleted')));
      gone.forEach(function (entry) { list.appendChild(runCard(entry)); });
    }
  }

  function redrawList() {
    var list = document.querySelector('#' + ROOT_ID + ' .slsb-list');
    if (list) fillList(list);
  }

  function searchBox() {
    var input = el('input', 'q');
    input.type = 'text';
    input.placeholder = t('search');
    input.value = query;
    input.maxLength = 60;
    input.spellcheck = false;
    watchTyping(input);
    input.addEventListener('input', function () {
      query = input.value;
      redrawList();
    });
    input.addEventListener('keydown', function (e) {
      if (e.key === 'Escape') {
        pageEsc(e);
        if (query) {
          query = input.value = '';
          redrawList();
        } else {
          input.blur();
        }
      } else if (e.key === 'Enter') {
        input.blur();
      }
    });
    return input;
  }

  function filterChips() {
    var chips = el('div', 'slsb-chips');
    ['all', 'active', 'finished', 'backed'].forEach(function (key) {
      var chip = el('button', 'chip' + (filter === key ? ' on' : ''), pick(TEXT.filters[key]));
      chip.type = 'button';
      chip.addEventListener('click', function () {
        if (isBusy) return;
        filter = key;
        draw();
      });
      chips.appendChild(chip);
    });
    return chips;
  }

  function findSelected() {
    var all = allRuns();
    for (var i = 0; i < all.length; i++) if (all[i].run.stem === selected) return all[i];
    selected = all.length ? all[0].run.stem : null;
    return all.length ? all[0] : null;
  }

  function findPicked(run) {
    var backups = run.backups || [];
    for (var i = 0; i < backups.length; i++) if (backups[i].id === picked) return backups[i];
    picked = backups.length ? backups[0].id : null;
    return backups.length ? backups[0] : null;
  }

  function runCard(entry) {
    var run = entry.run;
    var state = stateOf(entry);
    var card = el('div', 'slsb-run s-' + state + (run.stem === selected ? ' on' : '') + (entry.isGone ? ' gone' : ''));
    card.appendChild(avatar(entry, false));

    var meta = el('div', 'meta');
    var top = el('div', 'top');
    top.appendChild(el('div', 'nm', runTitle(run)));
    if (run.role && startedAt(run)) top.appendChild(el('div', 'dt', startedAt(run)));
    meta.appendChild(top);

    var line = el('div', 'l2');
    line.appendChild(stateBadge(state));
    line.appendChild(facts('', [run.diff, where(run)]));
    meta.appendChild(line);

    var backups = run.backups || [];
    meta.appendChild(facts('l3', backups.length
      ? [counted(backups.length, 'backupOne', 'backupCount'), f('lastAt', shortAt(backups[0].at))]
      : [t('noBackupsShort')]));
    card.appendChild(meta);

    card.addEventListener('click', function () {
      if (isBusy) return;
      selected = run.stem;
      picked = null;
      draw();
    });
    return card;
  }

  function info(label, value) {
    var cell = el('div');
    cell.appendChild(el('span', null, label));
    cell.appendChild(el('b', null, value));
    return cell;
  }

  function confirmStep(title, body, label, cls, parts, warn) {
    dialog = { title: title, body: body, warn: warn || '', label: label, cls: cls, parts: parts };
    draw();
  }

  function restoreStep(run, isGone, backup) {
    var name = runTitle(run);
    var warn = oldVersionWarn(backup);
    if (isGone) {
      confirmStep(t('bringBackTitle'), f('bringBackBody', name, where(backup), backup.at), t('bringBack'), 'pri', ['bringback', run.stem, backup.id], warn);
    } else {
      confirmStep(t('restoreTitle'), f('restoreBody', name, where(backup), backup.at), t('restore'), 'pri', ['restore', run.stem, backup.id], warn);
    }
  }

  function noteStep(run, backup) {
    dialog = {
      title: t('noteTitle'),
      hint: t('noteHint'),
      input: backup.note || '',
      label: t('save'),
      cls: 'pri',
      onOk: function (value) {
        busy();
        post(['note', run.stem, backup.id, encodeURIComponent(value.trim())]);
      }
    };
    draw();
  }

  function saveHeader(entry) {
    var run = entry.run;
    var backups = run.backups || [];
    var top = el('div', 'slsb-dtop');
    top.appendChild(avatar(entry, true));

    var box = el('div', 'info');
    var ttl = el('div', 'ttl');
    ttl.appendChild(el('h2', null, runTitle(run)));
    ttl.appendChild(stateBadge(stateOf(entry)));
    box.appendChild(ttl);

    var kv = el('div', 'kv');
    if (run.role) kv.appendChild(info(t('role'), run.role));
    if (run.diff) kv.appendChild(info(t('difficulty'), run.diff));
    kv.appendChild(info(t('progress'), f('loopDayTitle', run.turn, run.day)));
    if (run.finished && run.status) kv.appendChild(info(t('status'), run.status));
    if (startedAt(run)) kv.appendChild(info(t('started'), startedAt(run)));
    if (backups.length) kv.appendChild(info(t('lastBackup'), backups[0].at));
    kv.appendChild(info(t('backupsHeld'), String(backups.length)));
    if (backups.length) kv.appendChild(info(t('backupSize'), size(backupBytes(run))));
    box.appendChild(kv);

    if (entry.isGone) {
      box.appendChild(el('div', 'hint', t('goneHint')));
    } else {
      var acts = el('div', 'acts');
      acts.appendChild(button(t('load'), 'pri', function () { busy(); post(['load', run.file]); }));
      acts.appendChild(button(t('backUp'), '', function () { busy(); post(['backup', run.stem]); }));
      acts.appendChild(button(t('del'), 'bad', function () {
        confirmStep(t('deleteTitle'), f('deleteBody', runTitle(run)), t('del'), 'bad', ['delete', run.stem]);
      }));
      box.appendChild(acts);
    }
    top.appendChild(box);
    return top;
  }

  function behindWarning(run) {
    var backup = null;
    (run.backups || []).forEach(function (b) { if (b.id === run.behind.id) backup = b; });
    var warn = el('div', 'slsb-warn');
    warn.appendChild(el('b', null, t('behind') + '. '));
    warn.appendChild(document.createTextNode(f('behindBody', where(run), backup ? backup.at : run.behind.id, where(run.behind))));

    var acts = el('div', 'acts');
    if (backup) acts.appendChild(button(t('restore'), 'sm pri', function () { restoreStep(run, false, backup); }));
    acts.appendChild(button(t('dismiss'), 'sm', function () { busy(); post(['dismiss', run.stem]); }));
    warn.appendChild(acts);
    return warn;
  }

  function legend(backups) {
    var seen = {};
    backups.forEach(function (b) { seen[b.kind] = true; });
    var box = el('div', 'lg');
    Object.keys(KIND_COLORS).forEach(function (kind) {
      if (!seen[kind]) return;
      var item = el('span');
      var dot = el('i', 'dot');
      dot.style.background = KIND_COLORS[kind];
      item.appendChild(dot);
      item.appendChild(document.createTextNode(kindName(kind)));
      box.appendChild(item);
    });
    return box;
  }

  function kindDot(kind) {
    var dot = el('span', 'dot');
    dot.style.background = KIND_COLORS[kind] || '#888';
    return dot;
  }

  function backupRow(backup, isLatest) {
    var row = el('div', 'slsb-pt' + (backup.id === picked ? ' on' : ''));
    row.appendChild(el('div', 'tm', shortAt(backup.at)));
    row.appendChild(kindDot(backup.kind));

    var what = el('div', 'rs', kindName(backup.kind));
    if (!backup.progress) what.appendChild(el('small', null, t('safetyNote')));
    if (backup.note) what.appendChild(el('i', null, backup.note));
    row.appendChild(what);

    if (backup.oldVersion) row.appendChild(el('span', 'tag old', t('oldVersion')));
    if (backup.pinned) row.appendChild(el('span', 'tag pin', t('pinned')));
    if (isLatest) row.appendChild(el('span', 'tag new', t('latest')));

    row.addEventListener('click', function () {
      if (isBusy) return;
      picked = backup.id;
      draw();
    });
    return row;
  }

  function backupList(backups) {
    var pts = el('div', 'slsb-pts');
    function groupOf(b) {
      return b.progress ? f('loop', b.turn) + '  /  ' + f('day', b.day) : b.at.slice(0, 10);
    }
    // The game's own words for each day, from the first backup of that day that kept them.
    var dayTexts = {};
    backups.forEach(function (b) {
      var group = groupOf(b);
      if (b.dayText && !dayTexts[group]) dayTexts[group] = b.dayText;
    });
    var lastGroup = null;
    backups.forEach(function (b, i) {
      var group = groupOf(b);
      if (group !== lastGroup) {
        pts.appendChild(el('div', 'day', group));
        lastGroup = group;
        if (dayTexts[group]) {
          var dtx = el('div', 'dtx', dayTexts[group]);
          dtx.title = dayTexts[group];
          pts.appendChild(dtx);
        }
      }
      pts.appendChild(backupRow(b, i === 0));
    });
    return pts;
  }

  function backupPanel(entry, backup) {
    var run = entry.run;
    var pv = el('div', 'slsb-pv');
    var h4 = el('h4');
    h4.appendChild(kindDot(backup.kind));
    h4.appendChild(document.createTextNode(kindName(backup.kind)));
    pv.appendChild(h4);
    pv.appendChild(facts('when', [backup.at, backup.progress ? f('loopDayTitle', backup.turn, backup.day) : t('safetyNote'), size(backup.bytes)]));

    if (backup.pinned || backup.oldVersion) {
      var bdg = el('div', 'bdg');
      if (backup.pinned) bdg.appendChild(el('span', 'badge b-pin', t('pinned')));
      if (backup.oldVersion) {
        var old = el('span', 'badge b-old', t('oldVersion'));
        old.title = f('oldVersionTip', backup.version, view.version);
        bdg.appendChild(old);
      }
      pv.appendChild(bdg);
    }

    if (backup.dayText) pv.appendChild(el('div', 'journal', backup.dayText));
    if (backup.note) {
      var nb = el('div', 'nb');
      nb.appendChild(el('span', null, t('note')));
      nb.appendChild(document.createTextNode(backup.note));
      pv.appendChild(nb);
    }
    if (backup.oldVersion) pv.appendChild(el('div', 'ow', oldVersionWarn(backup)));

    var acts = el('div', 'acts');
    acts.appendChild(button(entry.isGone ? t('bringBack') : t('restore'), 'pri', function () { restoreStep(run, entry.isGone, backup); }));
    if (backup.progress) {
      acts.appendChild(button(t('copy'), '', function () {
        confirmStep(t('copyTitle'), f('copyBody', runTitle(run), where(backup), backup.at), t('copy'), 'pri', ['copy', run.stem, backup.id], oldVersionWarn(backup));
      }));
    }
    pv.appendChild(acts);

    var more = el('div', 'acts2');
    more.appendChild(button(backup.pinned ? t('unpin') : t('pin'), 'sm', function () {
      busy();
      post(['pin', run.stem, backup.id, backup.pinned ? '0' : '1']);
    }));
    more.appendChild(button(t('note'), 'sm', function () { noteStep(run, backup); }));
    more.appendChild(button(t('exportBtn'), 'sm', function () {
      busy();
      post(['export', run.stem, backup.id]);
    }));
    more.appendChild(button(t('remove'), 'sm bad', function () {
      confirmStep(t('removeTitle'), f('removeBody', backup.at, kindName(backup.kind)), t('remove'), 'bad', ['remove', run.stem, backup.id]);
    }));
    pv.appendChild(more);

    if (!entry.isGone) pv.appendChild(el('div', 'hint', t('restoreHint')));
    return pv;
  }

  function detail(entry) {
    var det = el('div', 'slsb-det');
    if (!entry) {
      det.appendChild(el('div', 'slsb-none slsb-empty', t('noRuns')));
      return det;
    }

    var run = entry.run;
    var backups = run.backups || [];
    det.appendChild(saveHeader(entry));
    if (run.behind) det.appendChild(behindWarning(run));

    var hd = el('div', 'slsb-tlhd');
    hd.appendChild(el('h3', null, t('backups')));
    hd.appendChild(legend(backups));
    det.appendChild(hd);

    if (!backups.length) {
      var none = el('div', 'slsb-none');
      none.appendChild(el('div', 'slsb-empty', f('noBackups', view.key ? f('keyHint', view.key) : '')));
      det.appendChild(none);
      return det;
    }

    var tl = el('div', 'slsb-tl');
    var backup = findPicked(run);
    tl.appendChild(backupList(backups));
    tl.appendChild(backupPanel(entry, backup));
    det.appendChild(tl);
    return det;
  }

  function header() {
    var hd = el('div', 'slsb-hd');
    var runs = view.runs || [];
    var backupTotal = 0;
    allRuns().forEach(function (entry) { backupTotal += (entry.run.backups || []).length; });
    hd.appendChild(el('h1', null, t('title')));
    hd.appendChild(facts('sub', [counted(runs.length, 'saveOne', 'saveCount'), counted(backupTotal, 'backupOne', 'backupCount'), t('sub')]));
    hd.appendChild(el('div', 'sp'));

    var x = el('button', 'x', 'X');
    x.type = 'button';
    x.addEventListener('click', function () { if (!isBusy) post(['close']); });
    hd.appendChild(x);
    return hd;
  }

  function diskUse() {
    var bytes = view.bytes || 0;
    var limit = view.limit || 0;
    var disk = el('div', 'disk', limit ? f('usedOf', size(bytes), size(limit)) : f('used', size(bytes)));
    if (!limit) return disk;

    var share = Math.min(1, bytes / limit);
    var bar = el('div', 'bar' + (share >= 0.95 ? ' full' : ''));
    var fill = el('i');
    fill.style.width = Math.max(share * 100, bytes ? 1 : 0).toFixed(1) + '%';
    bar.appendChild(fill);
    disk.appendChild(bar);
    return disk;
  }

  function saveList() {
    var left = el('div', 'slsb-runs');
    var tools = el('div', 'slsb-tools');
    tools.appendChild(searchBox());
    tools.appendChild(button(t('importBtn'), '', function () { busy(); post(['import']); }));
    left.appendChild(tools);
    left.appendChild(filterChips());

    var list = el('div', 'slsb-list');
    fillList(list);
    left.appendChild(list);

    var foot = el('div', 'slsb-foot');
    var acts = el('div', 'acts');
    acts.appendChild(button(t('gameList'), 'sm', function () { busy(); post(['gamelist']); }));
    acts.appendChild(button(t('folder'), 'sm', function () { busy(); post(['folder']); }));
    foot.appendChild(acts);
    foot.appendChild(diskUse());
    left.appendChild(foot);
    return left;
  }

  function closeDialog() {
    dialog = null;
    draw();
  }

  function modal() {
    var current = dialog;
    var back = el('div', 'slsb-modal');
    var box = el('div', 'slsb-mbox');
    box.appendChild(el('div', 'slsb-mhd', current.title));
    var bd = el('div', 'slsb-mbd');
    var input = null;

    function ok() {
      dialog = null;
      draw();
      if (current.onOk) {
        current.onOk(input ? input.value : '');
      } else {
        busy();
        post(current.parts);
      }
    }

    if (current.onOk) {
      if (current.hint) bd.appendChild(el('div', 'mh', current.hint));
      input = el('input', 'nt');
      input.type = 'text';
      input.maxLength = 200;
      input.spellcheck = false;
      input.value = current.input || '';
      watchTyping(input);
      input.addEventListener('input', function () { current.input = input.value; });
      input.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
          pageEsc(e);
          closeDialog();
        } else if (e.key === 'Enter') {
          e.preventDefault();
          ok();
        }
      });
      bd.appendChild(input);
    } else {
      bd.appendChild(document.createTextNode(current.body));
    }
    if (current.warn) bd.appendChild(el('div', 'mw', current.warn));
    box.appendChild(bd);

    var ft = el('div', 'slsb-mft');
    ft.appendChild(button(t('cancel'), '', closeDialog));
    ft.appendChild(button(current.label, current.cls, ok));
    box.appendChild(ft);
    back.appendChild(box);
    back.addEventListener('click', function (e) {
      if (e.target === back && !isBusy) closeDialog();
    });
    back.__input = input;
    return back;
  }

  // The scrolled panes keep their place across a redraw.
  var SCROLLERS = ['.slsb-list', '.slsb-pts', '.slsb-pv'];

  function scrollPlaces(root) {
    return SCROLLERS.map(function (sel) {
      var node = root.querySelector(sel);
      return node ? node.scrollTop : 0;
    });
  }

  function restoreScroll(root, places) {
    SCROLLERS.forEach(function (sel, i) {
      var node = root.querySelector(sel);
      if (node) node.scrollTop = places[i];
    });
  }

  function draw() {
    var root = document.getElementById(ROOT_ID);
    if (!view) return;
    ensureStyle();
    if (!root) {
      root = el('div');
      root.id = ROOT_ID;
      (document.body || document.documentElement).appendChild(root);
    }

    var places = scrollPlaces(root);
    var active = document.activeElement;
    var wasSearching = !!active && !!active.classList && active.classList.contains('q') && isTextBox(active);
    var caret = wasSearching ? active.selectionStart : 0;
    var hadNote = !!root.querySelector('input.nt');
    root.textContent = '';

    var box = el('div', 'slsb' + (isBusy ? ' busy' : ''));
    var entry = findSelected();
    var body = el('div', 'slsb-body');
    box.appendChild(header());
    body.appendChild(saveList());
    body.appendChild(detail(entry));
    box.appendChild(body);
    var dlg = dialog ? modal() : null;
    if (dlg) box.appendChild(dlg);
    root.appendChild(box);
    restoreScroll(root, places);

    // A redraw replaces the text boxes, so the one being typed in gets the focus back.
    if (dlg && dlg.__input) {
      dlg.__input.focus();
      if (!hadNote) dlg.__input.setSelectionRange(dlg.__input.value.length, dlg.__input.value.length);
    } else if (wasSearching && !dlg) {
      var q = root.querySelector('input.q');
      if (q) {
        q.focus();
        q.setSelectionRange(caret, caret);
      }
    }
    if (!isTextBox(document.activeElement)) typing(false);
    uiSize(root, { fit: box });
  }

  function parse(json) {
    try {
      return JSON.parse(json);
    } catch (e) {
      return null;
    }
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
      dialog = null;
      isBusy = false;
      selected = null;
      picked = null;
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
    close: function () {
      view = null;
      dialog = null;
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
      if (dialog) {
        dialog = null;
        draw();
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
