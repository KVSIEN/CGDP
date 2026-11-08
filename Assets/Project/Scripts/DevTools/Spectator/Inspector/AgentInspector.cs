using System.Collections.Generic;
using System.Text;
using CGD.BehaviorTrees;
using CGD.Enemies;

namespace CGD.DevTools
{
    // The inspected agent's panel text, one page at a time (InspectorPage): the overview
    // (status, abilities, whom it weighs up, the tree nodes running now), its blackboard,
    // and its decisions (the tree's recent trace and its latest tactical searches).
    // Records the agent's tree trace while it is inspected, alongside the Behavior Tree
    // Editor if that watches it too, and stops when inspection moves on.
    internal sealed class AgentInspector
    {
        private readonly StringBuilder _text = new();
        private readonly List<(string Label, string Value)> _rows = new();
        private readonly List<(BtNode Node, int Depth)> _running = new();

        private EnemyAI      _agent;
        private BehaviorTree _traced;

        public EnemyAI Agent => _agent;

        public void Inspect(EnemyAI agent)
        {
            if (agent == _agent) return;
            StopTrace();
            _agent = agent;
        }

        public void Clear() => Inspect(null);

        // The page's rich text, or null when there is nothing to show.
        public string Describe(int number, InspectorPage page)
        {
            if (_agent == null || page == InspectorPage.Hidden) return null;
            var insight = new EnemyInsight(_agent);
            if (!insight.IsValid) return null;
            KeepTracing(insight.Tree);

            _text.Clear();
            Header(number, insight, page);
            switch (page)
            {
                case InspectorPage.Blackboard: BlackboardPage.Write(_text, insight.Tree); break;
                case InspectorPage.Decisions:  DecisionsPage.Write(_text, insight, _traced?.Trace); break;
                default:                       Overview(insight); break;
            }
            return _text.ToString();
        }

        private void Header(int number, EnemyInsight insight, InspectorPage page)
        {
            EnemyHealth health = insight.Health;
            _text.Append("<size=120%><b>").Coloured(InspectorText.Gold, number.ToString()).Append("  ")
                 .Append(health.DisplayName).Append("</b></size>   ")
                 .Coloured(InspectorText.Muted, $"{insight.Team} · {insight.Data.name}").Append('\n');

            _text.Append("<size=85%>");
            for (var p = InspectorPage.Overview; p < InspectorPage.Hidden; p++)
            {
                if (p == page) _text.Append("<b>[").Append(p).Append("]</b>  ");
                else _text.Coloured(InspectorText.Muted, p.ToString()).Append("  ");
            }
            _text.Append("</size>\n");

            _text.Append("<color=").Append(InspectorText.Muted).Append(">Health</color><pos=34%>")
                 .Append(health.Health.ToString("0")).Append(" / ").Append(health.MaxHealth.ToString("0"));
            if (health.MaxShield > 0f)
                _text.Append("   ").Coloured(InspectorText.Shield, $"shield {health.Shield:0} / {health.MaxShield:0}");
            _text.Append('\n');
        }

        private void Overview(EnemyInsight insight)
        {
            _rows.Clear();
            insight.DescribeStatus(_rows);
            foreach ((string label, string value) in _rows) _text.Row(label, value);

            OverviewSections.Abilities(_text, insight);
            OverviewSections.Hostiles(_text, insight);
            if (insight.Tree == null) return;
            insight.Tree.CollectRunning(_running);
            OverviewSections.Running(_text, _running);
        }

        // Its tree appears in Start, and a respawn keeps it — so pick it up whenever it changes.
        private void KeepTracing(BehaviorTree tree)
        {
            if (tree == _traced) return;
            StopTrace();
            _traced = tree;
            _traced?.StartTrace();
        }

        private void StopTrace()
        {
            _traced?.StopTrace();
            _traced = null;
        }
    }
}
