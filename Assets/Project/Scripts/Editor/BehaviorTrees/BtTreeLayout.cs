using System.Collections.Generic;
using CGD.BehaviorTrees;
using UnityEngine;

namespace CGD.Editor
{
    // Tidy top-down layout: each node sits centred above its children, siblings side by side
    // in execution order. Nodes not connected to the root are lined up underneath.
    internal static class BtTreeLayout
    {
        private const float ColumnWidth = 220f;
        private const float RowHeight   = 150f;

        public static void Apply(BehaviorTreeAsset asset)
        {
            var placed = new HashSet<BtNode>();
            float nextColumn = 0f;
            int   depth      = 0;

            if (asset.Root != null) depth = Place(asset, asset.Root, 0, ref nextColumn, placed);

            float x = 0f;
            foreach (BtNode node in asset.Nodes)
            {
                if (node == null || placed.Contains(node)) continue;
                node.Position = new Vector2(x, (depth + 1) * RowHeight);
                x += ColumnWidth;
            }
        }

        // Returns the deepest row used under this node.
        private static int Place(BehaviorTreeAsset asset, BtNode node, int row, ref float nextColumn, HashSet<BtNode> placed)
        {
            placed.Add(node);
            int deepest = row;
            float first = float.NaN, last = 0f;

            if (node is BtParentNode parent)
            {
                foreach (string guid in parent.ChildGuids)
                {
                    BtNode child = asset.FindNode(guid);
                    if (child == null || placed.Contains(child)) continue;

                    deepest = Mathf.Max(deepest, Place(asset, child, row + 1, ref nextColumn, placed));
                    if (float.IsNaN(first)) first = child.Position.x;
                    last = child.Position.x;
                }
            }

            float column;
            if (float.IsNaN(first))
            {
                column = nextColumn;
                nextColumn += ColumnWidth;
            }
            else
            {
                column = (first + last) * 0.5f;
            }

            node.Position = new Vector2(column, row * RowHeight);
            return deepest;
        }
    }
}
