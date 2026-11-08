using System.Text;
using CGD.BehaviorTrees;
using CGD.Enemies;

namespace CGD.DevTools
{
    // The inspector's decisions page: its latest cover / flank / vanish searches (what they
    // looked at and what came of it — the Paths & plans overlay shows where) and the tree's
    // most recent decisions, newest first (the Behavior Tree Editor's trace, in game).
    internal static class DecisionsPage
    {
        private const int TraceLines = 14;
        private static readonly int VerdictCount = System.Enum.GetValues(typeof(CandidateVerdict)).Length;
        private static readonly int[] Counts = new int[VerdictCount];

        public static void Write(StringBuilder text, EnemyInsight insight, BtTrace trace)
        {
            text.Heading("Searches");
            float now = UnityEngine.Time.time;
            Search(text, insight.Tactics.Cover, now);
            Search(text, insight.Tactics.Flank, now);
            Search(text, insight.Tactics.Vanish, now);

            text.Heading("Tree, newest first");
            if (trace == null || trace.Count == 0)
            {
                text.Coloured(InspectorText.Muted, "nothing recorded yet").Append('\n');
                return;
            }

            float treeTime = insight.Tree.Context.Time;
            for (int i = trace.Count - 1, shown = 0; i >= 0 && shown < TraceLines; i--, shown++)
                Entry(text, trace[i], treeTime);
        }

        private static void Search(StringBuilder text, TacticalDecision decision, float now)
        {
            text.Append("<color=").Append(InspectorText.Muted).Append('>').Append(decision.Plan).Append("</color><pos=34%>");
            if (!decision.HasRun)
            {
                text.Coloured(InspectorText.Muted, "not yet").Append('\n');
                return;
            }

            text.Seconds(now - decision.Time).Append(" ago   ");
            text.Coloured(decision.Succeeded ? InspectorText.Good : InspectorText.Bad, decision.Succeeded ? "found" : "none");

            System.Array.Clear(Counts, 0, Counts.Length);
            foreach (TacticalCandidate candidate in decision.Candidates) Counts[(int)candidate.Verdict]++;
            for (int v = 0; v < VerdictCount; v++)
            {
                if (Counts[v] == 0 || v == (int)CandidateVerdict.Picked) continue;
                text.Append("  ·  ").Append(Counts[v]).Append(' ').Append(VerdictLabel((CandidateVerdict)v));
            }
            text.Append('\n');
        }

        private static void Entry(StringBuilder text, BtTraceEntry entry, float treeTime)
        {
            text.Coloured(InspectorText.Muted, (treeTime - entry.Time).ToString("0.0")).Append("<pos=12%>");
            switch (entry.Event)
            {
                case BtTraceEvent.Started:     text.Append("start"); break;
                case BtTraceEvent.Succeeded:   text.Coloured(InspectorText.Good, "done"); break;
                case BtTraceEvent.Failed:      text.Coloured(InspectorText.Bad, "fail"); break;
                case BtTraceEvent.Aborted:     text.Coloured(InspectorText.Muted, "stop"); break;
                case BtTraceEvent.Interrupted: text.Coloured(InspectorText.Warn, "cut"); break;
            }
            text.Append("<pos=26%>").Append(BtNodeNames.Of(entry.Node));
            if (entry.Event == BtTraceEvent.Interrupted && entry.Cause != null)
                text.Append("  <color=").Append(InspectorText.Muted).Append(">by ").Append(BtNodeNames.Of(entry.Cause)).Append("</color>");
            text.Append('\n');
        }

        private static string VerdictLabel(CandidateVerdict verdict) => verdict switch
        {
            CandidateVerdict.Passed      => "passed over",
            CandidateVerdict.Exposed     => "exposed",
            CandidateVerdict.Taken       => "taken",
            CandidateVerdict.Seen        => "seen",
            CandidateVerdict.Unreachable => "unreachable",
            CandidateVerdict.TooFar      => "too far",
            _                            => verdict.ToString(),
        };
    }
}
