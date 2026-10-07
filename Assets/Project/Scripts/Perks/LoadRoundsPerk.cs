using UnityEngine;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Perks
{
    // Loads part or all of the gun in hand's magazine on the spot, e.g. "dodging reloads this
    // weapon". On a weapon it only loads that weapon; on armor, whichever gun is held. Rounds
    // come out of the reserve like a normal reload unless the perk makes them.
    [CreateAssetMenu(fileName = "LoadRoundsPerk", menuName = "CGD/Perks/Load Rounds")]
    public class LoadRoundsPerk : TriggeredPerk
    {
        [Tooltip("Share of the magazine loaded (1 = a full reload). Always at least one round")]
        [SerializeField, Range(0.01f, 1f)] private float _magazineFraction = 1f;
        [Tooltip("Rounds appear without touching the reserve")]
        [SerializeField] private bool _free;

        public override bool Fits(ItemInstance gear) => gear is not MeleeWeaponInstance && base.Fits(gear);

        public override bool Apply(PerkContext context)
        {
            WeaponInstance gun = context.HeldFirearm;
            if (gun == null) return false;

            int rounds = Mathf.Max(1, Mathf.CeilToInt(gun.MagazineSize * _magazineFraction));
            return context.Firearm.LoadRounds(rounds, _free) > 0;
        }
    }
}
