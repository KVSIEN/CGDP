using UnityEngine;
using CGD.Items;

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
    public class WeaponInstance : ItemInstance
    {
        public WeaponData Data { get; }
        public int Magazine { get; internal set; }

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

        // Called on player revive. Refills the loaded mag only — reserve is inventory
        // state and lives outside the weapon.
        public void RefillMagazine()
        {
            if (Data != null) Magazine = MagazineSize;
        }

        // Taking off a bigger magazine can't leave more rounds loaded than now fit.
        private void ClampMagazine()
        {
            if (Data != null) Magazine = Mathf.Min(Magazine, MagazineSize);
        }
    }
}
