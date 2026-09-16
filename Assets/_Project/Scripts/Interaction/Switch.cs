using UnityEngine;

// Attach to a world GameObject with a Collider (set Is Trigger = true).
// Toggles every linked Door when interacted with.
[RequireComponent(typeof(Collider))]
public class Switch : MonoBehaviour, IInteractable
{
    [SerializeField] private string _label = "Switch";
    [SerializeField] private Door[] _doors;

    public string InteractLabel => _label;

    public void Interact(GameObject player)
    {
        foreach (Door door in _doors)
        {
            if (door != null) door.Toggle();
        }
    }
}
