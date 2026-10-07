using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Input;
using CGD.Items;
using CGD.UI;
using CGD.Weapons;

namespace CGD.Player
{
    // Owns the loadout: four slots, each a main weapon (a firearm with its own ammo, or a melee
    // weapon) and an offhand (a shield, an artifact or a one-handed melee weapon). Equipping a
    // slot hands its main to the controller — WeaponController for firearms, MeleeController
    // for melee weapons — and its offhand to PlayerOffhand; a main that is the same weapon as
    // before isn't redrawn, so slots sharing a gun differ only by their offhand. Refills every
    // weapon when the player is revived.
    // An item can sit in several slots at once (one shield behind a sword in slot 1 and a pistol
    // in slot 3): slots hold references, and an item leaves the pack when first slotted and
    // returns when no slot uses it. A two-handed main keeps its slot's offhand stored but unused.
    // Pressing a slot key equips that slot; holding it opens the weapon wheel to put another
    // weapon there — a whole slot from elsewhere (they swap) or a spare from the pack (the old
    // main goes to the pack unless another slot uses it). Next/Previous Weapon (scroll wheel)
    // cycle through the filled slots, and Last Weapon swaps back to the slot used before.
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

        private readonly WeaponItem[]   _slots    = new WeaponItem[SlotCount];
        private readonly ItemInstance[] _offhands = new ItemInstance[SlotCount];
        // Rebuilt when the loadout changes, so the HUD can read them every frame without allocating.
        private readonly string[]       _labels   = new string[SlotCount];

        public IReadOnlyList<WeaponItem>   Slots      => _slots;
        // Parallel to Slots: each slot's offhand (null = none). Unused while its main is two-handed.
        public IReadOnlyList<ItemInstance> Offhands   => _offhands;
        public int                         ActiveSlot => _activeSlot;
        // The weapon in hand (null with an empty slot).
        public WeaponItem                  Active     => _equippedWeapon;
        // The active slot's offhand, whether or not the main hand leaves room for it.
        public ItemInstance                ActiveOffhand => _activeSlot >= 0 ? _offhands[_activeSlot] : null;

        // A different main weapon is now in hand.
        public event System.Action<WeaponItem> ActiveChanged;
        // Anything about the loadout changed: what a slot holds, or which slot is active.
        public event System.Action Changed;

        private PlayerInputHandler _input;
        private WeaponController   _weapon;
        private MeleeController    _melee;
        private HealthManager      _health;
        private int                _activeSlot = -1;
        private int                _lastSlot   = -1;
        private WeaponItem         _equippedWeapon;
        private PlayerInventory    _inventory;
        private SlotKeyWheel       _keys;

        // The wheel's choices for the slot being held, in order: a whole slot from elsewhere
        // (FromSlot) or a spare main from the pack.
        private readonly List<(int FromSlot, WeaponItem Spare)> _wheelChoices = new();
        private readonly List<string> _wheelLabels = new();

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
            RebuildLabels();
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

        // A slot counts as filled with a main or just an offhand (fists and a shield).
        public bool IsFilled(int slot) => _slots[slot] != null || _offhands[slot] != null;

        // Steps to the next filled slot in `direction`, wrapping around; empty slots are skipped.
        private void Cycle(int direction)
        {
            int from = Mathf.Max(0, _activeSlot);
            for (int step = 1; step < SlotCount; step++)
            {
                int slot = ((from + direction * step) % SlotCount + SlotCount) % SlotCount;
                if (!IsFilled(slot)) continue;
                EquipSlot(slot);
                return;
            }
        }

        // Only while the previous slot still holds something.
        private void EquipLast()
        {
            if (_lastSlot >= 0 && IsFilled(_lastSlot)) EquipSlot(_lastSlot);
        }

