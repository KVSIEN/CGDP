using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Input;
using CGD.Items;
using CGD.UI;
using CGD.Weapons;

namespace CGD.Player
{
    // Owns the carried weapons (each with its own ammo) and tells WeaponController
    // which one to fire. Refills every weapon when the player is revived. Pressing a slot
    // key equips that slot; holding it opens the weapon wheel to put another weapon there —
    // one from another slot (they swap) or a spare from the pack (the old one goes to the pack).
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(WeaponController))]
    public class PlayerWeaponLoadout : MonoBehaviour
    {
        public const int SlotCount = 4;

        [Tooltip("Weapons carried at start; empty slots are filled by pickups")]
        [SerializeField] private WeaponData[] _startingWeapons = new WeaponData[SlotCount];
        [Tooltip("Optional — the wheel shown while holding a slot key")]
        [SerializeField] private SlotWheelHUD _wheel;

        // Inspector mirror of the runtime loadout; overwritten on every change, so edits here do nothing.
        [Header("Runtime (read-only)")]
        [SerializeField] private WeaponData   _activeWeapon;
        [SerializeField] private WeaponData[] _equippedWeapons = new WeaponData[SlotCount];

        private readonly WeaponInstance[] _slots = new WeaponInstance[SlotCount];

        public IReadOnlyList<WeaponInstance> Slots      => _slots;
        public int                           ActiveSlot => _activeSlot;

        private PlayerInputHandler _input;
        private WeaponController   _weapon;
        private HealthManager      _health;
        private int                _activeSlot = -1;
        private PlayerInventory    _inventory;
        private SlotKeyWheel       _keys;

        // The wheel's choices for the slot being held, in order.
        private readonly List<WeaponInstance> _wheelWeapons = new();
        private readonly List<string>         _wheelLabels  = new();

        private static readonly GameAction[] SlotActions =
        {
            GameAction.Weapon1,
            GameAction.Weapon2,
            GameAction.Weapon3,
            GameAction.Weapon4,
        };

        private void Awake()
        {
            _input  = GetComponent<PlayerInputHandler>();
            _weapon = GetComponent<WeaponController>();
            TryGetComponent(out _inventory);
            _keys = new SlotKeyWheel(_input, _wheel, SlotActions, "Weapon");

            for (int i = 0; i < Mathf.Min(SlotCount, _startingWeapons.Length); i++)
            {
                if (_startingWeapons[i] != null)
                    _slots[i] = new WeaponInstance(_startingWeapons[i]);
            }

            if (TryGetComponent(out _health))
                _health.OnRevived += RefillAll;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnRevived -= RefillAll;
        }

        private void Start()
        {
            EquipSlot(0);
        }

        private void OnDisable() => _keys.Cancel();

        private void Update() => _keys.Tick(Time.deltaTime, EquipSlot, null, WheelOptions, ChooseFromWheel);

        public void EquipSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return;
            if (_activeSlot == index) return;
            Equip(index);
        }

        // Puts `weapon` in the first empty slot and equips it; with every slot full it
        // replaces the active weapon instead. Returns the replaced weapon, or null.
        public WeaponInstance AddWeapon(WeaponInstance weapon)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] != null) continue;
                _slots[i] = weapon;
                Equip(i);
                return null;
            }

            int target = Mathf.Max(0, _activeSlot);
            WeaponInstance replaced = _slots[target];
            _slots[target] = weapon;
            Equip(target);
            return replaced;
        }

        // Every weapon in the other slots, then the spares in the pack.
        private IReadOnlyList<string> WheelOptions(int slot)
        {
            _wheelWeapons.Clear();
            _wheelLabels.Clear();

            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == null) continue;
                _wheelWeapons.Add(_slots[i]);
                _wheelLabels.Add(i == slot ? $"{_slots[i].DisplayName}  (here)" : $"{_slots[i].DisplayName}  [{i + 1}]");
            }

            if (_inventory != null)
                foreach (ItemInstance item in _inventory.Inventory.Items)
                {
                    if (item is not WeaponInstance spare) continue;
                    _wheelWeapons.Add(spare);
                    _wheelLabels.Add($"{spare.DisplayName}  (pack)");
                }
            return _wheelLabels;
        }

        private void ChooseFromWheel(int slot, int option)
        {
            if (option < 0 || option >= _wheelWeapons.Count) return;
            WeaponInstance chosen = _wheelWeapons[option];

            int from = System.Array.IndexOf(_slots, chosen);
            if (from >= 0)
            {
                (_slots[from], _slots[slot]) = (_slots[slot], _slots[from]);
            }
            else if (_inventory != null && _inventory.Inventory.Remove(chosen))
            {
                if (_slots[slot] != null) _inventory.Inventory.Add(_slots[slot]);
                _slots[slot] = chosen;
            }
            Equip(slot);
        }

        public void RefillAll()
        {
            foreach (WeaponInstance weapon in _slots)
                weapon?.RefillMagazine();

            if (_activeSlot >= 0) Equip(_activeSlot);
        }

        private void Equip(int index)
        {
            _activeSlot = index;
            _weapon.Equip(_slots[index]);
            RefreshInspectorView();
        }

        private void RefreshInspectorView()
        {
            if (_equippedWeapons == null || _equippedWeapons.Length != SlotCount)
                _equippedWeapons = new WeaponData[SlotCount];

            for (int i = 0; i < SlotCount; i++)
                _equippedWeapons[i] = _slots[i]?.Data;

            _activeWeapon = _slots[_activeSlot]?.Data;
        }
    }
}
