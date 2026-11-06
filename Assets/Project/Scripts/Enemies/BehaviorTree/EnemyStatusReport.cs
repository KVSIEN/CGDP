using System.Collections.Generic;

namespace CGD.Enemies
{
    // An enemy's live status for the Behavior Tree Editor (IBtAgentStatus): what it is doing,
    // what it believes, its squad job, what the director currently allows it and how often it
    // thinks — the answers to "why is it hanging back?". Editor-only reading; never in a frame.
    internal static class EnemyStatusReport
    {
        public static void Describe(EnemyAI ai, List<(string Label, string Value)> rows)
        {
            rows.Add(("State", ai.Step == EngageStep.None ? ai.State.ToString() : $"{ai.State} · {ai.Step}"));
            rows.Add(("Awareness", $"{ai.Awareness} ({ai.AwarenessValue:0.00}){(ai.IsCautious ? " · on edge" : "")}"));
            rows.Add(("Morale", $"{ai.MoraleValue:0.00}{(ai.Morale.IsShaken ? " · shaken" : "")}"));
            if (ai.Squad != null)
                rows.Add(("Squad", $"{ai.SquadRole}{(ai.Squad.Leader == ai ? " · leader" : "")} ({ai.Squad.Members.Count} members)"));
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
