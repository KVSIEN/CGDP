using System.Collections.Generic;
using CGD.BehaviorTrees;

namespace CGD.Editor
{
    // When a behavior gets its turn, in plain words: the checks at the top of its branch —
    // the leading conditions of its Sequence and the gates (Cooldown, Limit, Chance) wrapped
    // around it — e.g. "Knows Target (4 s memory) · Health Below (≤ 30%) · Cooldown (8 s)".
    // Ordering a priority list is mostly a question of these: a behavior with none takes the
    // turn whenever it can act, so whatever sits below it rarely or never runs.
    internal static class BehaviorTrigger
    {
        private const int MaxDepth = 8;   // a branch's checks sit near its top; deeper is its body

        public static string Describe(BehaviorTreeAsset asset, BtNode top)
        {
            var checks = new List<string>();
            BtNode body = Read(ref asset, top, checks);
            if (checks.Count > 0) return string.Join(" · ", checks);
            if (!IsChoice(body)) return "always, when its turn comes";
            return ActsAlways(asset, body, 1) ? "always (one of its choices always can)" : "when one of its choices can act";
        }

        // No checks of its own, or a list of choices one of which has none: it takes every turn
        // it gets (a routine ending in Patrol, a catch-all attack).
        public static bool ActsAlways(BehaviorTreeAsset asset, BtNode top) => ActsAlways(asset, top, 0);

        private static bool ActsAlways(BehaviorTreeAsset asset, BtNode top, int depth)
        {
            var checks = new List<string>();
            BtNode body = Read(ref asset, top, checks);
            if (checks.Count > 0 || body == null) return false;
            if (!IsChoice(body)) return true;
            if (depth >= MaxDepth) return false;

            foreach (string guid in ((BtParentNode)body).ChildGuids)
            {
                BtNode choice = asset.FindNode(guid);
                if (choice != null && choice is not Subtree { IsOn: false } && ActsAlways(asset, choice, depth + 1)) return true;
            }
            return false;
        }

        // Collects the checks from the top down and returns the node where they end (the
        // behavior's body: its Sequence, its first action, a list of choices); `asset` follows
        // it into subtrees.
        private static BtNode Read(ref BehaviorTreeAsset asset, BtNode top, List<string> checks)
        {
            BtNode node = top;
            for (int depth = 0; depth < MaxDepth && node != null; depth++)
            {
                switch (node)
                {
                    case BtRoot root:   // a behavior asset read from the top
                        node = FirstChild(asset, root);
                        continue;

                    case Subtree subtree:
                        if (subtree.Tree == null || subtree.Tree == asset || subtree.Tree.Root == null) return null;
                        asset = subtree.Tree;
                        node  = FirstChild(asset, asset.Root);
                        continue;

                    case Inverter inverter:
                        BtNode inverted = FirstChild(asset, inverter);
                        if (inverted is BtCondition) checks.Add("not " + Phrase(inverted));
                        return inverter;

                    case BtDecorator decorator:
                        if (IsGate(decorator)) checks.Add(Phrase(decorator));
                        node = FirstChild(asset, decorator);
                        continue;

                    case BtCondition:
                        checks.Add(Phrase(node));
                        return node;

                    case Sequence sequence:
                        AddLeadingChecks(asset, sequence, checks);
                        return sequence;

                    default:
                        return node;
                }
            }
            return node;
        }

        // A Sequence runs its children in order, so the conditions before its first action are
        // what must hold for the behavior to start.
        private static void AddLeadingChecks(BehaviorTreeAsset asset, Sequence sequence, List<string> checks)
        {
            foreach (string guid in sequence.ChildGuids)
            {
                BtNode child = asset.FindNode(guid);
                switch (child)
                {
                    case BtCondition:
                        checks.Add(Phrase(child));
                        break;
                    case Inverter inverter when FirstChild(asset, inverter) is BtCondition inverted:
                        checks.Add("not " + Phrase(inverted));
                        break;
                    case BtDecorator decorator when IsGate(decorator) && FirstChild(asset, decorator) is BtCondition gated:
                        checks.Add(Phrase(decorator));
                        checks.Add(Phrase(gated));
                        break;
                    default:
                        return;
                }
            }
        }

        // Chance isn't a gate for abort purposes (it rolls once per try), but to a designer it
        // is the same kind of "only sometimes" as a Cooldown.
        private static bool IsGate(BtDecorator decorator) => decorator.IsGate || decorator is Chance;

        // A Selector-like body acts when one of its children can (a Parallel runs them all).
        private static bool IsChoice(BtNode node) => node is BtComposite and not Sequence and not Parallel;

        private static string Phrase(BtNode node)
        {
            string name    = BtNodeNames.Of(node);
            string summary = node.Summary;
            return string.IsNullOrWhiteSpace(summary) ? name : $"{name} ({summary})";
        }

        private static BtNode FirstChild(BehaviorTreeAsset asset, BtParentNode parent) =>
            parent.ChildGuids.Count > 0 ? asset.FindNode(parent.ChildGuids[0]) : null;
    }
}
