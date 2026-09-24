using System;
using UnityEngine;
using CGD.Input;
using CGD.Items;
using CGD.Player;
using CGD.Weapons;

namespace CGD.UI
{
    // Character window (Tab): what the player wears, their weapons and fitted attachments
    // on the left; armor, attachments and consumables in the pack on the right.
    //   click worn armor        → take it off
    //   click pack armor        → wear it
    //   click an attachment     → pick it, then click a gear line on the left to fit it
    //   click a fitted one      → remove it back to the pack
    //   click a consumable      → cycle which quick-use slot it sits in
    public class CharacterPanel : ModalPanel
    {
        [SerializeField] private PlayerInventory   _inventory;
        [SerializeField] private PlayerEquipment   _equipment;
        [SerializeField] private PlayerWeaponLoadout _loadout;
        [SerializeField] private PlayerConsumables _consumables;

        private UIButtonList _worn;
        private UIButtonList _pack;
        private AttachmentDefinition _selected;

        protected override string  Title      => "Character";
        protected override Vector2 WindowSize => new(760f, 520f);

        protected override void Build(RectTransform content)
        {
            (RectTransform left, RectTransform right) = Columns(content);
            _worn = new UIButtonList(left);
            _pack = new UIButtonList(right);
        }

        private void Update()
        {
            if (_input == null || !_input.WasPressedRaw(GameAction.Character)) return;
            if (IsVisible && !OpenedThisFrame) Hide();
            else if (!IsVisible && !IsBlocked(this)) Show();
        }

        public override void Refresh()
        {
            if (!IsVisible || _inventory == null) return;
            RebuildWorn();
            RebuildPack();
        }

        protected override void OnOpened()
        {
            _selected = null;
            _inventory.Inventory.Changed += Refresh;
            if (_equipment != null) _equipment.Changed += Refresh;
            Refresh();
        }

        protected override void OnClosed()
        {
            _selected = null;
            if (_inventory == null) return;
            _inventory.Inventory.Changed -= Refresh;
            if (_equipment != null) _equipment.Changed -= Refresh;
        }

        private void RebuildWorn()
        {
            _worn.Begin();
            _worn.Heading(_selected != null ? $"Fit {_selected.DisplayName} to..." : "Worn");

            if (_equipment != null)
            {
                foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                {
                    ItemInstance armor = _equipment.Equipment.Get(slot);
                    if (armor == null) _worn.Label($"{slot}: —");
                    else               GearLine($"{slot}: {Describe(armor)}", armor, () => _equipment.Unwear(slot));
                }
            }

            if (_loadout != null)
            {
                _worn.Heading("Weapons");
                for (int i = 0; i < _loadout.Slots.Count; i++)
                {
                    WeaponInstance weapon = _loadout.Slots[i];
                    if (weapon == null) _worn.Label($"{i + 1}: —");
                    else                GearLine($"{i + 1}: {Describe(weapon)}", weapon, null);
                }
            }
            _worn.End();
        }

        // In fitting mode every gear line is a fit target; otherwise it runs its own action.
        // Fitted attachments are listed under it and can be removed.
        private void GearLine(string text, ItemInstance gear, Action onClick)
        {
            if (_selected != null)
                _worn.Add(text, () => Fit(gear), gear.CanHost(_selected));
            else if (onClick != null)
                _worn.Add(text, () => onClick());
            else
                _worn.Label(text);

            foreach (AttachmentDefinition attachment in gear.Attachments)
                _worn.Add($"    - {attachment.DisplayName}  (remove)", () => _equipment.Unfit(attachment, gear), _equipment != null);
        }

        private void RebuildPack()
        {
            _pack.Begin();
            Inventory inventory = _inventory.Inventory;

            _pack.Heading("Armor");
            bool any = false;
            foreach (ItemInstance item in inventory.Items)
            {
                if (!Equipment.CanWear(item)) continue;
                _pack.Add($"{Describe(item)}  → wear", () => _equipment.Wear(item), _equipment != null);
                any = true;
            }
            if (!any) _pack.Label("none");

            _pack.Heading("Attachments");
            any = false;
            foreach (ItemStack stack in inventory.Stacks)
            {
                if (stack.Definition is not AttachmentDefinition attachment) continue;
                string marker = attachment == _selected ? "> " : "";
                _pack.Add($"{marker}{attachment.DisplayName} ×{stack.Count}", () => Select(attachment), _equipment != null);
                any = true;
            }
            if (!any) _pack.Label("none");

            if (_consumables != null)
            {
                _pack.Heading("Consumables  (click to set quick slot)");
                any = false;
                foreach (ItemStack stack in inventory.Stacks)
                {
                    if (stack.Definition is not ConsumableDefinition consumable) continue;
                    _pack.Add($"{consumable.DisplayName} ×{stack.Count}{SlotSuffix(consumable)}", () => CycleSlot(consumable));
                    any = true;
                }
                if (!any) _pack.Label("none");
            }
            _pack.End();
        }

        private void Select(AttachmentDefinition attachment)
        {
            _selected = _selected == attachment ? null : attachment;
            Refresh();
        }

        private void Fit(ItemInstance gear)
        {
            if (_equipment.Fit(_selected, gear)) _selected = null;
            Refresh();
        }

        // None → slot 1 → slot 2 → none.
        private void CycleSlot(ConsumableDefinition consumable)
        {
            int current = SlotOf(consumable);
            if (current >= 0) _consumables.SetSlot(current, null);

            int next = current + 1;
            if (next < PlayerConsumables.SlotCount) _consumables.SetSlot(next, consumable);
            Refresh();
        }

        private int SlotOf(ConsumableDefinition consumable)
        {
            for (int i = 0; i < PlayerConsumables.SlotCount; i++)
                if (_consumables.Slots[i] == consumable) return i;
            return -1;
        }

        private string SlotSuffix(ConsumableDefinition consumable)
        {
            int slot = SlotOf(consumable);
            return slot >= 0 ? $"   [quick {slot + 1}]" : "";
        }

        private static string Describe(ItemInstance item) =>
            item.AttachmentSlots > 0
                ? $"{item.DisplayName} ({item.Tier})  [{item.Attachments.Count}/{item.AttachmentSlots}]"
                : $"{item.DisplayName} ({item.Tier})";
    }
}
