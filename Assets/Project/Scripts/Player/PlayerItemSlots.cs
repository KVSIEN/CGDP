using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Input;
using CGD.Items;
using CGD.Meters;
using CGD.Settings;
using CGD.Stats;
using CGD.UI;
using CGD.Weapons;

namespace CGD.Player
{
    // The item slots (keys 5–8). Using what's on a slot:
    //   - an instant consumable goes off on key down, so it can be woven into a combo, and
    //     locks every consumable for its shared cooldown.
    //   - a channelled consumable starts on a tap: after its cast time the item is taken from
    //     the inventory and its effects apply. The use is cancelled (and the item kept) when the
    //     player takes damage (if the item says so), is stunned, mantles or rolls, or taps a
    //     channelled item again. It sets no shared cooldown, so an instant item can follow it.
    //   - a throwable is readied in hand by ThrowableController (tap again to put it away).
    // Holding a slot's key opens the item wheel to choose what goes on that slot (for an
    // instant item only if the InstantItemWheel setting is on). With input buffering on, a
    // use that can't happen yet is retried briefly.
    [RequireComponent(typeof(PlayerInputHandler), typeof(PlayerInventory))]
    public class PlayerItemSlots : MonoBehaviour
    {
        public const int SlotCount = 4;

        private static readonly GameAction[] SlotActions = { GameAction.Item1, GameAction.Item2, GameAction.Item3, GameAction.Item4 };
        private const string EmptyOption = "(empty)";

        [Tooltip("Consumables or throwables on each slot at start")]
        [SerializeField] private ItemDefinition[] _slots = new ItemDefinition[SlotCount];
        [Tooltip("Optional — the wheel shown while holding a slot key")]
        [SerializeField] private SlotWheelHUD _wheel;

        private PlayerInputHandler     _input;
        private PlayerInventory        _inventory;
        private PlayerMovement         _movement;
        private PlayerHealth           _health;
        private StatusEffectController _statusEffects;
        private CharacterStats         _stats;
        private MeterSet               _meters;
        private ThrowableController    _throwing;
        private SlotKeyWheel           _keys;

        // The wheel's choices for the slot being held: null = empty, then each item.
        private readonly List<ItemDefinition> _wheelItems = new();
        private readonly List<string>         _wheelLabels = new();
        private static readonly string[]      NoWheel      = Array.Empty<string>();

        private readonly InputBuffer[] _buffers = new InputBuffer[SlotCount];
        private CooldownTimer _sharedCooldown;

        private ConsumableDefinition _using;
        private float _remaining;

        public IReadOnlyList<ItemDefinition> Slots => _slots;
        public ConsumableDefinition Using => _using;
        public bool IsUsing => _using != null;
        // 0..1 through the current use.
        public float UseProgress => IsUsing && _using.CastTime > 0f ? 1f - _remaining / _using.CastTime : 0f;
        // 1 when consumables are usable; rises from 0 while the shared cooldown runs.
        public float CooldownRatio => _sharedCooldown.Ratio;
        public bool  OnCooldown    => !_sharedCooldown.IsReady;

        // A consumable's use begins (instant ones end the same frame).
        public event Action<ConsumableDefinition> UseStarted;
        // (item, completed) — false when interrupted.
        public event Action<ConsumableDefinition, bool> UseEnded;
        public event Action SlotsChanged;

        private void Awake()
        {
            _input     = GetComponent<PlayerInputHandler>();
            _inventory = GetComponent<PlayerInventory>();
            TryGetComponent(out _movement);
            TryGetComponent(out _health);
            TryGetComponent(out _statusEffects);
            TryGetComponent(out _stats);
            TryGetComponent(out _meters);
            TryGetComponent(out _throwing);
            if (_slots.Length != SlotCount) Array.Resize(ref _slots, SlotCount);
            _keys = new SlotKeyWheel(_input, _wheel, SlotActions, "Item");
            for (int i = 0; i < SlotCount; i++) _buffers[i] = new InputBuffer();
        }

        private void OnEnable()
        {
            if (_health != null) _health.OnDamaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDamaged -= OnDamaged;
            _keys.Cancel();
            Cancel();
            foreach (InputBuffer buffer in _buffers) buffer.Clear();
        }

        private void Update()
        {
            _sharedCooldown.Tick(Time.deltaTime);
            _keys.Tick(Time.deltaTime, PressSlot, TapSlot, WheelOptions, ChooseFromWheel);

            if (!IsUsing)
            {
                RetryBuffered();
                return;
            }

            if (_movement != null && !_movement.CanAct)
            {
                Cancel();
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) Finish();
        }

        public static bool IsSlottable(ItemDefinition item) => item is ConsumableDefinition || item is ThrowableDefinition;

