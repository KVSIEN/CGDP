using UnityEngine;

namespace CGD.Dialogue
{
    // Decides whether a dialogue choice is offered (only mention the keycard if the player
    // carries it). Same asset-per-rule idea as DialogueAction.
    public abstract class DialogueCondition : ScriptableObject
    {
        public abstract bool IsMet(DialogueContext context);
    }
}
