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
    noRuns: ['No saves yet.', '还没有存档。'],
    deleted: ['Deleted saves', '已删除的存档'],
    gameList: ['Default list', '默认列表'],
    folder: ['Open folder', '打开文件夹'],
    used: ['Backups use {0}', '备份共占用 {0}'],
    load: ['Load', '读取'],
    backUp: ['Back up now', '立即备份'],
    del: ['Delete save', '删除存档'],
    restore: ['Restore', '恢复'],
    bringBack: ['Restore', '恢复'],
    remove: ['Remove', '移除'],
    pin: ['Pin', '固定'],
    unpin: ['Unpin', '取消固定'],
    pinned: ['Pinned', '已固定'],
    cancel: ['Cancel', '取消'],
    dismiss: ['Dismiss', '忽略'],
    backups: ['Backups', '备份'],
    noBackups: ['No backups of this save yet. One is made each morning, when you quit, before a rebirth, an expedition or a zombie wave, and at an ending{0}.', '这个存档还没有备份。每天早上、退出时、重生前、探索前、尸潮前和到达结局时都会自动备份{0}。'],
    keyHint: [', or press {0} in the run', '，也可以在游戏中按 {0}'],
    loop: ['Loop {0}', '第 {0} 轮'],
    day: ['Day {0}', '第 {0} 天'],
    loopDay: ['loop {0}, day {1}', '第 {0} 轮第 {1} 天'],
    progress: ['Progress', '进度'],
    role: ['Survivor', '角色'],
    time: ['Played', '游戏时长'],
    difficulty: ['Difficulty', '难度'],
    status: ['Status', '状态'],
    finished: ['Finished', '已结束'],
    endless: ['Endless', '无尽'],
    behind: ['Behind its backup', '比备份旧'],
    behindBody: ['This save is at {0}, but its backup from {1} is at {2}. Steam Cloud or a crash may have put back an older copy. Restore that backup to get your progress back, or dismiss this if you rolled it back on purpose.', '这个存档在{0}，但 {1} 的备份在{2}。可能是 Steam 云同步或崩溃换回了旧存档。恢复这个备份即可找回进度；如果是你自己回退的，点「忽略」即可。'],
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
    search: ['Search saves', '搜索存档'],
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
    more: ['More', '更多'],
    less: ['Less', '收起'],
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
    bringBackTitle: ['Bring this save back?', '恢复这个存档？'],
    bringBackBody: ['{0} goes back into your saves at {1}, as it was on {2}.', '「{0}」将以{1}（{2} 时的状态）回到存档列表。'],
    deleteTitle: ['Delete this save?', '删除这个存档？'],
    deleteBody: ['{0} is removed from your saves. Its backups stay, under Deleted saves, and it can be brought back from there.', '「{0}」将从存档列表中删除。它的备份会保留在「已删除的存档」中，可以从那里恢复。'],
    removeTitle: ['Remove this backup?', '移除这个备份？'],
    removeBody: ['The backup from {0} ({1}) is deleted for good.', '{0} 的备份（{1}）将被永久删除。'],
    safetyNote: ['made before a restore or a delete', '恢复或删除前自动创建']
  };

  var CSS = [
    '#' + ROOT_ID + '{position:fixed;inset:0;z-index:2147483000;background:rgba(0,0,0,.78);display:flex;align-items:center;justify-content:center;font-family:inherit;color:#ddd}',
    '#' + ROOT_ID + ' *{box-sizing:border-box}',
    '.slsb{--amber:#EBA308;--gold:#FFC83A;--teal:#8fc7bf;--red:#e06a5a;--dim:#8a7f6c;--line:rgba(220,188,120,.22);--panel:rgba(14,12,10,.92);position:relative;width:1500px;height:820px;display:flex;flex-direction:column;padding:24px 30px 20px;background:#0f0d0b;border:1px solid #3a342a;box-shadow:0 20px 60px rgba(0,0,0,.8)}',
    '.slsb.busy{cursor:progress}.slsb.busy button{pointer-events:none;opacity:.6}',
    '.slsb-hd{display:flex;align-items:flex-end;gap:20px;padding-bottom:12px;border-bottom:1px solid var(--line)}',
    '.slsb-hd h1{margin:0;font-weight:800;font-size:32px;color:#fff;letter-spacing:4px}',
    '.slsb-hd .sub{color:var(--dim);font-size:14px;padding-bottom:5px}',
    '.slsb .sp{flex:1}',
    '.slsb .x{width:38px;height:38px;border:1px solid #555;background:rgba(0,0,0,.4);color:#ccc;font-size:20px;cursor:pointer;font-family:inherit}',
    '.slsb .x:hover{border-color:var(--gold);color:#fff}',
    '.slsb-body{flex:1;display:flex;gap:18px;min-height:0;padding-top:14px}',
    '.slsb .btn{background:rgba(0,0,0,.45);color:#ddd;border:1px solid #5a5040;padding:8px 16px;font-size:15px;cursor:pointer;letter-spacing:1px;font-family:inherit}',
    '.slsb .btn:hover{border-color:var(--gold);color:#fff}',
    '.slsb .btn.pri{background:rgba(235,163,8,.2);border-color:var(--amber);color:var(--gold);font-weight:800}',
    '.slsb .btn.pri:hover{background:rgba(235,163,8,.35);color:#fff}',
    '.slsb .btn.bad{border-color:rgba(224,106,90,.6);color:#f0a094}',
    '.slsb .btn.sm{padding:4px 10px;font-size:13px}',
    '.slsb-runs{width:460px;display:flex;flex-direction:column;min-height:0}',
    '.slsb-list{flex:1;overflow-y:auto;padding-right:4px}',
    '.slsb-run{padding:10px 12px;margin-bottom:8px;border:1px solid #3a342a;background:var(--panel);cursor:pointer}',
    '.slsb-run:hover{border-color:#7a6a4a}',
    '.slsb-run.on{border-color:var(--gold);background:rgba(40,32,18,.92)}',
    '.slsb-run .nm{color:#fff;font-size:16px;font-weight:800;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    '.slsb-run .l2{color:#bbb;font-size:13px;margin-top:3px}',
    '.slsb-run .l3{color:var(--dim);font-size:12px;margin-top:3px}',
    '.slsb-run.gone{opacity:.75}',
    '.slsb-sec{color:var(--dim);font-size:12px;letter-spacing:2px;margin:14px 0 6px;display:flex;align-items:center;gap:8px}',
    '.slsb-sec::after{content:"";flex:1;height:1px;background:#2e2a22}',
    '.slsb-empty{border:1px dashed #5a5040;padding:14px;text-align:center;color:var(--dim);font-size:14px}',
    '.slsb-foot{margin-top:10px;display:flex;gap:8px;align-items:center;flex-wrap:wrap}',
    '.slsb-foot .disk{flex-basis:100%;font-size:12px;color:var(--dim)}',
    '.slsb .badge{display:inline-block;font-size:11px;padding:1px 7px;border:1px solid;letter-spacing:1px;margin-right:4px;vertical-align:1px}',
    '.slsb .b-end{color:var(--gold);border-color:rgba(255,200,58,.5)}',
    '.slsb .b-endless{color:#c9a4ff;border-color:rgba(201,164,255,.5)}',
    '.slsb .b-warn{color:var(--red);border-color:rgba(224,106,90,.6)}',
    '.slsb .b-gone{color:#aaa;border-color:#666}',
    '.slsb-det{flex:1;display:flex;flex-direction:column;min-width:0;min-height:0;border:1px solid #3a342a;background:var(--panel)}',
    '.slsb-dtop{padding:16px 18px;border-bottom:1px solid var(--line)}',
    '.slsb-dtop h2{margin:0;font-size:24px;color:#fff;font-weight:800}',
    '.slsb .kv{display:flex;flex-wrap:wrap;gap:6px 26px;margin:10px 0 4px;font-size:14px}',
    '.slsb .kv span{color:var(--dim);font-size:12px;display:block}',
    '.slsb .kv b{color:#eee;font-weight:400}',
    '.slsb .acts{display:flex;gap:8px;margin-top:12px;flex-wrap:wrap}',
    '.slsb-warn{margin:12px 18px 0;padding:10px 14px;border-left:3px solid var(--red);background:rgba(224,106,90,.08);font-size:14px;color:#f2c6be;line-height:1.5}',
    '.slsb-warn b{color:#fff}',
    '.slsb-tlhd{padding:12px 18px 4px;font-size:15px;color:var(--gold);letter-spacing:2px}',
    '.slsb-tl{flex:1;overflow-y:auto;padding:0 18px 14px}',
    '.slsb .day{color:var(--dim);font-size:12px;letter-spacing:2px;margin:10px 0 4px;display:flex;align-items:center;gap:8px}',
    '.slsb .day::after{content:"";flex:1;height:1px;background:#2e2a22}',
    '.slsb-pt{display:flex;align-items:center;gap:12px;padding:6px 8px;font-size:14px;border:1px solid transparent}',
    '.slsb-pt:hover{background:rgba(255,255,255,.03);border-color:#2e2a22}',
    '.slsb-pt .tm{width:132px;color:#bbb;font-family:Consolas,monospace;font-size:13px}',
    '.slsb-pt .rs{flex:1;color:#ddd;min-width:0}',
    '.slsb-pt .rs small{color:var(--dim);margin-left:6px}',
    '.slsb-pt .pn{color:var(--gold);font-size:12px}',
    '.slsb-pt .sz{width:70px;text-align:right;color:var(--dim);font-size:12px}',
    '.slsb-modal{position:absolute;inset:0;background:rgba(0,0,0,.72);display:flex;align-items:center;justify-content:center;z-index:5}',
    '.slsb-mbox{width:640px;border:1px solid var(--amber);background:#12100c;box-shadow:0 20px 60px rgba(0,0,0,.8)}',
    '.slsb-mhd{padding:14px 20px;border-bottom:1px solid var(--line);font-size:20px;font-weight:800;color:#fff;letter-spacing:2px}',
    '.slsb-mbd{padding:16px 20px;font-size:15px;line-height:1.6;color:#ccc}',
    '.slsb-mft{padding:12px 20px;border-top:1px solid var(--line);display:flex;gap:10px;justify-content:flex-end}',
    '.slsb-find{display:flex;flex-direction:column;gap:6px;margin-bottom:10px}',
    '.slsb input.q,.slsb input.nt{width:100%;background:rgba(0,0,0,.5);border:1px solid #5a5040;color:#eee;padding:7px 10px;font-size:15px;font-family:inherit;outline:none}',
    '.slsb input.q:focus,.slsb input.nt:focus{border-color:var(--gold)}',
    '.slsb-chips{display:flex;gap:6px;flex-wrap:wrap}',
    '.slsb .chip{background:none;border:1px solid #4a4236;color:#aaa;padding:3px 10px;font-size:13px;cursor:pointer;font-family:inherit}',
    '.slsb .chip.on{border-color:var(--amber);color:var(--gold)}',
    '.slsb-bk{border:1px solid transparent}',
    '.slsb-bk.open{border-color:#3a342a;background:rgba(255,255,255,.02)}',
    '.slsb-bk .slsb-pt{cursor:pointer}',
    '.slsb-bk .nb{padding:0 8px 6px 152px;color:#d8c9a6;font-size:13px;font-style:italic;word-break:break-word}',
    '.slsb-bk .more{display:flex;gap:6px;flex-wrap:wrap;padding:0 8px 8px 152px}',
    '.slsb .b-old{color:#e0a060;border-color:rgba(224,160,96,.6)}',
    '.slsb .dtx{color:#a99c80;font-size:13px;font-style:italic;margin:-2px 0 6px;line-height:1.45;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}',
    '.slsb-mbd .mw{margin-top:10px;padding:8px 12px;border-left:3px solid #e0a060;background:rgba(224,160,96,.08);color:#f0d2b0}',
    '.slsb-mbd .mh{color:var(--dim);font-size:13px;margin-bottom:8px}'
  ].join('');

  var view = null;
  var selected = null;
  var dialog = null;
  var isBusy = false;
  var query = '';
  var filter = 'all';
  var expanded = null;
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

  function t(key) {
    var pair = TEXT[key];
    return pair ? pair[view && view.zh ? 1 : 0] : key;
  }

  function f(key) {
    var text = t(key);
    for (var i = 1; i < arguments.length; i++) text = text.split('{' + (i - 1) + '}').join(String(arguments[i]));
    return text;
  }

  function kindName(kind) {
    var pair = TEXT.kinds[kind];
    return pair ? pair[view && view.zh ? 1 : 0] : kind;
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

  function busy() {
    isBusy = true;
    var box = document.querySelector('#' + ROOT_ID + ' .slsb');
    if (box) box.classList.add('busy');
  }

  function ensureStyle() {
    if (document.getElementById(ROOT_ID + '-css')) return;
    var style = el('style');
    style.id = ROOT_ID + '-css';
    style.textContent = CSS;
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
    var text = [run.name, run.stem, run.role, run.diff, run.status].join(' ').toLowerCase();
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

  function finder() {
    var box = el('div', 'slsb-find');
    var input = el('input', 'q');
    input.type = 'text';
    input.placeholder = t('search');
    input.value = query;
    input.maxLength = 60;
    input.spellcheck = false;
    watchTyping(input);
    input.addEventListener('input', function () {
      query = input.value;
      var list = document.querySelector('#' + ROOT_ID + ' .slsb-list');
      if (list) fillList(list);
    });
    input.addEventListener('keydown', function (e) {
      if (e.key === 'Escape') {
        pageEsc(e);
        if (query) {
          query = input.value = '';
          var list = document.querySelector('#' + ROOT_ID + ' .slsb-list');
          if (list) fillList(list);
        } else {
          input.blur();
        }
      } else if (e.key === 'Enter') {
        input.blur();
      }
    });
    box.appendChild(input);

    var chips = el('div', 'slsb-chips');
    ['all', 'active', 'finished', 'backed'].forEach(function (key) {
      var chip = el('button', 'chip' + (filter === key ? ' on' : ''), TEXT.filters[key][view.zh ? 1 : 0]);
      chip.type = 'button';
      chip.addEventListener('click', function () {
        if (isBusy) return;
        filter = key;
        draw();
      });
      chips.appendChild(chip);
    });
    box.appendChild(chips);
    return box;
  }

  function findSelected() {
    var all = allRuns();
    for (var i = 0; i < all.length; i++) if (all[i].run.stem === selected) return all[i];
    selected = all.length ? all[0].run.stem : null;
    return all.length ? all[0] : null;
  }

  function runCard(entry) {
    var run = entry.run;
    var card = el('div', 'slsb-run' + (run.stem === selected ? ' on' : '') + (entry.isGone ? ' gone' : ''));
    var name = el('div', 'nm');

    if (entry.isGone) name.appendChild(el('span', 'badge b-gone', t('deleted')));
    if (run.behind) name.appendChild(el('span', 'badge b-warn', t('behind')));
    if (run.finished) name.appendChild(el('span', 'badge b-end', t('finished')));
    if (run.endless) name.appendChild(el('span', 'badge b-endless', t('endless')));
    name.appendChild(document.createTextNode(run.name || run.stem));
    card.appendChild(name);

    var line = [where(run)];
    if (run.role) line.unshift(run.role);
    if (run.diff) line.push(run.diff);
    card.appendChild(el('div', 'l2', line.join('  /  ')));

    var count = (run.backups || []).length;
    var last = count ? run.backups[0].at : '';
    card.appendChild(el('div', 'l3', t('backups') + ': ' + count + (last ? '  /  ' + last : '')));
    card.addEventListener('click', function () {
      if (isBusy) return;
      selected = run.stem;
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
    var name = run.name || run.stem;
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

  function backupRow(run, isGone, backup) {
    var key = run.stem + '/' + backup.id;
    var isOpen = expanded === key;
    var wrap = el('div', 'slsb-bk' + (isOpen ? ' open' : ''));
    var row = el('div', 'slsb-pt');
    row.appendChild(el('div', 'tm', backup.at));

    var what = el('div', 'rs', kindName(backup.kind));
    what.appendChild(el('small', null, backup.progress ? where(backup) : t('safetyNote')));
    if (backup.pinned) {
      what.appendChild(document.createTextNode(' '));
      what.appendChild(el('span', 'pn', t('pinned')));
    }
    if (backup.oldVersion) {
      what.appendChild(document.createTextNode(' '));
      var old = el('span', 'badge b-old', t('oldVersion'));
      old.title = f('oldVersionTip', backup.version, view.version);
      what.appendChild(old);
    }
    row.appendChild(what);
    row.appendChild(el('div', 'sz', size(backup.bytes)));

    function toggle() {
      if (isBusy) return;
      expanded = isOpen ? null : key;
      draw();
    }

    row.appendChild(button(t('restore'), 'sm pri', function () { restoreStep(run, isGone, backup); }));
    row.appendChild(button(isOpen ? t('less') : t('more'), 'sm', toggle));
    row.addEventListener('click', toggle);
    wrap.appendChild(row);

    if (backup.note) wrap.appendChild(el('div', 'nb', backup.note));

    if (isOpen) {
      var more = el('div', 'more');
      if (backup.progress) {
        more.appendChild(button(t('copy'), 'sm', function () {
          confirmStep(t('copyTitle'), f('copyBody', run.name || run.stem, where(backup), backup.at), t('copy'), 'pri', ['copy', run.stem, backup.id], oldVersionWarn(backup));
        }));
      }
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
      wrap.appendChild(more);
    }
    return wrap;
  }

  function detail(entry) {
    var det = el('div', 'slsb-det');
    if (!entry) {
      det.appendChild(el('div', 'slsb-tl slsb-empty', t('noRuns')));
      return det;
    }

    var run = entry.run;
    var top = el('div', 'slsb-dtop');
    top.appendChild(el('h2', null, run.name || run.stem));

    var kv = el('div', 'kv');
    kv.appendChild(info(t('progress'), where(run)));
    if (run.role) kv.appendChild(info(t('role'), run.role));
    if (run.time) kv.appendChild(info(t('time'), run.time));
    if (run.diff) kv.appendChild(info(t('difficulty'), run.diff));
    if (run.status) kv.appendChild(info(t('status'), run.status));
    top.appendChild(kv);

    var acts = el('div', 'acts');
    if (!entry.isGone) {
      acts.appendChild(button(t('load'), 'pri', function () { busy(); post(['load', run.file]); }));
      acts.appendChild(button(t('backUp'), '', function () { busy(); post(['backup', run.stem]); }));
      acts.appendChild(button(t('del'), 'bad', function () {
        confirmStep(t('deleteTitle'), f('deleteBody', run.name || run.stem), t('del'), 'bad', ['delete', run.stem]);
      }));
    }
    top.appendChild(acts);
    det.appendChild(top);

    if (run.behind) {
      var backup = null;
      (run.backups || []).forEach(function (b) { if (b.id === run.behind.id) backup = b; });
      var warn = el('div', 'slsb-warn');
      warn.appendChild(el('b', null, t('behind') + '. '));
      warn.appendChild(document.createTextNode(f('behindBody', where(run), backup ? backup.at : run.behind.id, where(run.behind))));
      var wacts = el('div', 'acts');
      if (backup) {
        wacts.appendChild(button(t('restore'), 'sm pri', function () {
          restoreStep(run, false, backup);
        }));
      }
      wacts.appendChild(button(t('dismiss'), 'sm', function () { busy(); post(['dismiss', run.stem]); }));
      warn.appendChild(wacts);
      det.appendChild(warn);
    }

    det.appendChild(el('div', 'slsb-tlhd', t('backups')));
    var tl = el('div', 'slsb-tl');
    var backups = run.backups || [];
    if (!backups.length) {
      tl.appendChild(el('div', 'slsb-empty', f('noBackups', view.key ? f('keyHint', view.key) : '')));
    }
    var lastGroup = null;
    var hasDayText = false;
    backups.forEach(function (b) {
      var group = b.progress ? f('loop', b.turn) + '  /  ' + f('day', b.day) : b.at.slice(0, 10);
      if (group !== lastGroup) {
        tl.appendChild(el('div', 'day', group));
        lastGroup = group;
        hasDayText = false;
      }
      // The game's own words for the day, from the first backup of that day that kept them.
      if (!hasDayText && b.dayText) {
        var dtx = el('div', 'dtx', b.dayText);
        dtx.title = b.dayText;
        tl.appendChild(dtx);
        hasDayText = true;
      }
      tl.appendChild(backupRow(run, entry.isGone, b));
    });
    det.appendChild(tl);
    return det;
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

  function draw() {
    var root = document.getElementById(ROOT_ID);
    if (!view) return;
    ensureStyle();
    if (!root) {
      root = el('div');
      root.id = ROOT_ID;
      (document.body || document.documentElement).appendChild(root);
    }

    var scrollTop = 0;
    var runsTop = 0;
    var oldList = root.querySelector('.slsb-tl');
    if (oldList) scrollTop = oldList.scrollTop;
    var oldRuns = root.querySelector('.slsb-list');
    if (oldRuns) runsTop = oldRuns.scrollTop;
    var active = document.activeElement;
    var wasSearching = !!active && !!active.classList && active.classList.contains('q') && isTextBox(active);
    var caret = wasSearching ? active.selectionStart : 0;
    var hadNote = !!root.querySelector('input.nt');
    root.textContent = '';

    var box = el('div', 'slsb' + (isBusy ? ' busy' : ''));
    var hd = el('div', 'slsb-hd');
    hd.appendChild(el('h1', null, t('title')));
    hd.appendChild(el('div', 'sub', t('sub')));
    hd.appendChild(el('div', 'sp'));
    var x = el('button', 'x', 'X');
    x.type = 'button';
    x.addEventListener('click', function () { if (!isBusy) post(['close']); });
    hd.appendChild(x);
    box.appendChild(hd);

    var body = el('div', 'slsb-body');
    var left = el('div', 'slsb-runs');
    var list = el('div', 'slsb-list');
    var entry = findSelected();

    left.appendChild(finder());
    fillList(list);
    left.appendChild(list);

    var foot = el('div', 'slsb-foot');
    foot.appendChild(button(t('gameList'), '', function () { busy(); post(['gamelist']); }));
    foot.appendChild(button(t('folder'), '', function () { busy(); post(['folder']); }));
    foot.appendChild(button(t('importBtn'), '', function () { busy(); post(['import']); }));
    foot.appendChild(el('div', 'disk', f('used', size(view.bytes || 0))));
    left.appendChild(foot);

    body.appendChild(left);
    body.appendChild(detail(entry));
    box.appendChild(body);
    var dlg = dialog ? modal() : null;
    if (dlg) box.appendChild(dlg);
    root.appendChild(box);

    var newList = root.querySelector('.slsb-tl');
    if (newList) newList.scrollTop = scrollTop;
    var newRuns = root.querySelector('.slsb-list');
    if (newRuns) newRuns.scrollTop = runsTop;

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
      expanded = null;
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
