using UnityEngine;

namespace CGD.Enemies
{
    // One grenade an enemy throws, as debug views show it: being thrown now, or waiting out its cooldown.
    public readonly struct GrenadeStatus
    {
        public readonly string Name;
        public readonly float  CooldownLeft;
        public readonly bool   Throwing;

        public GrenadeStatus(string name, float cooldownLeft, bool throwing)
        {
            Name         = name;
            CooldownLeft = cooldownLeft;
            Throwing     = throwing;
        }

        public GrenadeStatus Merge(GrenadeStatus other) =>
            new(Name, Mathf.Max(CooldownLeft, other.CooldownLeft), Throwing || other.Throwing);
    }
}
