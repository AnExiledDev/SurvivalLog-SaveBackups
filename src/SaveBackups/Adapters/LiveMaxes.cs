using System.Diagnostics;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using SaveBackups.Core;

namespace SaveBackups.Adapters;

/// <summary>
/// Keeps the survivor's full most of each meter (base plus every buff, as the game's bars read it) in the open run's
/// counter map, once a second, so the next save the game writes carries it (<see cref="LiveMax"/>).
/// </summary>
internal static class LiveMaxes
{
    private static readonly AttrName[] Maxes = { AttrName.MaxVitality, AttrName.MaxSatiety, AttrName.MaxMorale, AttrName.MaxStamina };
    private static readonly Stopwatch SinceTick = Stopwatch.StartNew();

    public static void Tick()
    {
        if (SinceTick.Elapsed.TotalSeconds < 1) return;

        SinceTick.Restart();

        var role = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetLeadingRole();
        var attributes = role == null ? null : AgentTools.GetAgentComponent<AttributeComponent>(role);

        if (attributes?.AttrDict == null) return;

        foreach (var name in Maxes)
        {
            if (!attributes.AttrDict.ContainsKey(name)) continue;

            var max = attributes.GetTotalValue_Int(name);
            var key = LiveMax.Key((int)name);

            // The counter map belongs to the run that is open now, so a run loaded since is written at once.
            if (max > 0 && GameCounter.GetCounter(key) != max) GameCounter.SetCounter(key, max);
        }
    }
}
