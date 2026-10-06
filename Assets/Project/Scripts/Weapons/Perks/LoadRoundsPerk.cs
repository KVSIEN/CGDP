using UnityEngine;

namespace CGD.Weapons
{
    // Loads part or all of the magazine on the spot, e.g. "dodging reloads this weapon".
    // Rounds come out of the reserve like a normal reload unless the perk makes them.
    [CreateAssetMenu(fileName = "LoadRoundsPerk", menuName = "CGD/Weapons/Perks/Load Rounds")]
    public class LoadRoundsPerk : WeaponPerk
    {
        [Tooltip("Share of the magazine loaded (1 = a full reload). Always at least one round")]
        [SerializeField, Range(0.01f, 1f)] private float _magazineFraction = 1f;
        [Tooltip("Rounds appear without touching the reserve")]
        [SerializeField] private bool _free;

        public override bool Fits(WeaponItem weapon) => weapon is WeaponInstance && base.Fits(weapon);

        public override bool Apply(PerkContext context)
        {
            if (context.Firearm == null || context.Firearm.Current != context.Weapon) return false;

            int rounds = Mathf.Max(1, Mathf.CeilToInt(context.Firearm.Current.MagazineSize * _magazineFraction));
            return context.Firearm.LoadRounds(rounds, _free) > 0;
        }
    }
}
