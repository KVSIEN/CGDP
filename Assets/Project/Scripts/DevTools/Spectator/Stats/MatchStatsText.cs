using System.Text;
using CGD.Combat;
using CGD.Enemies;

namespace CGD.DevTools
{
    // The match stats panel: a row per agent in its faction colour (kills, deaths, damage
    // dealt and taken, damage per second over the match, abilities used, average time to
    // kill), a total for any faction with more than one agent, and anyone else's damage.
    internal sealed class MatchStatsText
    {
        private const string Columns = "<pos=34%>K<pos=41%>D<pos=48%>Dealt<pos=61%>Taken<pos=73%>DPS<pos=82%>Abil<pos=91%>TTK";

        private readonly StringBuilder _text = new();
        private readonly AgentTally    _teamTotal = new();

        public string Write(MatchStats stats, EnemyAI[] agents, float now, string scenario, string resetKey)
        {
            float duration = stats.Duration(now);
            _text.Clear();
            _text.Append("<b>MATCH</b>   ").Append(Clock(duration));
            if (!string.IsNullOrEmpty(scenario)) _text.Append("   ").Coloured(InspectorText.Gold, scenario);
            _text.Append("   ").Coloured(InspectorText.Muted, $"{resetKey} resets").Append('\n');
            _text.Append("<size=85%><color=").Append(InspectorText.Muted).Append(">Agent").Append(Columns).Append("</color></size>\n");

            for (var team = Team.None; team <= Team.Void; team++) TeamRows(stats, agents, team, duration);
            if (stats.Others.Hits > 0 || stats.Others.DamageTaken > 0f)
                Row("Others (player…)", null, stats.Others, duration);
            return _text.ToString();
        }

        private void TeamRows(MatchStats stats, EnemyAI[] agents, Team team, float duration)
        {
            int members = 0;
            _teamTotal.Clear();
            for (int i = 0; i < agents.Length; i++)
            {
                if (agents[i] == null || agents[i].Health.Team != team) continue;
                members++;
                AgentTally tally = stats[i];
                Row($"{i + 1}  {agents[i].Health.DisplayName}", OverlayPalette.Hex(OverlayPalette.ForTeam(team)), tally, duration);
                Add(_teamTotal, tally);
            }
            if (members > 1) Row($"<i>{team} total</i>", OverlayPalette.Hex(OverlayPalette.ForTeam(team)), _teamTotal, duration);
        }

        private void Row(string name, string colour, AgentTally tally, float duration)
        {
            if (colour != null) _text.Append("<color=").Append(colour).Append('>');
            _text.Append(name);
            if (colour != null) _text.Append("</color>");
            _text.Append("<pos=34%>").Append(tally.Kills)
                 .Append("<pos=41%>").Append(tally.Deaths)
                 .Append("<pos=48%>").Append(tally.DamageDealt.ToString("0"))
                 .Append("<pos=61%>").Append(tally.DamageTaken.ToString("0"))
                 .Append("<pos=73%>").Append((duration > 0f ? tally.DamageDealt / duration : 0f).ToString("0.0"))
                 .Append("<pos=82%>").Append(tally.AbilityUses)
                 .Append("<pos=91%>");
            if (tally.AverageTimeToKill > 0f) _text.Append(tally.AverageTimeToKill.ToString("0.0")).Append('s');
            else _text.Append('–');
            _text.Append('\n');
        }

        // A faction total: counts add up; time to kill can't be averaged from averages, so it is left out.
        private static void Add(AgentTally total, AgentTally tally)
        {
            total.Kills       += tally.Kills;
            total.Deaths      += tally.Deaths;
            total.Hits        += tally.Hits;
            total.AbilityUses += tally.AbilityUses;
            total.DamageDealt += tally.DamageDealt;
            total.DamageTaken += tally.DamageTaken;
        }

        private static string Clock(float seconds) => $"{(int)(seconds / 60f)}:{(int)(seconds % 60f):00}";
    }
}
