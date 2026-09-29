using UnityEngine;

namespace CGD.Dialogue
{
    // Something a dialogue choice does besides moving the conversation on: open a shop,
    // hand over an item, start a quest. Each is a small asset shared by every dialogue
    // that needs it, so new behaviour is a new subclass rather than a change to dialogue.
    public abstract class DialogueAction : ScriptableObject
    {
        public abstract void Execute(DialogueContext context);
    }
}
