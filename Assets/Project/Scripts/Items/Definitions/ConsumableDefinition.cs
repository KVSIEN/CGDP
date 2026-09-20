using UnityEngine;

namespace CGD.Items
{
    // Fixed-effect item. The GDD deliberately excludes consumables from the quality
    // system: a bandage always heals the same amount, which is what makes them the
    // reliable tool in a loadout full of rolled gear.
    //
    // CastTime is the GDD's commitment cost — instant items weave into a combo,
    // channelled ones force the player to find a safe window.
    [CreateAssetMenu(fileName = "NewConsumable", menuName = "CGD/Items/Consumable")]
    public class ConsumableDefinition : ItemDefinition
    {
        [Header("Consumable")]
        [Tooltip("Seconds of channelling before the effect lands. 0 = instant.")]
        [SerializeField] private float _castTime;
        [SerializeField] private int   _maxStack = 20;

        public float CastTime => Mathf.Max(0f, _castTime);

        public override int MaxStack => Mathf.Max(1, _maxStack);
    }
}
