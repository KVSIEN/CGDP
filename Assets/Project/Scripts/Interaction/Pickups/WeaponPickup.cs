using UnityEngine;
using CGD.Core;
using CGD.Feedback;
using CGD.Player;
using CGD.Weapons;

namespace CGD.Interaction
{
    // World pickup: fills the first empty loadout slot, or swaps with the active weapon.
    [RequireComponent(typeof(Collider))]
    public class WeaponPickup : MonoBehaviour, IInteractable, IPoolable
    {
        [SerializeField] private WeaponData _data;

        private WeaponInstance _weapon;

        public WeaponInstance Weapon => _weapon ??= _data != null ? new WeaponInstance(_data) : null;

        public string GetInteractLabel(GameObject interactor) => Weapon != null ? $"Pick Up  {Weapon.Data.WeaponName}" : "Pick Up";

        // Used by RandomWeaponPickup to hand over an already-rolled weapon, so its
        // quality and attachment slots survive being picked up.
        public void SetWeapon(WeaponInstance weapon)
        {
            _weapon = weapon;
            _data   = weapon?.Data;
        }

        // A pooled pickup is handed its weapon by whoever spawns it (see LootDropper).
        public void OnDespawned() => _weapon = null;

        public bool CanInteract(GameObject player) =>
            Weapon != null && player.TryGetComponent<PlayerWeaponLoadout>(out _);

        public void Interact(GameObject player)
        {
            if (Weapon == null || !player.TryGetComponent(out PlayerWeaponLoadout loadout)) return;

            WeaponInstance picked   = Weapon;
            WeaponInstance replaced = loadout.AddWeapon(picked);
            FeedbackBus.Notify($"Picked up {picked.DisplayName}", NotificationStyle.Reward);
            if (replaced == null)
            {
                PrefabPool.Release(gameObject);
                return;
            }

            _weapon = replaced;
            _data   = replaced.Data;
        }
    }
}
