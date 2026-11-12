using System.Collections.Generic;

namespace CGD.Enemies
{
    // An enemy's live status for the Behavior Tree Editor (IBtAgentStatus) and the spectator's
    // inspector (EnemyInsight): what it is doing, what it believes, its squad job, what the
    // director currently allows it and how often it thinks — the answers to "why is it hanging
    // back?". Read a few times a second by debug views; nothing in a frame depends on it.
    internal static class EnemyStatusReport
    {
        public static void Describe(EnemyAI ai, List<(string Label, string Value)> rows)
        {
            rows.Add(("State", ai.Step == EngageStep.None ? ai.State.ToString() : $"{ai.State} · {ai.Step}"));
            rows.Add(("Awareness", $"{ai.Awareness} ({ai.AwarenessValue:0.00}){(ai.IsCautious ? " · on edge" : "")}"));
            if (ai.Perception.Target != null)
                rows.Add(("Target", $"{ai.Perception.Target.name} · last seen {ai.Perception.TargetMotion} · " +
                                    $"{ai.Perception.TargetHealthShare:P0} strength{(ai.Perception.IsTargetDistracted ? " · distracted" : "")}"));
            rows.Add(("Morale", $"{ai.MoraleValue:0.00}{(ai.Morale.IsShaken ? " · shaken" : "")}{(ai.Tells.IsCommitting ? " · taking it in" : "")}"));
            rows.Add(("Doctrine", ai.Data.Doctrine != null ? ai.Data.Doctrine.name : "none"));
            if (ai.Squad != null)
                rows.Add(("Squad", $"{(ai.Squad.Leader == ai ? "leader · " : "")}{ai.Squad.Members.Count} members"));
            if (ai.Encounter != null)
            {
                Encounter encounter = ai.Encounter;
                rows.Add(("Encounter", $"{ai.SquadRole}{(encounter.Lead == ai ? " · lead" : "")}{(encounter.IsSteppedUp(ai) ? " · stepped up" : "")}" +
                                       $" · {encounter.Posture} ({encounter.Members.Count} on {(encounter.Target != null ? encounter.Target.name : "?")})"));
            }
            if (Overseers.TryGet(ai.Doctrine, ai.Health.Team, out FactionOverseer overseer))
                rows.Add(("Overseer", $"menace {overseer.MenaceShare:P0}{(overseer.IsBackingOff ? " · backing off" : "")} · escalation {overseer.Escalation}"));
            if (ai.Tactics.HasMeleeTurn) rows.Add(("Melee turn", "has it"));

            EnemyPacingState pacing = EnemyPacing.Current;
            rows.Add(("Director", pacing.IsDirected
                ? $"{pacing.Phase} · pressure {pacing.Pressure:0.00} · player stress {pacing.Intensity:0.00}"
                : "none (unscaled, unlimited permits)"));
            if (pacing.IsDirected)
                rows.Add(("Bold moves", $"{BoldMovePermits.InUse} / {pacing.BoldMoves} in use{(BoldMovePermits.Holds(ai) ? " · holds one" : "")}"));

            float interval = ai.TreeTickInterval();
            rows.Add(("Thinks", interval <= 0f ? "every frame" : $"every {interval:0.##} s (calm)"));
        }
    }
}
