using UnityEngine;

namespace CGD.Items
{
    // A wearable piece. Which stats it can roll is authored per asset via
    // StatRanges, so a helm and a backslot cape can draw from different pools
    // without needing separate types.
    [CreateAssetMenu(fileName = "NewArmor", menuName = "CGD/Items/Armor")]
    public class ArmorDefinition : GearDefinition
    {
        [Header("Armor")]
        [SerializeField] private EquipmentSlot _slot = EquipmentSlot.Torso;

        public EquipmentSlot Slot => _slot;
    }
}
