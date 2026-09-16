using UnityEngine;
using CGD.Player;

namespace CGD.Interaction
{
    // Attach to a world GameObject with a Collider (set Is Trigger = true).
    // Heals the player on interact.
    [RequireComponent(typeof(Collider))]
    public class HealthPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private float _amount = 25f;

        public string InteractLabel => "Pick Up  Health";

        public void Interact(GameObject player)
        {
            var health = player.GetComponent<PlayerHealth>();
            if (health == null) return;

            health.Heal(_amount);
            Destroy(gameObject);
        }
    }
}
