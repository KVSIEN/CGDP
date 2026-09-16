using UnityEngine;
using CGD.Weapons;

namespace CGD.Interaction
{
    // Attach to a world GameObject with a Collider (set Is Trigger = true).
    // Adds reserve ammo to the player's currently equipped weapon on interact.
    [RequireComponent(typeof(Collider))]
    public class AmmoPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _amount = 30;

        public string InteractLabel => "Pick Up  Ammo";

        public bool CanInteract(GameObject player) =>
            player.TryGetComponent(out WeaponController weapon) && weapon.Current != null;

        public void Interact(GameObject player)
        {
            if (player.TryGetComponent(out WeaponController weapon) && weapon.AddReserveAmmo(_amount))
                Destroy(gameObject);
        }
    }
}
