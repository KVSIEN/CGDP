using UnityEngine;

namespace CGD.Combat
{
    // Gets a say in every hit a character takes, after armour and before shields and
    // health — a raised guard, a damage-absorbing barrier. Put it on the same object as the
    // HealthManager; it is found once on Awake.
    public interface IDamageInterceptor
    {
        // Lower runs first; each sees what the previous ones let through
        // (a melee guard runs before a Reflector, so a reflect works on damage taken).
        int Order { get; }

        // Returns the damage that gets through (0 = fully stopped).
        float Intercept(in DamageInfo info, float amount, Vector3 point);
    }
}