        public void EquipSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return;
            if (_activeSlot == index) return;
            Equip(index);
        }

        // "Glock 17", or with an offhand "Glock 17 (+Tactical Knife)"; "Fists" for an offhand alone.
        public string SlotLabel(int slot) => _labels[slot];

        private void RebuildLabels()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                WeaponItem main  = _slots[i];
                ItemInstance off = _offhands[i];
                string name = main != null ? main.DisplayName : "Fists";
                _labels[i] = main == null && off == null ? "— Empty —"
                           : off != null ? $"{name} (+{off.DisplayName})"
                           : name;
            }
        }

        private void RaiseChanged()
        {
            RebuildLabels();
            Changed?.Invoke();
        }

        // Puts `weapon` in the first slot with no main and equips it; with every slot full it
        // replaces the active slot's main instead. Returns the weapon that lost its place, or
        // null — also when another slot still uses it.
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
            return IsEquipped(replaced) ? null : replaced;
        }

        // ---- Items in slots ----

        public bool IsEquipped(ItemInstance item) => SlotsUsing(item) > 0;

        // How many slots hold it, as a main or an offhand.
        public int SlotsUsing(ItemInstance item)
        {
            if (item == null) return 0;

            int count = 0;
            for (int i = 0; i < SlotCount; i++)
                if (ReferenceEquals(_slots[i], item) || ReferenceEquals(_offhands[i], item)) count++;
            return count;
        }

        // Every item any slot holds, once each — what the player carries besides the pack.
        public IEnumerable<ItemInstance> Equipped()
        {
            var seen = new HashSet<ItemInstance>();
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] != null && seen.Add(_slots[i])) yield return _slots[i];
                if (_offhands[i] != null && seen.Add(_offhands[i])) yield return _offhands[i];
            }
        }

        // Puts a weapon from the pack, or one already in another slot, in a slot's main place.
        public bool AssignMain(int slot, WeaponItem weapon)
        {
            if (!IsSlot(slot) || weapon == null || ReferenceEquals(weapon, _offhands[slot])) return false;
            if (_slots[slot] == weapon) return true;
            if (!TakeFromPack(weapon)) return false;

            WeaponItem previous = _slots[slot];
            _slots[slot] = weapon;
            ReturnToPack(previous);
            AfterSlotChange(slot);
            return true;
        }

        // Puts a shield, artifact or one-handed melee weapon in a slot's offhand, from the pack or
        // another slot; null empties it. The old one goes to the pack unless another slot uses it.
        public bool AssignOffhand(int slot, ItemInstance item)
        {
            if (!IsSlot(slot)) return false;
            if (item != null && (!PlayerOffhand.CanHold(item) || ReferenceEquals(item, _slots[slot]))) return false;
            if (_offhands[slot] == item) return true;
            if (item != null && !TakeFromPack(item)) return false;

            ItemInstance previous = _offhands[slot];
            _offhands[slot] = item;
            ReturnToPack(previous);
            AfterSlotChange(slot);
            return true;
        }

        // For gear handed over at the start of a run: the first slot with a free offhand that its main leaves room for.
        public bool AssignOffhandToFreeSlot(ItemInstance item)
        {
            for (int i = 0; i < SlotCount; i++)
                if (_offhands[i] == null && (_slots[i] == null || _slots[i].IsOneHanded) && AssignOffhand(i, item))
                    return true;
            return false;
        }

        private static bool IsSlot(int slot) => slot >= 0 && slot < SlotCount;

        // Slotting an item takes it out of the pack, unless a slot already holds it.
        private bool TakeFromPack(ItemInstance item) =>
            IsEquipped(item) || (_inventory != null && _inventory.Inventory.Remove(item));

        private void ReturnToPack(ItemInstance item)
        {
            if (item != null && _inventory != null && !IsEquipped(item)) _inventory.Inventory.Add(item);
        }

        private void AfterSlotChange(int slot)
        {
            if (slot == _activeSlot) Equip(slot);
            else                     RaiseChanged();
        }

        // ---- Weapon wheel ----

        // Every filled slot, then the spare weapons in the pack.
        private IReadOnlyList<string> WheelOptions(int slot)
        {
            _wheelChoices.Clear();
            _wheelLabels.Clear();

            for (int i = 0; i < SlotCount; i++)
            {
                if (!IsFilled(i)) continue;
                _wheelChoices.Add((i, null));
                _wheelLabels.Add(i == slot ? $"{SlotLabel(i)}  (here)" : $"{SlotLabel(i)}  [{i + 1}]");
            }

            if (_inventory != null)
                foreach (ItemInstance item in _inventory.Inventory.Items)
                {
                    if (item is not WeaponItem spare) continue;
                    _wheelChoices.Add((-1, spare));
                    _wheelLabels.Add($"{spare.DisplayName}  (pack)");
                }
            return _wheelLabels;
        }

        private void ChooseFromWheel(int slot, int option)
        {
            if (option < 0 || option >= _wheelChoices.Count) return;

            (int from, WeaponItem spare) = _wheelChoices[option];
            if (from >= 0) SwapSlots(from, slot);
            else           AssignMain(slot, spare);
        }

        // Rearranging: both slots trade their main and their offhand.
        private void SwapSlots(int a, int b)
        {
            if (a == b) return;

            (_slots[a], _slots[b])       = (_slots[b], _slots[a]);
            (_offhands[a], _offhands[b]) = (_offhands[b], _offhands[a]);
            if      (_lastSlot == a) _lastSlot = b;
            else if (_lastSlot == b) _lastSlot = a;

            if (_activeSlot == a || _activeSlot == b) Equip(_activeSlot);
            else                                      RaiseChanged();
        }

        public void RefillAll()
        {
            foreach (WeaponItem weapon in _slots)
                weapon?.Refill();

            if (_activeSlot >= 0) Equip(_activeSlot, force: true);
        }

        // A main that is already in hand isn't drawn again, so moving between slots that share a
        // weapon only changes the offhand. `force` re-equips regardless (revive).
        private void Equip(int index, bool force = false)
        {
            WeaponItem equipping = _slots[index];
            bool first       = _activeSlot < 0;
            bool mainChanged = _equippedWeapon != equipping;
            if (!first && _activeSlot != index) _lastSlot = _activeSlot;

            _equippedWeapon = equipping;
            _activeSlot     = index;
            if (first || mainChanged || force)
            {
                _weapon.Equip(equipping as WeaponInstance);
                if (_melee != null) _melee.Equip(equipping);
            }

            RefreshInspectorView();
            if (mainChanged) ActiveChanged?.Invoke(equipping);
            RaiseChanged();
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
