using System.Collections.Generic;
using System.Text;
using CGD.Abilities;
using CGD.BehaviorTrees;
using CGD.Enemies;

namespace CGD.DevTools
{
    // The parts of the inspector's overview page below the status rows.
    internal static class OverviewSections
    {
        private const int MaxRunningLines = 16;
        private const int CommentLength   = 26;

        // Each ability its tree has asked about: casting, its buff running, cooling down or ready.
        public static void Abilities(StringBuilder text, EnemyInsight insight)
        {
            IReadOnlyList<Ability> abilities = insight.Abilities;
            if (abilities.Count == 0) return;

            text.Heading("Abilities");
            foreach (Ability ability in abilities)
            {
                text.Append("<color=").Append(InspectorText.Muted).Append('>').Append(ability.name).Append("</color><pos=34%>");
                float effect   = insight.EffectLeft(ability);
                float cooldown = insight.CooldownLeft(ability);
                if (insight.Casting == ability) text.Coloured(InspectorText.Warn, "casting");
                else if (cooldown <= 0f)        text.Coloured(InspectorText.Good, "ready");
                else                            text.Append("cooldown ").Seconds(cooldown);
                if (effect > 0f) text.Append("   <color=").Append(InspectorText.Good).Append(">active ").Seconds(effect).Append("</color>");
                text.Append('\n');
            }
        }

        // The nearest hostiles its senses weighed, with how strongly its priorities favour each.
        public static void Hostiles(StringBuilder text, EnemyInsight insight)
        {
            IReadOnlyList<WeighedHostile> weighed = insight.Weighed;
            if (weighed.Count == 0) return;

            text.Heading("Weighing up");
            foreach (WeighedHostile hostile in weighed)
            {
                if (hostile.Hostile == null) continue;
                bool target = hostile.Hostile == insight.Target;
                text.Append("<color=").Append(InspectorText.Muted).Append('>').Append(hostile.Hostile.DisplayName).Append("</color><pos=34%>")
                    .Append('×').Append(hostile.Weight.ToString("0.00")).Append("   ")
                    .Append(hostile.Seen ? "seen" : hostile.Sensed ? "heard" : "<color=#9AA0B0>not sensed</color>");
                if (target) text.Append("   ").Coloured(InspectorText.Gold, "target");
                text.Append('\n');
            }
        }

        // The tree nodes running right now, indented under their parents; the ones doing the
        // work (nothing running below them) stand out.
        public static void Running(StringBuilder text, List<(BtNode Node, int Depth)> running)
        {
            text.Heading("Doing");
            if (running.Count == 0)
            {
                text.Coloured(InspectorText.Muted, "nothing running (between ticks, or no tree)").Append('\n');
                return;
            }

            int shown = running.Count <= MaxRunningLines ? running.Count : MaxRunningLines - 1;
            for (int i = 0; i < shown; i++)
            {
                (BtNode node, int depth) = running[i];
                bool leaf = i + 1 >= running.Count || running[i + 1].Depth <= depth;
                text.Indent(depth);
                if (leaf) text.Append("<color=").Append(InspectorText.Gold).Append('>');
                text.Append(BtNodeNames.WithSummary(node));
                if (leaf) text.Append("</color>");
                Comment(text, node.Comment);
                text.Append('\n');
            }
            if (shown < running.Count)
                text.Coloured(InspectorText.Muted, $"… {running.Count - shown} more").Append('\n');
        }

        // The designer's note on the node, first line and shortened, so branches can be told apart.
        private static void Comment(StringBuilder text, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment)) return;
            int end = comment.IndexOf('\n');
            if (end < 0) end = comment.Length;
            bool cut = end > CommentLength;
            text.Append("  <color=").Append(InspectorText.Muted).Append("><i>")
                .Append(comment, 0, cut ? CommentLength : end).Append(cut ? "…" : "").Append("</i></color>");
        }
    }
}
