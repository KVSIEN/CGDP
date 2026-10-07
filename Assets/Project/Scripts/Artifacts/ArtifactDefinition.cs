using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Stats;
using CGD.Weapons;

namespace CGD.Artifacts
{
    // An offhand artifact type — a relic, a tome, a mirror, a lantern — rolled into individual
    // artifacts like a weapon category. It can do two things, together or alone:
    //   • passively, while held: its rolled stats (a curse is just a negative range), passive
    //     perks and triggered perks, applied to the wielder like worn armor;
    //   • actively, through an OffhandBehavior used on the aim input. An active artifact takes
    //     the aim input, so a gun can't aim while it is held — the trade shields make too.
    // Quality decides where its stats land and how potent its behavior is.
    [CreateAssetMenu(fileName = "ArtifactDefinition", menuName = "CGD/Artifacts/Artifact")]
    public class ArtifactDefinition : GearDefinition
    {
        [Header("Use")]
        [Tooltip("What the aim input does while it is held. Empty = passive only (the aim input stays free)")]
        [SerializeField] private OffhandBehavior _behavior;
        [Tooltip("Scales the strength of its behavior: ability power, channel modifiers. Min = worst roll, Max = best roll")]
        [SerializeField] private FloatRange _potency = new(0.8f, 1.25f);

        [Header("Offhand")]
        [Tooltip("What holding it costs the main-hand gun")]
        [SerializeField] private StatModifier[] _mainHandPenalty = System.Array.Empty<StatModifier>();

        public OffhandBehavior Behavior => _behavior;
        public FloatRange Potency => _potency;
        public StatModifier[] MainHandPenalty => _mainHandPenalty;

        public override ItemInstance CreateInstance(ItemRoll roll) => new ArtifactInstance(this, roll);
    }
}
