using UnityEngine;
using CGD.Meters;
using CGD.Stats;

namespace CGD.Items
{
    // Fixed-effect item. The GDD deliberately excludes consumables from the quality
    // system: a bandage always heals the same amount, which is what makes them the
    // reliable tool in a loadout full of rolled gear.
    //
    // CastTime is the GDD's commitment cost — instant items weave into a combo,
    // channelled ones force the player to find a safe window. Effects land together
    // when the cast finishes; any left empty are skipped. PlayerConsumables uses them.
    [CreateAssetMenu(fileName = "NewConsumable", menuName = "CGD/Items/Consumable")]
    public class ConsumableDefinition : ItemDefinition
    {
        [Header("Consumable")]
        [Tooltip("Seconds of channelling before the effect lands. 0 = instant.")]
        [SerializeField] private float _castTime;
        [Tooltip("Taking damage while channelling cancels the use (the item is kept)")]
        [SerializeField] private bool _interruptedByDamage = true;
        [SerializeField] private int   _maxStack = 20;

        [Header("Effects")]
        [SerializeField, Min(0f)] private float _heal;
        [Tooltip("Removes every status effect (bleed, fire, poison…)")]
        [SerializeField] private bool _cleanse;
        [SerializeField] private StatModifierPreset _buff;
        [SerializeField, Min(0f)] private float _buffDuration = 10f;
        [SerializeField] private MeterDefinition _restoreMeter;
        [SerializeField, Min(0f)] private float _restoreAmount;

        public float CastTime             => Mathf.Max(0f, _castTime);
        public bool  InterruptedByDamage  => _interruptedByDamage;
        public override int MaxStack      => Mathf.Max(1, _maxStack);

        public float              Heal          => _heal;
        public bool               Cleanse       => _cleanse;
        public StatModifierPreset Buff          => _buff;
        public float              BuffDuration  => _buffDuration;
        public MeterDefinition    RestoreMeter  => _restoreMeter;
        public float              RestoreAmount => _restoreAmount;
    }
}
