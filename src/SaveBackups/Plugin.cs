using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using SaveBackups.Adapters;
using SaveBackups.Core;
using UnityEngine;

namespace SaveBackups;

[BepInPlugin(Guid, "Save Backups", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, OptionalLive.ToolkitVersion)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.savebackups";
    private const string Version = "0.2.0";
    private const long Megabyte = 1024L * 1024L;

    public override void Load()
    {
        var backupKey = Config.Bind("Controls", "BackupKey", KeyCode.F8,
            "Key that saves the run you are playing and backs it up. It works only in a run, when the game could save. None turns the key off.\n" +
            "在游戏中按下此键会先保存当前存档，再备份它。只在可以存档的时候有效。设为 None 可关闭此键。");
        var folder = Config.Bind("Backups", "Folder", "",
            "Folder the backups are kept in. Empty, the default, keeps them in the game's own folder (LocalLow/LLS/SLGame/SaveBackups), next to Saves but out of Steam Cloud.\n" +
            "备份存放的文件夹。留空（默认）则放在游戏自己的文件夹（LocalLow/LLS/SLGame/SaveBackups），与 Saves 相邻，但不会被 Steam 云同步。");
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
        var keepMorning = Keep(Config, "Morning", 10, "morning backups", "早上的备份");
        var keepQuit = Keep(Config, "Quit", 5, "backups made when you quit", "退出时的备份");
        var keepRebirth = Keep(Config, "Rebirth", 5, "backups made before a rebirth", "重生前的备份");
        var keepManual = Keep(Config, "Manual", 0, "backups you made yourself", "手动备份");
        var keepSafety = Keep(Config, "Safety", 5, "backups made before a restore or a delete", "恢复或删除前的备份");
        var keepExpedition = Keep(Config, "Expedition", 5, "backups made before an expedition", "探索前的备份");
        var keepWave = Keep(Config, "Wave", 5, "backups made before a zombie wave", "尸潮前的备份");
        var keepEnding = Keep(Config, "Ending", 3, "backups made at an ending", "结局时的备份");
        var maxTotal = Config.Bind("Keep", "MaxTotalMB", 2048,
            "Most space all backups together may take, in MB. Past it, the oldest backups go first, automatic ones before your own. Pinned backups and each run's newest backup are always kept. 0 means no limit.\n" +
            "所有备份合计最多占用的空间（MB）。超出后先删除最旧的备份，自动备份先于手动备份。已固定的备份和每个存档最新的备份始终保留。设为 0 表示不限制。");
        var replaceLoadScreen = Config.Bind("Screen", "ReplaceLoadScreen", true,
            "The title screen's Load button opens the save manager, with every save's backups. Its Default list button opens the game's list. False keeps the game's list.\n" +
            "标题画面的「读取存档」按钮会打开存档管理，显示每个存档的备份。其中的「默认列表」按钮可打开游戏原本的列表。设为 false 则使用游戏原本的列表。");
        var autoToasts = Config.Bind("Toasts", "AutoBackupToasts", false,
            "Show a message for every automatic backup, not only for the ones you make.\n" +
            "每次自动备份都显示提示，而不仅是手动备份。");

        var log = new ModLog(Log, Path.Combine(Paths.BepInExRootPath, "save-backups.log"));
        var harmony = new Harmony(Guid);
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
                Math.Max(0, maxTotal.Value) * Megabyte));

        var session = new Session(new GameSaves(), new WebPage(log.Write), worker, Read(), log.Write);

        log.Write($"===== Save Backups {Version} started");
        PageMessages.Apply(harmony, log.Write);
        HotkeyGate.Apply(harmony, log.Write);
        Hooks.Session = session;
        Hooks.Apply(harmony, log.Write);
        OptionalLive.Watch(backupKey, key =>
        {
            session.UseKey(key);
            log.Write($"backup key {key}");
        });
        OptionalLive.Watch(() => session.UseSettings(Read()), folder, onMorning, onQuit, beforeRebirth, beforeExpedition, beforeWave, atEnding, keepMorning, keepQuit, keepRebirth, keepManual, keepSafety, keepExpedition, keepWave, keepEnding, maxTotal, replaceLoadScreen, autoToasts);
        session.Start();
        Ticker.Session = session;
        Ticker.Log = log.Write;
        Ticker.Quit = log.Finish;
        AddComponent<Ticker>();
    }

    private static ConfigEntry<int> Keep(ConfigFile config, string kind, int keep, string what, string whatZh) =>
        config.Bind("Keep", kind, keep,
            $"How many {what} each save keeps. Older ones are removed when a new one is made; pinned backups do not count. 0 means keep them all.\n" +
            $"每个存档保留多少个{whatZh}。新备份完成后会删除更旧的，已固定的备份不计入。设为 0 表示全部保留。");
}
