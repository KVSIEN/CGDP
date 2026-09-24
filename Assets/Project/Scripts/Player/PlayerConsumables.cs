using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Input;
using CGD.Items;
using CGD.Meters;
using CGD.Stats;

namespace CGD.Player
{
    // Quick-use slots for consumables. Pressing a slot's key starts using one: after its
    // cast time the item is taken from the inventory and its effects apply. The use is
    // cancelled (and the item kept) when the player takes damage (if the item says so),
    // is stunned, mantles or rolls, or presses the key again.
    [RequireComponent(typeof(PlayerInputHandler), typeof(PlayerInventory))]
    public class PlayerConsumables : MonoBehaviour
    {
        public const int SlotCount = 2;

        private static readonly GameAction[] SlotActions = { GameAction.QuickUse1, GameAction.QuickUse2 };

        [SerializeField] private ConsumableDefinition[] _slots = new ConsumableDefinition[SlotCount];

        private PlayerInputHandler     _input;
        private PlayerInventory        _inventory;
        private PlayerMovement         _movement;
        private PlayerHealth           _health;
        private StatusEffectController _statusEffects;
        private CharacterStats         _stats;
        private MeterSet               _meters;

        private ConsumableDefinition _using;
        private float _remaining;

        public IReadOnlyList<ConsumableDefinition> Slots => _slots;
        public ConsumableDefinition Using => _using;
        public bool IsUsing => _using != null;
        // 0..1 through the current use.
        public float UseProgress => IsUsing && _using.CastTime > 0f ? 1f - _remaining / _using.CastTime : 0f;

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
            if (_slots.Length != SlotCount) Array.Resize(ref _slots, SlotCount);
        }

        private void OnEnable()
        {
            if (_health != null) _health.OnDamaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDamaged -= OnDamaged;
            Cancel();
        }

        private void Update()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (!_input.GetAction(SlotActions[i])) continue;

                if (IsUsing) Cancel();
                else         TryUse(_slots[i]);
                return;
            }

            if (!IsUsing) return;

            if (_movement != null && !_movement.CanAct)
            {
                Cancel();
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f) Finish();
        }

        public void SetSlot(int index, ConsumableDefinition item)
        {
            if (index < 0 || index >= SlotCount || _slots[index] == item) return;

            _slots[index] = item;
            SlotsChanged?.Invoke();
        }

        public int CountOf(int slot) => _slots[slot] != null ? _inventory.Inventory.CountOf(_slots[slot]) : 0;

        public bool TryUse(ConsumableDefinition item)
        {
            if (item == null || IsUsing || !_inventory.Inventory.Has(item, 1)) return false;
            if (_movement != null && !_movement.CanAct) return false;
            if (_health != null && _health.IsDead) return false;

            _using     = item;
            _remaining = item.CastTime;
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
