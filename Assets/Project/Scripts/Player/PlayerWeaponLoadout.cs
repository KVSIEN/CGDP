using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Input;
using CGD.Items;
using CGD.UI;
using CGD.Weapons;

namespace CGD.Player
{
    // Owns the carried weapons (firearms with their own ammo, or melee weapons) and hands
    // the active one to its controller: WeaponController for firearms, MeleeController for
    // melee weapons — the other one is left empty. Refills every weapon when the player is revived. Pressing a slot
    // key equips that slot; holding it opens the weapon wheel to put another weapon there —
    // one from another slot (they swap) or a spare from the pack (the old one goes to the pack).
    // Next/Previous Weapon (scroll wheel) cycle through the filled slots, and Last Weapon
    // swaps back to the weapon used before the current one.
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
        [SerializeField] private ScriptableObject   _activeWeapon;
        [SerializeField] private ScriptableObject[] _equippedWeapons = new ScriptableObject[SlotCount];

        private readonly WeaponItem[] _slots = new WeaponItem[SlotCount];

        public IReadOnlyList<WeaponItem> Slots      => _slots;
        public int                       ActiveSlot => _activeSlot;
        // The weapon in hand (null with an empty slot).
        public WeaponItem                Active     => _equippedWeapon;

        // A different weapon is now in hand.
        public event System.Action<WeaponItem> ActiveChanged;

        private PlayerInputHandler _input;
        private WeaponController   _weapon;
        private MeleeController    _melee;
        private HealthManager      _health;
        private int                _activeSlot = -1;
        // The weapon itself, not its slot, so it is still found after the wheel moves it.
        private WeaponItem         _lastWeapon;
        private WeaponItem         _equippedWeapon;
        private PlayerInventory    _inventory;
        private SlotKeyWheel       _keys;

        // The wheel's choices for the slot being held, in order.
        private readonly List<WeaponItem> _wheelWeapons = new();
        private readonly List<string>     _wheelLabels  = new();

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
            TryGetComponent(out _melee);
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

        private void Update()
        {
            _keys.Tick(Time.deltaTime, EquipSlot, null, WheelOptions, ChooseFromWheel);
            if (_keys.IsOpen) return;

            if      (_input.GetAction(GameAction.NextWeapon))     Cycle(1);
            else if (_input.GetAction(GameAction.PreviousWeapon)) Cycle(-1);
            else if (_input.GetAction(GameAction.LastWeapon))     EquipLast();
        }

        // Steps to the next filled slot in `direction`, wrapping around; empty slots are skipped.
        private void Cycle(int direction)
        {
            int from = Mathf.Max(0, _activeSlot);
            for (int step = 1; step < SlotCount; step++)
            {
                int slot = ((from + direction * step) % SlotCount + SlotCount) % SlotCount;
                if (_slots[slot] == null) continue;
                EquipSlot(slot);
                return;
            }
        }

        // Only while the previous weapon is still in a slot (not dropped, sold or packed).
        private void EquipLast()
        {
            int slot = _lastWeapon != null ? System.Array.IndexOf(_slots, _lastWeapon) : -1;
            if (slot >= 0) EquipSlot(slot);
        }

        public void EquipSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return;
            if (_activeSlot == index) return;
            Equip(index);
        }

        // Puts `weapon` in the first empty slot and equips it; with every slot full it
        // replaces the active weapon instead. Returns the replaced weapon, or null.
        public WeaponItem AddWeapon(WeaponItem weapon)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] != null) continue;
                _slots[i] = weapon;
                Equip(i);
                return null;
            }

            int target = Mathf.Max(0, _activeSlot);
            WeaponItem replaced = _slots[target];
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
                    if (item is not WeaponItem spare) continue;
                    _wheelWeapons.Add(spare);
                    _wheelLabels.Add($"{spare.DisplayName}  (pack)");
                }
            return _wheelLabels;
        }

        private void ChooseFromWheel(int slot, int option)
        {
            if (option < 0 || option >= _wheelWeapons.Count) return;
            WeaponItem chosen = _wheelWeapons[option];

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
            foreach (WeaponItem weapon in _slots)
                weapon?.Refill();

            if (_activeSlot >= 0) Equip(_activeSlot);
        }

        private void Equip(int index)
        {
            WeaponItem equipping = _slots[index];
            bool changed = _equippedWeapon != equipping;
            if (_equippedWeapon != null && changed) _lastWeapon = _equippedWeapon;
            _equippedWeapon = equipping;
            _activeSlot = index;
            _weapon.Equip(_slots[index] as WeaponInstance);
            if (_melee != null) _melee.Equip(_slots[index] as MeleeWeaponInstance);
            RefreshInspectorView();
            if (changed) ActiveChanged?.Invoke(equipping);
        }

        private void RefreshInspectorView()
        {
            if (_equippedWeapons == null || _equippedWeapons.Length != SlotCount)
                _equippedWeapons = new ScriptableObject[SlotCount];

            for (int i = 0; i < SlotCount; i++)
                _equippedWeapons[i] = DataOf(_slots[i]);

            _activeWeapon = DataOf(_slots[_activeSlot]);
        }

        private static ScriptableObject DataOf(WeaponItem weapon) => weapon switch
        {
            WeaponInstance firearm    => firearm.Data,
            MeleeWeaponInstance melee => melee.Data,
            _                         => null,
        };
    }
}
