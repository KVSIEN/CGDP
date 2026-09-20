using UnityEngine;

namespace CGD.Items
{
    // A craftable modifier fitted into a gear attachment slot. Attachments have no
    // rolled stats of their own — their deltas are authored and fixed, and a better
    // attachment is a separate higher-Tier definition. That keeps them in the
    // stackable family and makes crafting output predictable.
    //
    // Fitting is reversible: the deltas resolve on top of the gear's base roll
    // rather than being written into it, so detaching restores the original item.
    [CreateAssetMenu(fileName = "NewAttachment", menuName = "CGD/Items/Attachment")]
    public class AttachmentDefinition : ItemDefinition
    {
        [Header("Attachment")]
        [Tooltip("Stat changes applied while fitted. Negative values are the drawback side.")]
        [SerializeField] private StatModifier[] _modifiers;

        [Tooltip("Gear whose slot type matches can host this. Empty = fits anything.")]
        [SerializeField] private EquipmentSlot[] _armorSlots;

        [SerializeField] private int _maxStack = 20;

        public StatModifier[]  Modifiers  => _modifiers;
        public EquipmentSlot[] ArmorSlots => _armorSlots;

        public override int MaxStack => Mathf.Max(1, _maxStack);
    }
}
