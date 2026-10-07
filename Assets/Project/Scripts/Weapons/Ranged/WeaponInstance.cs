using UnityEngine;
using CGD.Items;
using CGD.Perks;

namespace CGD.Weapons
{
    // Runtime state of one carried weapon: the loaded magazine. Reserve ammo is no
    // longer per-weapon — it lives in the player's Inventory as shared pools by
    // AmmoType, so dropping and picking up a weapon preserves its mag but never
    // grants extra reserve.
    //
    // Inherits ItemInstance, so a generated weapon carries the same quality, tier
    // and attachment slots as any other piece of gear. Its stats stay on Data rather
    // than in BaseStats because the firing code reads those fields directly; the
    // properties below are those fields with fitted attachments applied.
    public class WeaponInstance : WeaponItem
    {
        public WeaponData Data { get; }
        public int Magazine { get; internal set; }
        // How a bow is being held; each bow remembers its own. Ignored by weapons without draw stances.
        public DrawOrientation Draw { get; set; }
        // Reload swap-cancel: swapped away once its rounds were in, so swapping back
        // before this game time skips the draw.
        public float QuickDrawUntil { get; set; } = float.NegativeInfinity;

        public override string DisplayName => Data != null ? Data.WeaponName : "Weapon";

        // Hand-authored weapon placed directly in a scene: no roll behind it, so no
        // quality and no attachment slots. Spawns loaded.
        public WeaponInstance(WeaponData data) : base(null)
        {
            Data     = data;
            Magazine = data != null ? data.MagazineSize : 0;
        }

        internal WeaponInstance(WeaponCategoryData category, ItemRoll roll, WeaponData data)
            : base(category, roll)
        {
            Data     = data;
            Magazine = data != null ? data.MagazineSize : 0;
            AttachmentsChanged += ClampMagazine;
        }

        public float Damage             => Modify(ItemStat.Damage, Data.Damage);
        public int   MagazineSize       => Mathf.Max(1, Mathf.RoundToInt(Modify(ItemStat.MagazineSize, Data.MagazineSize)));
        public float ReloadTime         => Mathf.Max(0.1f, Modify(ItemStat.ReloadTime, Data.ReloadTime));
        public float TacticalReloadTime => Mathf.Max(0.1f, Modify(ItemStat.ReloadTime, Data.TacticalReloadTime));
        public float RoundsPerMinute    => Mathf.Max(1f, Modify(ItemStat.FireRate, Data.RoundsPerMinute));
        public float DrawTime           => Mathf.Max(0f, Modify(ItemStat.DrawTime, Data.DrawTime));
        public float ArmorPenetration   => Modify(ItemStat.ArmorPenetration, Data.ArmorPenetration);
        // The headshot (critical) multiplier; CritDamage raises it.
        public float CriticalMultiplier => Modify(ItemStat.CritDamage, Data.HeadshotMultiplier);

        // Stats the weapon doesn't roll itself, so they start from a neutral base: chances and
        // bonuses from 0, scales from 1. Perks and attachments move them.
        public float CritChance           => Modify(ItemStat.CritChance, 0f);
        public float StatusChance         => Modify(ItemStat.StatusChance, 0f);
        public float StatusDamage         => Modify(ItemStat.StatusDamage, 0f);
        public float Multishot            => Modify(ItemStat.Multishot, 0f);
        public float RangeScale           => Modify(ItemStat.Range, 1f);
        public float ProjectileSpeedScale => Modify(ItemStat.ProjectileSpeed, 1f);
        public float SpreadScale          => Modify(ItemStat.Spread, 1f);
        public float RecoilScale          => Modify(ItemStat.Recoil, 1f);

        // What the Melee key does with this gun in hand: its own bash, unless a perk (Tactical
        // Knife) swaps in another. Null = fists.
        public MeleeWeaponData QuickMelee
        {
            get
            {
                foreach (GearPerk perk in Perks)
                    if (perk is QuickMeleePerk swap && swap.QuickMelee != null) return swap.QuickMelee;
                return Data != null ? Data.QuickMelee : null;
            }
        }

        // Called on player revive. Refills the loaded mag only — reserve is inventory
        // state and lives outside the weapon.
        public void RefillMagazine()
        {
            if (Data != null) Magazine = MagazineSize;
        }

        public override void Refill() => RefillMagazine();

        // Taking off a bigger magazine can't leave more rounds loaded than now fit.
        private void ClampMagazine()
        {
            if (Data != null) Magazine = Mathf.Min(Magazine, MagazineSize);
        }
    }
}
