using System;
using UnityEngine;
using CGD.Input;
using CGD.Items;
using CGD.Perks;
using CGD.Player;
using CGD.Weapons;

namespace CGD.UI
{
    // Character window (Tab): what the player wears and their loadout slots (a main weapon and
    // an offhand each), with perks and fitted attachments, on the left; armor, weapons,
    // attachments and consumables in the pack on the right.
    //   click worn armor        → take it off
    //   click pack armor        → wear it
    //   click an attachment     → pick it, then click a gear line on the left to fit it
    //   click a fitted one      → remove it back to the pack
    //   click a slot item       → cycle which item slot (5–8) it sits in
    //   click a weapon, shield or artifact (pack, or already in a slot) → pick it, then click a
    //     slot's main or offhand line on the left to put it there; one item can fill several slots
    //   click a slot's offhand → empty it (it goes back to the pack unless another slot uses it)
    public class CharacterPanel : ModalPanel
    {
        [SerializeField] private PlayerInventory   _inventory;
        [SerializeField] private PlayerEquipment   _equipment;
        [SerializeField] private PlayerWeaponLoadout _loadout;
        [SerializeField] private PlayerItemSlots _itemSlots;

        private UIButtonList _worn;
        private UIButtonList _pack;
        private AttachmentDefinition _selected;
        // A weapon, shield or artifact picked to put in a loadout slot.
        private ItemInstance _assigning;

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
            _selected  = null;
            _assigning = null;
            _inventory.Inventory.Changed += Refresh;
            if (_equipment != null) _equipment.Changed += Refresh;
            if (_loadout   != null) _loadout.Changed   += Refresh;
            Refresh();
        }

        protected override void OnClosed()
        {
            _selected  = null;
            _assigning = null;
            if (_inventory == null) return;
            _inventory.Inventory.Changed -= Refresh;
            if (_equipment != null) _equipment.Changed -= Refresh;
            if (_loadout   != null) _loadout.Changed   -= Refresh;
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

            if (_loadout != null) RebuildLoadout();
            _worn.End();
        }

        // Each slot: its main and its offhand. With an item picked, the lines are where to put it.
        private void RebuildLoadout()
        {
            _worn.Heading(_assigning != null ? $"Put {_assigning.DisplayName} in..." : "Loadout slots");
            bool pickingWeapon  = _assigning is WeaponItem;
            bool pickingOffhand = _assigning != null && PlayerOffhand.CanHold(_assigning);

            for (int i = 0; i < _loadout.Slots.Count; i++)
            {
                int slot = i;
                WeaponItem   main = _loadout.Slots[i];
                ItemInstance off  = _loadout.Offhands[i];
                string active     = _loadout.ActiveSlot == i ? "  <" : "";
                string mainName   = main != null ? Describe(main) : "—";
                string offName    = off  != null ? Describe(off)  : "—";
                // A two-handed main leaves the offhand in the slot, but unused.
                string unused     = off != null && main != null && !main.IsOneHanded ? "  (unused: two-handed main)" : "";

                if (_assigning != null)
                {
                    _worn.Add($"{i + 1}  Main: {mainName}{active}", () => AssignTo(slot, asMain: true), pickingWeapon);
                    _worn.Add($"      Offhand: {offName}{unused}", () => AssignTo(slot, asMain: false), pickingOffhand);
                    continue;
                }

                if (main == null) _worn.Label($"{i + 1}  Main: —{active}");
                else              GearLine($"{i + 1}  Main: {mainName}{active}", main, null);

                if (off == null)  _worn.Label("      Offhand: —");
                else              GearLine($"      Offhand: {offName}{unused}", off, () => _loadout.AssignOffhand(slot, null));
            }
        }

        private void AssignTo(int slot, bool asMain)
        {
            if (asMain) _loadout.AssignMain(slot, (WeaponItem)_assigning);
            else        _loadout.AssignOffhand(slot, _assigning);
            _assigning = null;
            Refresh();
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

            foreach (GearPerk perk in gear.Perks)
                if (perk != null) _worn.Label($"    * {perk.DisplayName}: {perk.Description}");

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
                _pack.Add($"{DescribeWithPerks(item)}  → wear", () => _equipment.Wear(item), _equipment != null);
                any = true;
            }
            if (!any) _pack.Label("none");

            _pack.Heading("Weapons, shields & artifacts  (click, then pick a slot)");
            any = false;
            foreach (ItemInstance item in inventory.Items)
            {
                if (!IsLoadoutItem(item)) continue;
                _pack.Add($"{(item == _assigning ? "> " : "")}{DescribeWithPerks(item)}", () => PickForSlot(item), _loadout != null);
                any = true;
            }
            if (_loadout != null)
                foreach (ItemInstance item in _loadout.Equipped())
                {
                    _pack.Add($"{(item == _assigning ? "> " : "")}{DescribeWithPerks(item)}  [slot {SlotsText(item)}]", () => PickForSlot(item));
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

            if (_itemSlots != null)
            {
                _pack.Heading("Consumables & throwables  (click to set item slot)");
                any = false;
                foreach (ItemStack stack in inventory.Stacks)
                {
                    ItemDefinition item = stack.Definition;
                    if (!PlayerItemSlots.IsSlottable(item)) continue;
                    _pack.Add($"{item.DisplayName} ×{stack.Count}{SlotSuffix(item)}", () => CycleSlot(item));
                    any = true;
                }
                if (!any) _pack.Label("none");
            }
            _pack.End();
        }

        private void Select(AttachmentDefinition attachment)
        {
            _selected  = _selected == attachment ? null : attachment;
            _assigning = null;
            Refresh();
        }

        private static bool IsLoadoutItem(ItemInstance item) => item is WeaponItem || PlayerOffhand.CanHold(item);

        private void PickForSlot(ItemInstance item)
        {
            _assigning = _assigning == item ? null : item;
            _selected  = null;
            Refresh();
        }

        // "1, 3": the slots an item is in.
        private string SlotsText(ItemInstance item)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < _loadout.Slots.Count; i++)
            {
                if (!ReferenceEquals(_loadout.Slots[i], item) && !ReferenceEquals(_loadout.Offhands[i], item)) continue;
                if (text.Length > 0) text.Append(", ");
                text.Append(i + 1);
            }
            return text.ToString();
        }

        private void Fit(ItemInstance gear)
        {
            if (_equipment.Fit(_selected, gear)) _selected = null;
            Refresh();
        }

        // None → slot 1 → … → slot 4 → none.
        private void CycleSlot(ItemDefinition item)
        {
            int current = _itemSlots.SlotOf(item);
            if (current >= 0) _itemSlots.SetSlot(current, null);

            int next = current + 1;
            if (next < PlayerItemSlots.SlotCount) _itemSlots.SetSlot(next, item);
            Refresh();
        }

        private string SlotSuffix(ItemDefinition item)
        {
            int slot = _itemSlots.SlotOf(item);
            return slot >= 0 ? $"   [item slot {slot + 1}]" : "";
        }

        private static string Describe(ItemInstance item) =>
            item.AttachmentSlots > 0
                ? $"{item.DisplayName} ({item.Tier})  [{item.Attachments.Count}/{item.AttachmentSlots}]"
                : $"{item.DisplayName} ({item.Tier})";

        // Pack lines have no room for a perk list underneath, so the names go inline.
        private static string DescribeWithPerks(ItemInstance item) =>
            item.Perks.Count > 0 ? $"{Describe(item)}  ({item.PerkNames()})" : Describe(item);
    }
}
