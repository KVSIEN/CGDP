using UnityEngine;

namespace CGD.Combat
{
    // Jams the target's abilities while it lasts: the player can't start one (and one being cast
    // fizzles), enemies can't cast theirs. Weapons, melee and dodging still work. A hacker's
    // jamming pulse, a null field. PlayerAbilities and EnemyAbilities ask for it.
    [CreateAssetMenu(fileName = "JamEffect", menuName = "CGD/Combat/Status Effects/Jam")]
    public class JamEffect : StatusEffect
    {
        // Nothing to do over time: the abilities check whether it is on.
        public override void Tick(StatusEffectController target, int stacks, float magnitude) { }
    }
}
