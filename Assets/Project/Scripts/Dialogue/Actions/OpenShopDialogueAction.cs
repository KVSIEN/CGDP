using UnityEngine;
using CGD.Economy;

namespace CGD.Dialogue
{
    // "Let's trade": opens the shop of the Vendor on the speaking NPC.
    [CreateAssetMenu(fileName = "OpenShopDialogueAction", menuName = "CGD/Dialogue/Actions/Open Shop")]
    public class OpenShopDialogueAction : DialogueAction
    {
        public override void Execute(DialogueContext context)
        {
            if (context.Speaker != null && context.Speaker.TryGetComponent(out Vendor vendor))
                vendor.OpenFor(context.Listener);
            else
                Debug.LogWarning($"{name}: the speaker has no Vendor to open.", context.Speaker);
        }
    }
}
