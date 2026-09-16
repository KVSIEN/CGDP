using UnityEngine;

namespace CGD.Interaction
{
    public interface IInteractable
    {
        string InteractLabel { get; }
        void Interact(GameObject player);
    }
}
