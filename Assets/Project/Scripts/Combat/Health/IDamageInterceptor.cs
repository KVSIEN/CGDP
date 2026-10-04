using UnityEngine;

namespace CGD.Combat
{
    // Gets a say in every hit a character takes, after armour and before shields and
    // health — a raised guard, a damage-absorbing barrier. Put it on the same object as the
    // HealthManager; it is found once on Awake.
    public interface IDamageInterceptor
    {
        // Returns the damage that gets through (0 = fully stopped).
        float Intercept(in DamageInfo info, float amount, Vector3 point);
    }
}
