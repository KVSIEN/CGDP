using UnityEngine;
using CGD.Interaction;

namespace CGD.Dialogue
{
    // Offers a choice only while the player carries an item (a keycard, enough scrap).
    [CreateAssetMenu(fileName = "NewHasItemCondition", menuName = "CGD/Dialogue/Conditions/Has Item")]
    public class HasItemDialogueCondition : DialogueCondition
    {
        [SerializeField] private ItemRequirement _requirement;

        public override bool IsMet(DialogueContext context) =>
            context.Listener != null && _requirement.IsMetBy(context.Listener);
    }
}
