using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using ExiledToolkit;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using HarmonyLib;
using SaveBackups.Adapters;
using SaveBackups.Core;
using UnityEngine;

namespace SaveBackups;

[BepInPlugin(Guid, "Save Backups", Version)]
[BepInDependency(ToolkitGuid, ToolkitVersion)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.savebackups";
    private const string Version = "0.4.3";
    private const long Megabyte = 1024L * 1024L;

    /// <summary>Exiled's Toolkit: its shared helpers, toasts, live settings, typing watch and pause menu buttons all come from it.</summary>
    private const string ToolkitGuid = "com.dev.survivallog.exiledtoolkit";
    private const string ToolkitVersion = ">=0.19.0";

    /// <summary>The name the page script hands its window to the Toolkit's UI size with.</summary>
    private const string UiSizeName = "SaveBackups";

    public override void Load()
    {
        ModUiSize.Bind(Config, UiSizeName, description:
            "How big the save manager is drawn, in percent, from 75 to 200. At 0 it follows the UI size set in Exiled's Toolkit (its UiScale). The save manager nearly fills the screen, so past about 105 it is drawn only as big as still fits.\n" +
            "存档管理的显示大小（百分比），范围 75 到 200。设为 0 时跟随 Exiled's Toolkit 的界面大小设置（它的 UiScale）。存档管理几乎占满屏幕，所以超过 105 左右后只会以能完整显示的最大尺寸显示。");

        var backupKey = Config.Bind("Controls", "BackupKey", KeyCode.None,
            "Key that saves the run you are playing and backs it up, like the Back up now button in the pause menu. It works only in a run, when the game could save. None, the default, leaves it unbound.\n" +
            "在游戏中按下此键会先保存当前存档，再备份它，与暂停菜单里的「立即备份」按钮相同。只在可以存档的时候有效。默认为 None，即不绑定按键。");
        var folder = Config.Bind("Backups", "Folder", "",
            "Folder the backups are kept in. Empty, the default, keeps them in the game's own folder (LocalLow/LLS/SLGame/SaveBackups), next to Saves but out of Steam Cloud.\n" +
            "备份存放的文件夹。留空（默认）则放在游戏自己的文件夹（LocalLow/LLS/SLGame/SaveBackups），与 Saves 相邻，但不会被 Steam 云同步。");
        var secondFolder = Config.Bind("Backups", "SecondFolder", "",
            "A second folder that gets a copy of every backup, such as another drive or a synced folder. Empty, the default, keeps one copy.\n" +
            "第二个文件夹，每个备份都会再复制一份到这里，例如另一块硬盘或同步文件夹。留空（默认）则只保留一份。");
        var onMorning = Config.Bind("Backups", "OnMorning", true,
            "Back up the run each morning, right after the game's own save.\n" +
            "每天早上游戏自动存档后，备份当前存档。");
        var onQuit = Config.Bind("Backups", "OnQuit", true,
            "Back up the run when you quit to the title screen or close the game.\n" +
            "返回标题画面或关闭游戏时，备份当前存档。");
        var beforeRebirth = Config.Bind("Backups", "BeforeRebirth", true,
            "Back up the run right before a rebirth rolls it back.\n" +
            "在重生让存档回退之前，备份当前存档。");
        var beforeExpedition = Config.Bind("Backups", "BeforeExpedition", true,
            "Back up the run right before the survivor sets out on an expedition. The game saves first when it can.\n" +
            "幸存者出发探索之前，备份当前存档。可以存档时会先让游戏存档。");
        var beforeWave = Config.Bind("Backups", "BeforeWave", true,
            "Back up the run right before a zombie wave starts, once a day. The game saves first when it can.\n" +
            "尸潮开始之前备份当前存档，每天一次。可以存档时会先让游戏存档。");
        var atEnding = Config.Bind("Backups", "AtEnding", true,
            "Back up the run when it reaches an ending and the game saved your choice, so you can restore it and try another ending.\n" +
            "存档到达结局、游戏保存你的选择时备份，之后可以恢复它去尝试其他结局。");
        var onTimer = Config.Bind("Backups", "OnTimer", false,
            "Also back up the run after every TimerMinutes minutes of play. The game saves first.\n" +
            "每游玩 TimerMinutes 分钟也备份一次当前存档。备份前会先让游戏存档。");
        var timerMinutes = Config.Bind("Backups", "TimerMinutes", 20,
            new ConfigDescription(
                "Minutes of play between timer backups, when OnTimer is on.\n" +
                "开启 OnTimer 时，两次定时备份之间的游玩分钟数。",
                new AcceptableValueRange<int>(5, 240)));
        var takePictures = Config.Bind("Backups", "TakePictures", true,
            "Take a picture of the game with each backup made while you play, shown in the save manager.\n" +
            "游玩时每次备份都会拍一张游戏画面，在存档管理中显示。");
        var verify = Config.Bind("Backups", "Verify", true,
            "Read each backup back and compare it with the save before it counts as made. A copy that does not match is not kept.\n" +
            "每次备份后都会读回并与存档比对，一致才算完成。不一致的副本不会保留。");
        var keepMorning = Keep(Config, "Morning", 10, "morning backups", "早上的备份");
        var keepQuit = Keep(Config, "Quit", 5, "backups made when you quit", "退出时的备份");
        var keepRebirth = Keep(Config, "Rebirth", 5, "backups made before a rebirth", "重生前的备份");
        var keepManual = Keep(Config, "Manual", 0, "backups you made yourself", "手动备份");
        var keepSafety = Keep(Config, "Safety", 5, "backups made before a restore or a delete", "恢复或删除前的备份");
        var keepExpedition = Keep(Config, "Expedition", 5, "backups made before an expedition", "探索前的备份");
        var keepWave = Keep(Config, "Wave", 5, "backups made before a zombie wave", "尸潮前的备份");
        var keepEnding = Keep(Config, "Ending", 3, "backups made at an ending", "结局时的备份");
        var keepTimer = Keep(Config, "Timer", 6, "timer backups", "定时备份");
        var keepAllDays = Config.Bind("Keep", "KeepAllDays", 3,
            "Every backup of the last this many days of play is kept. Older automatic backups are thinned to one a day (see DailyDays). 0 turns thinning off.\n" +
            "最近这么多个游戏日的备份全部保留。更早的自动备份会精简为每天一个（见 DailyDays）。设为 0 表示不精简。");
        var dailyDays = Config.Bind("Keep", "DailyDays", 14,
            "After KeepAllDays, one backup a day is kept for this many more days of play; older ones keep one per loop when PerLoop is on. Thinned backups go to the trash.\n" +
            "超过 KeepAllDays 之后，再往前这么多个游戏日每天保留一个备份；更早的在开启 PerLoop 时每个轮回保留一个。被精简的备份会进入回收站。");
        var perLoop = Config.Bind("Keep", "PerLoop", true,
            "Past DailyDays, keep the last backup of each loop. False lets the per-kind limits take them.\n" +
            "超过 DailyDays 后，保留每个轮回的最后一个备份。设为 false 则交给各类备份的数量上限处理。");
        var maxTotal = Config.Bind("Keep", "MaxTotalMB", 2048,
            "Most space all backups and the trash together may take, in MB. Past it, the trash is emptied oldest first, then the oldest backups go, automatic ones before your own. Pinned backups and each run's newest backup are always kept. 0 means no limit.\n" +
            "所有备份和回收站合计最多占用的空间（MB）。超出后先从最旧的开始清空回收站，再删除最旧的备份，自动备份先于手动备份。已固定的备份和每个存档最新的备份始终保留。设为 0 表示不限制。");
        var binDays = Config.Bind("Keep", "TrashDays", 30,
            "Days a removed backup, or a deleted save's backups, stay in the trash before they are deleted for good. 0 keeps them until you empty the trash.\n" +
            "被移除的备份以及已删除存档的备份在回收站中保留的天数，之后永久删除。设为 0 表示一直保留，直到手动清空回收站。");
        var replaceLoadScreen = Config.Bind("Screen", "ReplaceLoadScreen", true,
            "The title screen's Load button opens the save manager, with every save's backups. Its Default Load screen button opens the game's own list. False keeps the game's list.\n" +
            "标题画面的「读取存档」按钮会打开存档管理，显示每个存档的备份。其中的「默认读取画面」按钮可打开游戏原本的列表。设为 false 则使用游戏原本的列表。");
        var warnWhenBehind = Config.Bind("Screen", "WarnWhenBehind", true,
            "Warn on the title screen when a save is older than its last backup, as when Steam Cloud put back an older copy.\n" +
            "当某个存档比它最近的备份更旧时（例如 Steam 云同步换回了旧存档），在标题画面提醒。");
        var showGameSnapshots = Config.Bind("Screen", "ShowGameSnapshots", true,
            "Show the game's own day snapshots of a save in the save manager, where each can be copied out as a new save.\n" +
            "在存档管理中显示游戏自带的每日快照，每个快照都可以复制为新存档。");
        var autoToasts = Config.Bind("Toasts", "AutoBackupToasts", false,
            "Show a message for every automatic backup, not only for the ones you make.\n" +
            "每次自动备份都显示提示，而不仅是手动备份。");

        var log = new ModLog(Log, "save-backups.log");
        var hooks = new PatchKit(new Harmony(Guid), log.Write);
        var worker = new DiskWorker("SaveBackups disk");

        Settings Read() => new(
            string.IsNullOrWhiteSpace(folder.Value) ? Path.Combine(Application.persistentDataPath, "SaveBackups") : folder.Value.Trim(),
            onMorning.Value,
            onQuit.Value,
            beforeRebirth.Value,
            beforeExpedition.Value,
            beforeWave.Value,
            atEnding.Value,
            replaceLoadScreen.Value,
            autoToasts.Value,
            new Limits(
                Math.Max(0, keepMorning.Value),
                Math.Max(0, keepQuit.Value),
                Math.Max(0, keepRebirth.Value),
                Math.Max(0, keepManual.Value),
                Math.Max(0, keepSafety.Value),
                Math.Max(0, keepExpedition.Value),
                Math.Max(0, keepWave.Value),
                Math.Max(0, keepEnding.Value),
                Math.Max(0, keepTimer.Value),
                Math.Max(0, maxTotal.Value) * Megabyte) { Thin = new Thinning(Math.Max(0, keepAllDays.Value), Math.Max(0, dailyDays.Value), perLoop.Value) })
        {
            FolderSetting = folder.Value.Trim(),
            OnTimer = onTimer.Value,
            TimerMinutes = Math.Clamp(timerMinutes.Value, 5, 240),
            SecondFolder = secondFolder.Value.Trim(),
            TakePictures = takePictures.Value,
            Verify = verify.Value,
            WarnWhenBehind = warnWhenBehind.Value,
            ShowGameSnapshots = showGameSnapshots.Value,
            BinDays = Math.Max(0, binDays.Value),
        };

        // The Settings tab writes through the cfg entries, so a change there is the same as an edit in the file.
        string? SetSetting(string id, string value)
        {
            var key = SettingKeys.Find(id);

            if (key == null) return "unknown setting " + id;
            if (SettingKeys.Normalize(key, value) is not { } normal) return "not a value for " + id + ": " + value;

            if (key.Type == SettingType.Key)
            {
                if (!Enum.TryParse<KeyCode>(normal, true, out var code)) return "not a key: " + value;

                normal = code.ToString();
            }

            if (!Config.ContainsKey(new ConfigDefinition(key.Section, key.Key))) return "unknown setting " + id;

            try
            {
                Config[new ConfigDefinition(key.Section, key.Key)].SetSerializedValue(normal);

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        IReadOnlyDictionary<string, string> SettingValues()
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var key in SettingKeys.All)
            {
                var definition = new ConfigDefinition(key.Section, key.Key);

                if (Config.ContainsKey(definition)) values[key.Id] = Config[definition].GetSerializedValue();
            }

            return values;
        }

        var session = new Session(new GameSaves(), new WebPage(log.Write), worker, Read(), log.Write, SetSetting, SettingValues);

        log.Write($"===== Save Backups {Version} started");
        Hooks.Session = session;
        Hooks.Apply(hooks);
        LiveSettings.Watch(backupKey, key =>
        {
            session.UseKey(key);
            log.Write($"backup key {key}");
        });
        LiveSettings.Watch(() => session.UseSettings(Read()),
            folder, secondFolder, onMorning, onQuit, beforeRebirth, beforeExpedition, beforeWave, atEnding, onTimer, timerMinutes, takePictures, verify,
            keepMorning, keepQuit, keepRebirth, keepManual, keepSafety, keepExpedition, keepWave, keepEnding, keepTimer, keepAllDays, dailyDays, perLoop, maxTotal, binDays,
            replaceLoadScreen, warnWhenBehind, showGameSnapshots, autoToasts);
        session.Start();
        AddPauseButtons(session);
        Ticker.Session = session;
        Ticker.Log = log.Write;
        Ticker.Quit = log.Flush;
        AddComponent<Ticker>();
    }

    /// <summary>
    /// Backups of this run and Back up now, at the bottom of the pause menu while a run is open. Exiled's Toolkit places
    /// them with every other mod's menu buttons, so the menu fits on screen however many there are.
    /// </summary>
    private static void AddPauseButtons(Session session)
    {
        static bool IsInRun() => GameSaves.CurrentFile() != null;

        ExiledToolkit.GameMenu.Add(new ExiledToolkit.GameMenuButton("savebackups.open", "Backups of this run", ExiledToolkit.GameMenuPlace.PauseBottom, session.OpenInRun)
        {
            ChineseLabel = "本局备份",
            Order = 200,
            Accent = Accent,
            IsShown = IsInRun,
        });
        ExiledToolkit.GameMenu.Add(new ExiledToolkit.GameMenuButton("savebackups.now", "Back up now", ExiledToolkit.GameMenuPlace.PauseBottom, session.BackUpFromPauseMenu)
        {
            ChineseLabel = "立即备份",
            Order = 210,
            Accent = Accent,
            IsShown = IsInRun,
        });
    }

    private const string Accent = "#8fc7bf";

    private static ConfigEntry<int> Keep(ConfigFile config, string kind, int keep, string what, string whatZh) =>
        config.Bind("Keep", kind, keep,
            $"How many {what} each save keeps. Older ones go to the trash when a new one is made; pinned backups do not count. 0 means keep them all.\n" +
            $"每个存档保留多少个{whatZh}。新备份完成后，更旧的会进入回收站，已固定的备份不计入。设为 0 表示全部保留。");
}
