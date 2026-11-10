using System;
using System.Collections.Generic;

namespace CGD.DevTools
{
    // Who is winning the AI test arena since the last reset: per agent its kills, deaths,
    // hits, damage dealt and taken, abilities used and how fast its kills go down. Agents are
    // numbered as the spectator numbers them; damage from anyone else (the player, hazards)
    // is gathered under Others. Plain bookkeeping on game time — MatchStatsRecorder feeds it.
    public sealed class MatchStats
    {
        public const int Nobody = -1;

        private readonly List<AgentTally> _agents = new();

        public MatchStats(int agentCount)
        {
            Grow(agentCount);
        }

        public int        Count  => _agents.Count;
        public AgentTally Others { get; } = new();
        // Game time the match (re)started at.
        public float      StartedAt { get; private set; }

        public AgentTally this[int agent] => _agents[agent];

        // Room for more agents (the roster grew); never shrinks, so numbers stay put.
        public void Grow(int agentCount)
        {
            while (_agents.Count < agentCount) _agents.Add(new AgentTally());
        }

        public float Duration(float now) => Math.Max(0f, now - StartedAt);

        public void Reset(float now)
        {
            StartedAt = now;
            foreach (AgentTally tally in _agents) tally.Clear();
            Others.Clear();
        }

        // A hit that landed. dealer / target are agent numbers, or Nobody for anyone else.
        public void Hit(int dealer, int target, float amount, bool killed, float now)
        {
            if (amount <= 0f && !killed) return;

            AgentTally from = Tally(dealer);
            AgentTally to   = Tally(target);
            from.Hits++;
            from.DamageDealt += amount;
            to.DamageTaken   += amount;
            if (to.UnderFireSince < 0f) to.UnderFireSince = now;
            if (!killed) return;

            to.Deaths++;
            if (dealer != target) from.Kills++;
            if (dealer != Nobody && dealer != target) from.AddKillTime(now - to.UnderFireSince);
            to.UnderFireSince = -1f;   // its next life starts untouched
        }

        public void AbilityUsed(int agent) => Tally(agent).AbilityUses++;

        private AgentTally Tally(int agent) => agent >= 0 && agent < _agents.Count ? _agents[agent] : Others;
    }
}