        // An item already on another slot moves here; that slot gets what was here.
        public void SetSlot(int index, ItemDefinition item)
        {
            if (index < 0 || index >= SlotCount || _slots[index] == item) return;
            if (item != null && !IsSlottable(item)) return;

            int previous = item != null ? Array.IndexOf(_slots, item) : -1;
            if (previous >= 0) _slots[previous] = _slots[index];
            _slots[index] = item;
            SlotsChanged?.Invoke();
        }

        public int SlotOf(ItemDefinition item) => item != null ? Array.IndexOf(_slots, item) : -1;

        // Key down: instant consumables go off right away instead of waiting for the tap.
        private void PressSlot(int index)
        {
            if (_slots[index] is ConsumableDefinition { IsInstant: true } instant) Use(index, instant);
        }

        private void TapSlot(int index)
        {
            switch (_slots[index])
            {
                case ConsumableDefinition { IsInstant: true }:
                    break; // already used on key down
                case ConsumableDefinition consumable:
                    if (IsUsing) Cancel();
                    else         Use(index, consumable);
                    break;
                case ThrowableDefinition throwable when _throwing != null:
                    Cancel();
                    _throwing.Ready(throwable);
                    break;
            }
        }

        private void Use(int slot, ConsumableDefinition item)
        {
            if (TryUse(item)) _buffers[slot].Clear();
            else if (GameSettings.Current.InputBuffering) _buffers[slot].Press(Time.time);
        }

        // A use that failed shortly before (mid-channel, on cooldown, rolling) goes off as
        // soon as it can, e.g. an instant item pressed just before a channel finishes.
        private void RetryBuffered()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (!_buffers[i].IsPending(Time.time)) continue;
                if (_slots[i] is ConsumableDefinition item && TryUse(item)) _buffers[i].Clear();
                if (IsUsing) return;
            }
        }

        // Empty first, then every slottable item carried, with counts.
        private IReadOnlyList<string> WheelOptions(int slot)
        {
            if (_slots[slot] is ConsumableDefinition { IsInstant: true } && !GameSettings.Current.InstantItemWheel)
                return NoWheel;

            _wheelItems.Clear();
            _wheelLabels.Clear();
            _wheelItems.Add(null);
            _wheelLabels.Add(EmptyOption);

            foreach (ItemStack stack in _inventory.Inventory.Stacks)
            {
                if (!IsSlottable(stack.Definition) || _wheelItems.Contains(stack.Definition)) continue;
                _wheelItems.Add(stack.Definition);
                _wheelLabels.Add($"{stack.Definition.DisplayName} ×{_inventory.Inventory.CountOf(stack.Definition)}");
            }
            return _wheelLabels;
        }

        private void ChooseFromWheel(int slot, int option)
        {
            if (option < 0 || option >= _wheelItems.Count) return;
            SetSlot(slot, _wheelItems[option]);
        }

        public int CountOf(int slot) => _slots[slot] != null ? _inventory.Inventory.CountOf(_slots[slot]) : 0;

        public bool TryUse(ConsumableDefinition item)
        {
            if (item == null || IsUsing || !_sharedCooldown.IsReady || !_inventory.Inventory.Has(item, 1)) return false;
            if (_movement != null && !_movement.CanAct) return false;
            if (_health != null && _health.IsDead) return false;

            _using     = item;
            _remaining = item.CastTime;
            UseStarted?.Invoke(item);
            if (_remaining <= 0f) Finish();
            return true;
        }

        public void Cancel()
        {
            if (!IsUsing) return;

            ConsumableDefinition item = _using;
            _using = null;
            UseEnded?.Invoke(item, false);
        }

        private void OnDamaged(float amount)
        {
            if (IsUsing && _using.InterruptedByDamage) Cancel();
        }

        // The item is only spent once the use completes.
        private void Finish()
        {
            ConsumableDefinition item = _using;
            _using = null;
            if (!_inventory.Inventory.Remove(item, 1)) return;

            Apply(item);
            if (item.IsInstant) _sharedCooldown.Start(item.SharedCooldown);
            UseEnded?.Invoke(item, true);
        }

        private void Apply(ConsumableDefinition item)
        {
            if (item.Heal > 0f && _health != null) _health.Heal(item.Heal);
            if (item.Cleanse && _statusEffects != null) _statusEffects.Clear();
            if (item.Buff != null && _stats != null) _stats.AddTimed(item.Buff, item.BuffDuration);

            if (item.RestoreMeter != null && item.RestoreAmount > 0f && _meters != null
                && _meters.TryGet(item.RestoreMeter, out Meter meter))
                meter.Restore(item.RestoreAmount);
        }
    }
}
