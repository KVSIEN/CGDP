using UnityEngine;
using CGD.Items;

namespace CGD.Weapons
{
    // A grenade (or anything thrown) as an inventory item: looted, bought, crafted and
    // carried like any other item, and put on an item slot to be readied and thrown.
    [CreateAssetMenu(fileName = "NewThrowable", menuName = "CGD/Items/Throwable")]
    public class ThrowableDefinition : ItemDefinition
    {
        [Header("Throwable")]
        [Tooltip("What gets thrown: fuse, blast, smoke cloud, trajectory preview")]
        [SerializeField] private GrenadeData _grenade;
        [SerializeField, Min(1)] private int _maxStack = 3;

        public GrenadeData Grenade => _grenade;
        public override int MaxStack => Mathf.Max(1, _maxStack);
    }
}
