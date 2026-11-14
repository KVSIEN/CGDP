using System;
using System.Collections.Generic;
using CGD.BehaviorTrees;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The Behavior Composer's "What if…": tick which of the tree's checks hold (Knows Target,
    // Health Below 30%…) and each priority list marks the behavior that would get the turn —
    // the first one, top down, whose checks all hold. Answers "what does it do when…?" and
    // "why does it never pounce?" without playing. It reads only the checks at the top of each
    // behavior, so an action that fails partway (no path, say) is not foreseen.
    internal sealed class BehaviorSituation
    {
        private const int MaxDepth = 8;

        private readonly HashSet<string> _holding = new();   // check phrases ticked as true
        private bool _gatesReady = true;
        private bool _open;

        public bool IsActive => _open;

        public void Set(string phrase, bool holds)
        {
            if (holds) _holding.Add(phrase);
            else _holding.Remove(phrase);
        }

        public bool GatesReady { get => _gatesReady; set => _gatesReady = value; }

        public void Draw(BehaviorTreeAsset tree)
        {
            _open = EditorGUILayout.Foldout(_open, "What if… (try a situation)", true, EditorStyles.foldoutHeader);
            if (!_open) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Tick what is true right now; each list marks the behavior that would get the turn (→). " +
                                           "Only the checks at the top of each behavior are considered.", EditorStyles.wordWrappedMiniLabel);
                _gatesReady = EditorGUILayout.ToggleLeft("Cooldowns, limits and chances are ready", _gatesReady);

                List<string> phrases = Phrases(tree);
                if (phrases.Count == 0) EditorGUILayout.LabelField("No behavior in this tree has a check to try.", EditorStyles.miniLabel);
                foreach (string phrase in phrases)
                    Set(phrase, EditorGUILayout.ToggleLeft(phrase, _holding.Contains(phrase)));

                if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(60f))) _holding.Clear();
            }
        }

        // The index in `list` of the behavior that would get the turn, or -1 if none would.
        public int Turn(BehaviorTreeAsset tree, Selector list, Func<Subtree, bool> isOn)
        {
            for (int i = 0; i < list.ChildGuids.Count; i++)
            {
                BtNode child = tree.FindNode(list.ChildGuids[i]);
                if (child == null || child is Subtree subtree && !isOn(subtree)) continue;
                if (WouldAct(tree, child, 0)) return i;
            }
            return -1;
        }

        public bool WouldAct(BehaviorTreeAsset asset, BtNode top, int depth)
        {
            BehaviorTrigger.Reading reading = BehaviorTrigger.Read(asset, top);
            if (reading.Body == null) return false;
            foreach (BehaviorTrigger.Check check in reading.Checks)
                if (!Holds(check)) return false;

            if (!reading.IsChoice) return true;
            if (depth >= MaxDepth) return false;
            foreach (BtNode choice in reading.Choices())
                if (WouldAct(reading.BodyAsset, choice, depth + 1)) return true;
            return false;
        }

        private bool Holds(BehaviorTrigger.Check check)
        {
            if (check.IsGate) return _gatesReady;
            return _holding.Contains(check.Phrase) != check.Negated;
        }

        // Every distinct condition at the top of the tree's behaviors (and of their choices),
        // in the order met, top list first.
        public static List<string> Phrases(BehaviorTreeAsset tree)
        {
            var phrases = new List<string>();
            foreach (BehaviorEditing.BehaviorList list in BehaviorEditing.Lists(tree))
            foreach (string guid in list.Selector.ChildGuids)
            {
                BtNode row = tree.FindNode(guid);
                if (row != null) Collect(tree, row, phrases, 0);
            }
            return phrases;
        }

        private static void Collect(BehaviorTreeAsset asset, BtNode top, List<string> phrases, int depth)
        {
            BehaviorTrigger.Reading reading = BehaviorTrigger.Read(asset, top);
            foreach (BehaviorTrigger.Check check in reading.Checks)
                if (!check.IsGate && !phrases.Contains(check.Phrase)) phrases.Add(check.Phrase);

            if (depth >= MaxDepth) return;
            foreach (BtNode choice in reading.Choices()) Collect(reading.BodyAsset, choice, phrases, depth + 1);
        }
    }
}
