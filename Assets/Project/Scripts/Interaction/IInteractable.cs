using UnityEngine;

namespace CGD.Interaction
{
    // Anything the player can use with the Interact key: doors, switches, pickups, chests,
    // terminals, NPCs. PlayerInteraction finds implementers on colliders in range; nothing
    // needs registering. Optional behaviour has defaults, so simple objects only supply a
    // label and Interact.
    public interface IInteractable
    {
        // Prompt text for this interactor, e.g. "Open", "Pick Up  Rifle", "Locked  (Red Keycard)".
        string GetInteractLabel(GameObject interactor);

        void Interact(GameObject interactor);

        // False hides the prompt and blocks Interact (e.g. a health pickup at full health).
        bool CanInteract(GameObject interactor) => true;

        // Seconds the Interact key must be held before Interact fires; 0 = on press.
        float HoldDuration => 0f;

        // How the key is used. Unless an interactable says otherwise: a hold when it has a
        // HoldDuration, a single press when it has none. Mash needs TapsRequired.
        InteractionStyle Style => HoldDuration > 0f ? InteractionStyle.Hold : InteractionStyle.Press;

        // Mash only: taps that complete the effort.
        int TapsRequired => 5;

        // Mash only: share of the bar lost per second once the player stops tapping (0 = never).
        float TapDecayPerSecond => 0.3f;

        // Breaks ties between overlapping interactables: higher wins before look direction
        // is considered (e.g. a keycard lying in front of the door it opens).
        int InteractPriority => 0;

        // True skips PlayerInteraction's line-of-sight check, so the target can be used while
        // hidden behind or sunk into geometry (pickups that fall through the floor).
        bool IgnoreLineOfSight => false;
    }
}
