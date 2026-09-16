using UnityEngine;

// Attach to a world GameObject with a Collider (set Is Trigger = true).
// Heals the player on interact.
[RequireComponent(typeof(Collider))]
public class HealthPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private float _amount = 25f;

    public string InteractLabel => "Pick Up  Health";

    public void Interact(GameObject player)
    {
        var stats = player.GetComponent<PlayerStats>();
        if (stats == null) return;

        stats.Heal(_amount);
        Destroy(gameObject);
    }
}
