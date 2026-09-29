using System;
using UnityEngine;

namespace CGD.Dialogue
{
    // A conversation as a small graph: nodes of NPC lines, each with player choices that
    // lead to another node, end the talk, or trigger actions. Plain data — Conversation
    // walks it at runtime.
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "CGD/Dialogue/Dialogue")]
    public class DialogueDefinition : ScriptableObject
    {
        [Tooltip("Id of the opening node. Empty = the first node")]
        [SerializeField] private string _startNode;
        [SerializeField] private DialogueNode[] _nodes = Array.Empty<DialogueNode>();

        public DialogueNode StartNode =>
            TryGetNode(_startNode, out DialogueNode node) ? node : _nodes.Length > 0 ? _nodes[0] : null;

        public bool TryGetNode(string id, out DialogueNode node)
        {
            node = null;
            if (string.IsNullOrEmpty(id)) return false;

            foreach (DialogueNode candidate in _nodes)
            {
                if (candidate.Id != id) continue;
                node = candidate;
                return true;
            }
            return false;
        }
    }
}
