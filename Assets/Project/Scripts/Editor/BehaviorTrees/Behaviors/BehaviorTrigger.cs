using System.Collections.Generic;
using CGD.BehaviorTrees;

namespace CGD.Editor
{
    // When a behavior gets its turn, in plain words: the checks at the top of its branch —
    // the leading conditions of its Sequence and the gates (Cooldown, Limit, Chance) wrapped
    // around it — e.g. "Knows Target (4 s memory) · Health Below (≤ 30%) · Cooldown (8 s)".
    // Ordering a priority list is mostly a question of these: a behavior with none takes the
    // turn whenever it can act, so whatever sits below it rarely or never runs.
    // The checks also come back as data, for the Composer's live ✓/✗ marks and its what-if
    // situation (BehaviorSituation).
    internal static class BehaviorTrigger
    {
        private const int MaxDepth = 8;   // a branch's checks sit near its top; deeper is its body

        // One check at the top of a behavior: a condition (maybe negated) or a gate.
        public readonly struct Check
        {
            public readonly BehaviorTreeAsset Asset;   // the asset the node lives in (a behavior's, for a Subtree)
            public readonly BtNode            Node;
            public readonly bool              Negated;
            public readonly bool              IsGate;
            public readonly string            Phrase;  // "Health Below (≤ 30%)", without "not"

            public Check(BehaviorTreeAsset asset, BtNode node, bool negated, bool isGate)
            {
                Asset   = asset;
                Node    = node;
                Negated = negated;
                IsGate  = isGate;
                Phrase  = PhraseOf(node);
            }

            public string Text => Negated ? "not " + Phrase : Phrase;
        }

        // A behavior read from the top: its checks, then its body — the node where the checks
        // end, in the asset it lives in. A body that is a list of choices acts when one of them can.
        public readonly struct Reading
        {
            public readonly List<Check>       Checks;
            public readonly BtNode            Body;
            public readonly BehaviorTreeAsset BodyAsset;

            public Reading(List<Check> checks, BtNode body, BehaviorTreeAsset bodyAsset)
            {
                Checks    = checks;
                Body      = body;
                BodyAsset = bodyAsset;
            }

            public bool IsChoice => Body is BtComposite and not Sequence and not Parallel;

            // The choices of a choice body, switched-off behaviors left out.
            public List<BtNode> Choices()
            {
                var choices = new List<BtNode>();
                if (!IsChoice) return choices;
                foreach (string guid in ((BtParentNode)Body).ChildGuids)
                {
                    BtNode choice = BodyAsset.FindNode(guid);
                    if (choice != null && choice is not Subtree { IsOn: false }) choices.Add(choice);
                }
                return choices;
            }
        }

        public static string Describe(BehaviorTreeAsset asset, BtNode top)
        {
            Reading reading = Read(asset, top);
            if (reading.Checks.Count > 0) return Join(reading.Checks, null);
            if (!reading.IsChoice) return "always, when its turn comes";
            return ActsAlways(reading, 1) ? "always (one of its choices always can)" : "when one of its choices can act";
        }

        // The checks with how each last answered for the watched agent: ✓ held, ✗ didn't,
        // · not asked yet. Gates are left unmarked (their answer is their child's).
        public static string DescribeLive(BehaviorTreeAsset asset, BtNode top, BehaviorTree watched)
        {
            Reading reading = Read(asset, top);
            return reading.Checks.Count > 0 ? Join(reading.Checks, watched) : Describe(asset, top);
        }

        // No checks of its own, or a list of choices one of which has none: it takes every turn
        // it gets (a routine ending in Patrol, a catch-all attack).
        public static bool ActsAlways(BehaviorTreeAsset asset, BtNode top) => ActsAlways(Read(asset, top), 0);

        private static bool ActsAlways(Reading reading, int depth)
        {
            if (reading.Checks.Count > 0 || reading.Body == null) return false;
            if (!reading.IsChoice) return true;
            if (depth >= MaxDepth) return false;

            foreach (BtNode choice in reading.Choices())
                if (ActsAlways(Read(reading.BodyAsset, choice), depth + 1)) return true;
            return false;
        }

