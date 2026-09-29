using System;
using UnityEngine;

namespace CGD.Dialogue
{
    // One thing the NPC says and the replies to it.
    [Serializable]
    public class DialogueNode
    {
        [Tooltip("Referenced by choices' Next")]
        public string Id;
        [TextArea(2, 6)] public string Text;
        [Tooltip("No available choices = the player can only leave")]
        public DialogueChoice[] Choices = Array.Empty<DialogueChoice>();
    }
}
