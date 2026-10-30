using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using Il2CppInterop.Runtime;

namespace SaveBackups.Adapters;

/// <summary>
/// The game moments a backup is made at, and the Load button. Static because a Harmony patch has nowhere else to
/// look. Every hook fails soft: a failure is logged once and the game carries on as if the mod were not there.
/// </summary>
internal static class Hooks
{
    internal static Session? Session;
    private static Action<string> _log = _ => { };
    private static bool _hasReportedFailure;

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;
        Patch(harmony, typeof(CountUpTimer), "CaptureRebirthDaySnapshot", null, nameof(AfterMorningSave), "morning backups are not made");
        Patch(harmony, typeof(GameSaveManager), "BattleToMain", nameof(BeforeLeaveRun), nameof(AfterLeaveRun), "backups on quit to title are not made");
        Patch(harmony, typeof(GameSaveManager), "OnApplicationQuitInternal", nameof(BeforeLeaveRun), nameof(AfterGameQuit), "backups on game quit are not made");
        Patch(harmony, typeof(GameSaveManager), "TryBeginMinorRebirth", nameof(BeforeRebirth), null, "backups before a rebirth are not made");
        Patch(harmony, typeof(ExploreManager), "OnCheckAndGo", nameof(BeforeExpedition), null, "backups before an expedition are not made");
        Patch(harmony, typeof(TowerWaveManager), "StartWave", nameof(BeforeWave), null, "backups before a zombie wave are not made");

        // The ending choice is saved inside each ending line's confirm step (MarkPendingEndingChoice cannot be hooked).
        Patch(harmony, typeof(LineEndingManager), "OnEndingEventConfirmed", nameof(BeforeEndingConfirm), nameof(AfterEndingConfirm), "backups at an ending are not made");
        Patch(harmony, typeof(GirlLineEndingManager), "OnEndingEventConfirmed", nameof(BeforeEndingConfirm), nameof(AfterEndingConfirm), "backups at the girl line's ending are not made");
        Patch(harmony, typeof(WarehouseLineEndingManager), "OnEndingEventConfirmed", nameof(BeforeEndingConfirm), nameof(AfterEndingConfirm), "backups at the warehouse line's ending are not made");
        // RA_LoadGame is compiled into Trigger_RA_LoadGame, so a patch on RA_LoadGame itself never runs.
        Patch(harmony, typeof(Reducer_Web_MainUI), "Trigger_RA_LoadGame", nameof(OnLoadButton), null, "the Load button opens the game's own list");
    }

    private static void Patch(Harmony harmony, Type type, string method, string? prefix, string? postfix, string without)
    {
        try
        {
            harmony.Patch(
                AccessTools.Method(type, method),
                prefix: prefix == null ? null : new HarmonyMethod(typeof(Hooks), prefix),
                postfix: postfix == null ? null : new HarmonyMethod(typeof(Hooks), postfix));
        }
        catch (Exception ex)
        {
            _log($"could not hook {type.Name}.{method}, {without}: {ex.Message}");
        }
    }

    private static void AfterMorningSave() => Guard(() => Session?.MorningSaved());

    private static void BeforeLeaveRun() => Guard(() => Session?.LeavingRun());

    private static void AfterLeaveRun() => Guard(() => Session?.LeftRun(wait: false));

    private static void AfterGameQuit() => Guard(() => Session?.LeftRun(wait: true));

    private static void BeforeRebirth() => Guard(() => Session?.RebirthStarting());

    private static void BeforeExpedition() => Guard(() => Session?.ExpeditionStarting());

    private static void BeforeWave() => Guard(() => Session?.WaveStarting());

    /// <summary>The ending choice the save holds before the confirm, so the postfix can tell whether this confirm marked one.</summary>
    private static void BeforeEndingConfirm(out int __state)
    {
        __state = 0;

        try
        {
            __state = GameSaves.PendingEndingId();
        }
        catch (Exception ex)
        {
            ReportOnce(ex);
        }
    }

    private static void AfterEndingConfirm(int __state) => Guard(() => Session?.EndingConfirmed(__state));

    /// <summary>False, so the game's list stays shut, when the mod's own manager opened instead.</summary>
    private static bool OnLoadButton()
    {
        try
        {
            var state = Session == null ? null : MainScreen();

            return state == null || !Session!.OpenInsteadOfGameList(state);
        }
        catch (Exception ex)
        {
            ReportOnce(ex);

            return true;
        }
    }

    private static Il2CppSystem.Type? _mainScreenType;

    private static State_Web_MainUI? MainScreen() =>
        GameSingleton.Existing<ReduxUISystem>()?.GetState<State_Web_MainUI>(_mainScreenType ??= Il2CppType.Of<State_Web_MainUI>());

    private static void Guard(Action step)
    {
        try
        {
            step();
        }
        catch (Exception ex)
        {
            ReportOnce(ex);
        }
    }

    private static void ReportOnce(Exception ex)
    {
        if (_hasReportedFailure) return;

        _hasReportedFailure = true;
        _log("a save hook failed, the game carried on without the mod (reported once): " + ex);
    }
}