        // Collects the checks from the top down, following subtrees into their assets.
        public static Reading Read(BehaviorTreeAsset asset, BtNode top)
        {
            var checks = new List<Check>();
            BtNode node = top;
            for (int depth = 0; depth < MaxDepth && node != null; depth++)
            {
                switch (node)
                {
                    case BtRoot root:   // a behavior asset read from the top
                        node = FirstChild(asset, root);
                        continue;

                    case Subtree subtree:
                        if (subtree.Tree == null || subtree.Tree == asset || subtree.Tree.Root == null) return new Reading(checks, null, asset);
                        asset = subtree.Tree;
                        node  = FirstChild(asset, asset.Root);
                        continue;

                    case Inverter inverter:
                        if (FirstChild(asset, inverter) is BtCondition inverted) checks.Add(new Check(asset, inverted, true, false));
                        return new Reading(checks, inverter, asset);

                    case BtDecorator decorator:
                        if (IsGate(decorator)) checks.Add(new Check(asset, decorator, false, true));
                        node = FirstChild(asset, decorator);
                        continue;

                    case BtCondition:
                        checks.Add(new Check(asset, node, false, false));
                        return new Reading(checks, node, asset);

                    case Sequence sequence:
                        AddLeadingChecks(asset, sequence, checks);
                        return new Reading(checks, sequence, asset);

                    default:
                        return new Reading(checks, node, asset);
                }
            }
            return new Reading(checks, node, asset);
        }

        // A Sequence runs its children in order, so the conditions before its first action are
        // what must hold for the behavior to start.
        private static void AddLeadingChecks(BehaviorTreeAsset asset, Sequence sequence, List<Check> checks)
        {
            foreach (string guid in sequence.ChildGuids)
            {
                BtNode child = asset.FindNode(guid);
                switch (child)
                {
                    case BtCondition:
                        checks.Add(new Check(asset, child, false, false));
                        break;
                    case Inverter inverter when FirstChild(asset, inverter) is BtCondition inverted:
                        checks.Add(new Check(asset, inverted, true, false));
                        break;
                    case BtDecorator decorator when IsGate(decorator) && FirstChild(asset, decorator) is BtCondition gated:
                        checks.Add(new Check(asset, decorator, false, true));
                        checks.Add(new Check(asset, gated, false, false));
                        break;
                    default:
                        return;
                }
            }
        }

        private static string Join(List<Check> checks, BehaviorTree watched)
        {
            var parts = new string[checks.Count];
            for (int i = 0; i < checks.Count; i++)
                parts[i] = watched != null && !checks[i].IsGate ? Mark(checks[i], watched) + " " + checks[i].Text : checks[i].Text;
            return string.Join(" · ", parts);
        }

        // A condition's own answer, flipped when an Inverter wraps it. A behavior run in several
        // places reports its first use (BehaviorTree.TryGetNode).
        private static string Mark(Check check, BehaviorTree watched)
        {
            if (!watched.TryGetNode(check.Asset, check.Node.Guid, out BtNode live) || live.LastTick < 0) return "·";
            bool held = live.LastStatus == BtStatus.Success;
            return held != check.Negated ? "✓" : "✗";
        }

        // Chance isn't a gate for abort purposes (it rolls once per try), but to a designer it
        // is the same kind of "only sometimes" as a Cooldown.
        private static bool IsGate(BtDecorator decorator) => decorator.IsGate || decorator is Chance;

        private static string PhraseOf(BtNode node)
        {
            string name    = BtNodeNames.Of(node);
            string summary = node.Summary;
            return string.IsNullOrWhiteSpace(summary) ? name : $"{name} ({summary})";
        }

        private static BtNode FirstChild(BehaviorTreeAsset asset, BtParentNode parent) =>
            parent.ChildGuids.Count > 0 ? asset.FindNode(parent.ChildGuids[0]) : null;
    }
}
