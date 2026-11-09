using UnityEngine;
using CGD.Abilities;
using CGD.Stealth;

namespace CGD.Enemies
{
    // An enemy's armed ambush strike: while it lasts, the next attack deals extra damage and
    // gives the enemy away (ends its cloak). Used up by that attack, or when time runs out.
    internal sealed class EnemyAmbush : IAmbushStrike
    {
        private readonly Stealthable _stealth;

        private float _multiplier = 1f;
        private float _expiresAt;

        public EnemyAmbush(Stealthable stealth) => _stealth = stealth;

        public bool IsArmed => _multiplier > 1f && Time.time < _expiresAt;

        public void Arm(float damageMultiplier, float seconds)
        {
            _multiplier = damageMultiplier;
            _expiresAt  = Time.time + seconds;
        }

        // The damage multiplier for the attack landing now (1 = not armed), spending the ambush.
        public float Consume()
        {
            float multiplier = IsArmed ? _multiplier : 1f;
            Disarm();
            if (multiplier > 1f && _stealth != null) _stealth.EndCloak();
            return multiplier;
        }

        public void Disarm() => _multiplier = 1f;
    }
}
