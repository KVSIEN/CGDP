using UnityEngine;
using CGD.Core;
using CGD.Player;

namespace CGD.Interaction
{
    // Attach to a world GameObject with a Collider (set Is Trigger = true).
    // Heals the player on interact.
    [RequireComponent(typeof(Collider))]
    public class HealthPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private float _amount = 25f;

        public string GetInteractLabel(GameObject interactor) => "Pick Up  Health";

        public bool CanInteract(GameObject player) =>
            player.TryGetComponent(out PlayerHealth health) && !health.IsDead && health.Health < health.MaxHealth;

        public void Interact(GameObject player)
        {
            if (!CanInteract(player)) return;

            player.GetComponent<PlayerHealth>().Heal(_amount);
            PrefabPool.Release(gameObject);
        }
    }
}
