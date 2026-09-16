using UnityEngine;

namespace CGD.Interaction
{
    public interface IInteractable
    {
        string InteractLabel { get; }
        void Interact(GameObject player);

        // False hides the prompt and blocks Interact (e.g. a health pickup at full health).
        bool CanInteract(GameObject player) => true;
    }
}
