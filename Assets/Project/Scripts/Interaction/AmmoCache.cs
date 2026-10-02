using System.Collections.Generic;
using UnityEngine;
using CGD.Feedback;
using CGD.Items;
using CGD.Player;
using CGD.Weapons;

namespace CGD.Interaction
{
    // A resupply point with a limited number of uses. Each use tops up the reserve ammo of
    // every weapon the player carries to at least Magazines full magazines (a reserve
    // already above that is left alone). A use is only spent when it actually adds ammo.
    [RequireComponent(typeof(Collider))]
    public class AmmoCache : MonoBehaviour, IInteractable
    {
        [Tooltip("What it hands out — one munition per ammo type it supplies")]
        [SerializeField] private MunitionDefinition[] _munitions = System.Array.Empty<MunitionDefinition>();
        [SerializeField, Min(1)] private int _uses = 3;
        [Tooltip("Reserve each carried weapon is topped up to, in its own magazines")]
        [SerializeField, Min(1)] private int _magazines = 3;
        [Tooltip("Optional — hidden once the cache is empty (e.g. the ammo boxes inside)")]
        [SerializeField] private GameObject _contents;

        // Largest magazine carried per ammo type: what one "magazine" of reserve means for it.
        private readonly Dictionary<AmmoType, int> _magazineSizes = new();
        private int _left;

        public int UsesLeft => _left;

        private void Awake() => _left = _uses;

        public string GetInteractLabel(GameObject interactor) =>
            _left > 0 ? $"Resupply Ammo  ({_left} left)" : "Ammo Cache  (empty)";

        public bool CanInteract(GameObject interactor) =>
            _left > 0 && interactor.TryGetComponent(out PlayerWeaponLoadout _) && interactor.TryGetComponent(out PlayerInventory _);

        public void Interact(GameObject interactor)
        {
            if (_left <= 0) return;
            if (!interactor.TryGetComponent(out PlayerWeaponLoadout loadout)) return;
            if (!interactor.TryGetComponent(out PlayerInventory inventory)) return;

            CollectMagazineSizes(loadout);
            int added = 0;
            foreach (var (type, magazine) in _magazineSizes)
            {
                MunitionDefinition munition = MunitionFor(type);
                if (munition == null) continue;

                int missing = magazine * _magazines - inventory.Inventory.CountOf(type);
                if (missing <= 0) continue;

                int given = missing - inventory.Inventory.Add(munition, missing);
                if (given <= 0) continue;
                added += given;
                FeedbackBus.Notify($"+{given} {munition.DisplayName}", NotificationStyle.Reward);
            }

            if (added == 0)
            {
                FeedbackBus.Notify("Ammo already topped up", NotificationStyle.Info);
                return;
            }

            _left--;
            if (_left == 0 && _contents != null) _contents.SetActive(false);
        }

        private void CollectMagazineSizes(PlayerWeaponLoadout loadout)
        {
            _magazineSizes.Clear();
            foreach (WeaponInstance weapon in loadout.Slots)
            {
                if (weapon == null) continue;
                AmmoType type = weapon.Data.AmmoType;
                _magazineSizes.TryGetValue(type, out int size);
                _magazineSizes[type] = Mathf.Max(size, weapon.MagazineSize);
            }
        }

        private MunitionDefinition MunitionFor(AmmoType type)
        {
            foreach (MunitionDefinition munition in _munitions)
                if (munition != null && munition.AmmoType == type) return munition;
            return null;
        }
    }
}
