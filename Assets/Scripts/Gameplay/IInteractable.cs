using UnityEngine;

public interface IInteractable
{
    string InteractLabel { get; }
    void Interact(GameObject player);
}
