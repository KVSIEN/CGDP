using UnityEngine;
using CGD.Player;
using CGD.Weapons;

namespace CGD.Interaction
{
    // World pickup: fills the first empty loadout slot, or swaps with the active weapon.
    [RequireComponent(typeof(Collider))]
    public class WeaponPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private WeaponData _data;

        private WeaponInstance _weapon;

        public WeaponInstance Weapon => _weapon ??= _data != null ? new WeaponInstance(_data) : null;

        public string InteractLabel => Weapon != null ? $"Pick Up  {Weapon.Data.WeaponName}" : "Pick Up";

        // Used by RandomWeaponPickup to hand over an already-rolled weapon, so its
        // quality and attachment slots survive being picked up.
        public void SetWeapon(WeaponInstance weapon)
        {
            _weapon = weapon;
            _data   = weapon?.Data;
        }

        public bool CanInteract(GameObject player) =>
            Weapon != null && player.TryGetComponent<PlayerWeaponLoadout>(out _);

        public void Interact(GameObject player)
        {
            if (Weapon == null || !player.TryGetComponent(out PlayerWeaponLoadout loadout)) return;

            WeaponInstance replaced = loadout.AddWeapon(Weapon);
            if (replaced == null)
            {
                Destroy(gameObject);
                return;
            }

            _weapon = replaced;
            _data   = replaced.Data;
        }
    }
}
