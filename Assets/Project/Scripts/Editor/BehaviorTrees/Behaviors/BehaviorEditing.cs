using System.Collections.Generic;
using CGD.BehaviorTrees;
using CGD.Enemies;
using UnityEngine;

namespace CGD.Editor
{
    // The tree edits behind behaviors. A behavior is a branch kept in its own tree asset and run
    // through a Subtree node, so one behavior (an ambush, a pounce, leaving a hazard) can be
    // shared by many enemy types and switched off for some of them. An enemy's tree is then a
    // priority list — a Selector — of such behaviors.
    //
    // These only change the assets in memory; callers record Undo, mark them dirty and save.
    internal static class BehaviorEditing
    {
        private const float ColumnWidth = 220f;
        private const float RowHeight   = 150f;

        // A Selector in the tree and its children: one priority list of behaviors.
        public readonly struct BehaviorList
        {
            public readonly Selector Selector;
            public readonly string   Title;
            public readonly int      Depth;

            public BehaviorList(Selector selector, string title, int depth)
            {
                Selector = selector;
                Title    = title;
                Depth    = depth;
            }
        }

        // Every Selector reachable from the Root (not inside subtrees), top first, named by its
        // comment or by the branch it decides for (e.g. "Fight while it knows where the target is").
        public static List<BehaviorList> Lists(BehaviorTreeAsset asset)
        {
            var lists = new List<BehaviorList>();
            if (asset != null && asset.Root != null) CollectLists(asset, asset.Root, null, 0, lists);
            return lists;
        }

        private static void CollectLists(BehaviorTreeAsset asset, BtNode node, BtNode parent, int depth, List<BehaviorList> lists)
        {
            if (node is Selector selector)
            {
                string title = FirstNonEmpty(selector.Comment, parent is BtRoot ? null : parent?.Comment,
                                             lists.Count == 0 ? "Behaviors" : "Choices");
                lists.Add(new BehaviorList(selector, title, depth));
                depth++;
            }

            if (node is not BtParentNode parentNode) return;
            foreach (string guid in parentNode.ChildGuids)
            {
                BtNode child = asset.FindNode(guid);
                if (child != null) CollectLists(asset, child, node, depth, lists);
            }
        }

        // The node and everything under it, parents before children.
        public static List<BtNode> Branch(BehaviorTreeAsset asset, BtNode top)
        {
            var nodes = new List<BtNode>();
            var stack = new Stack<BtNode>();
            stack.Push(top);
            while (stack.Count > 0)
            {
                BtNode node = stack.Pop();
                if (node == null || nodes.Contains(node)) continue;
                nodes.Add(node);
                if (node is not BtParentNode parent) continue;
                for (int i = parent.ChildGuids.Count - 1; i >= 0; i--) stack.Push(asset.FindNode(parent.ChildGuids[i]));
            }
            return nodes;
        }

        // Moves a branch into `module` (an empty tree) and puts a Subtree running it in its place,
        // so the tree behaves exactly as before. The branch's comment becomes the behavior's
        // description (the module Root's comment) and stays on the Subtree node. Null (nothing
        // changed) for the Root or a branch that isn't connected to anything.
        public static Subtree ExtractBranch(BehaviorTreeAsset source, BtNode top, BehaviorTreeAsset module)
        {
            BtParentNode parent = top is BtRoot ? null : source.FindParent(top);
            if (parent == null) return null;   // only a connected branch can be replaced
            List<BtNode> moved = Branch(source, top);

            var subtree = new Subtree { Position = top.Position, Comment = top.Comment };
            subtree.SetTree(module);

            var order = new List<string>(parent.ChildGuids);
            order[order.IndexOf(top.Guid)] = subtree.Guid;
            foreach (BtNode node in moved) source.RemoveNode(node);   // parents first, so links inside the branch survive
            source.AddNode(subtree);
            parent.SetChildOrder(order);
            ReplaceInGroups(source, moved, top.Guid, subtree.Guid);

            module.EnsureRoot();
            module.Root.Comment = top.Comment;
            Vector2 offset = module.Root.Position + new Vector2(0f, RowHeight) - top.Position;
            foreach (BtNode node in moved)
            {
                node.Position += offset;
                module.AddNode(node);
            }
            module.Root.AddChild(top.Guid);
            return subtree;
        }

