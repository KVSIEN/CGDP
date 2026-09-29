using System;
using UnityEngine;

namespace CGD.Dialogue
{
    // One reply the player can pick.
    [Serializable]
    public class DialogueChoice
    {
        public string Text = "Goodbye.";
        [Tooltip("Id of the node this leads to. Empty = end the conversation")]
        public string Next;
        [Tooltip("All must be met for the choice to be offered")]
        public DialogueCondition[] Conditions = Array.Empty<DialogueCondition>();
        [Tooltip("Run when picked, after the conversation moves on (or ends)")]
        public DialogueAction[] Actions = Array.Empty<DialogueAction>();

        public bool EndsConversation => string.IsNullOrEmpty(Next);

        public bool IsAvailable(DialogueContext context)
        {
            foreach (DialogueCondition condition in Conditions)
                if (condition != null && !condition.IsMet(context)) return false;
            return true;
        }
    }
}