        // Adds a Subtree running `module` to the list, at `index` (or the end), beside its siblings.
        public static Subtree AddBehavior(BehaviorTreeAsset asset, BtParentNode list, BehaviorTreeAsset module, int index = -1)
        {
            var subtree = new Subtree { Position = NextChildPosition(asset, list) };
            subtree.SetTree(module);
            asset.AddNode(subtree);

            var order = new List<string>(list.ChildGuids);
            order.Insert(index < 0 || index > order.Count ? order.Count : index, subtree.Guid);
            list.SetChildOrder(order);
            return subtree;
        }

        // Moves the child at `from` to `to` in its list (priority order).
        public static void Move(BtParentNode list, int from, int to)
        {
            var order = new List<string>(list.ChildGuids);
            if (from < 0 || from >= order.Count || to < 0 || to >= order.Count || from == to) return;

            string guid = order[from];
            order.RemoveAt(from);
            order.Insert(to, guid);
            list.SetChildOrder(order);
        }

        // Deletes the node and everything under it (a Subtree just goes; its tree stays).
        public static void RemoveBranch(BehaviorTreeAsset asset, BtNode top)
        {
            List<BtNode> branch = Branch(asset, top);
            var guids = new HashSet<string>();
            foreach (BtNode node in branch) guids.Add(node.Guid);
            foreach (BtNode node in branch) asset.RemoveNode(node);
            foreach (BtGroup group in asset.Groups) group.Nodes.RemoveAll(guids.Contains);
        }

        // A tree laid out the way the enemy trees are, ready for behaviors to be added:
        //   Selector (most urgent first)
        //     [regroup, raise alarm — if given]
        //     Sequence: fight while it knows where the target is (Knows Target)
        //       Selector — the fight behaviors go here
        //     [routine — if given: investigate, return to post, patrol]
        public static void BuildStarterTree(BehaviorTreeAsset asset, BehaviorTreeAsset regroup, BehaviorTreeAsset raiseAlarm, BehaviorTreeAsset routine)
        {
            asset.EnsureRoot();
            var top = Add(asset, asset.Root, new Selector { Comment = "Most urgent first: behaviors that abort lower priority interrupt whatever runs below them" });

            if (regroup != null)    AddBehavior(asset, top, regroup);
            if (raiseAlarm != null) AddBehavior(asset, top, raiseAlarm);

            var fight = Add(asset, top, new Sequence { Comment = "Fight while it knows where the target is", AbortSetting = AbortMode.Both });
            Add(asset, fight, new KnowsTarget());
            Add(asset, fight, new Selector { Comment = "Fight behaviors, most urgent first" });

            if (routine != null) AddBehavior(asset, top, routine);
            BtTreeLayout.Apply(asset);
        }

        private static T Add<T>(BehaviorTreeAsset asset, BtParentNode parent, T node) where T : BtNode
        {
            node.Position = NextChildPosition(asset, parent);
            asset.AddNode(node);
            parent.AddChild(node.Guid);
            return node;
        }

        private static Vector2 NextChildPosition(BehaviorTreeAsset asset, BtParentNode parent)
        {
            IReadOnlyList<string> children = parent.ChildGuids;
            BtNode last = children.Count > 0 ? asset.FindNode(children[^1]) : null;
            return last != null ? last.Position + new Vector2(ColumnWidth, 0f) : parent.Position + new Vector2(0f, RowHeight);
        }

        // Moved nodes leave their groups; the branch's own place goes to the Subtree.
        private static void ReplaceInGroups(BehaviorTreeAsset asset, List<BtNode> moved, string topGuid, string subtreeGuid)
        {
            var guids = new HashSet<string>();
            foreach (BtNode node in moved) guids.Add(node.Guid);

            foreach (BtGroup group in asset.Groups)
            {
                bool hadTop = group.Nodes.Contains(topGuid);
                group.Nodes.RemoveAll(guids.Contains);
                if (hadTop) group.Nodes.Add(subtreeGuid);
            }
        }

        private static string FirstNonEmpty(params string[] texts)
        {
            foreach (string text in texts)
                if (!string.IsNullOrWhiteSpace(text)) return text;
            return "";
        }
    }
}
