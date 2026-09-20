using CGD.Items;

namespace CGD.Weapons
{
    // Runtime state of one carried weapon, so its ammo survives swaps and drops.
    //
    // Inherits ItemInstance, so a generated weapon carries the same quality, tier
    // and attachment slots as any other piece of gear. Its stats stay on Data rather
    // than in BaseStats because the firing code reads those fields directly.
    public class WeaponInstance : ItemInstance
    {
        public WeaponData Data { get; }
        public int Magazine { get; internal set; }
        public int Reserve  { get; internal set; }

        // Hand-authored weapon placed directly in a scene: no roll behind it, so no
        // quality and no attachment slots.
        public WeaponInstance(WeaponData data) : base(null)
        {
            Data = data;
            Refill();
        }

        internal WeaponInstance(WeaponCategoryData category, ItemRoll roll, WeaponData data)
            : base(category, roll)
        {
            Data = data;
            Refill();
        }

        public void Refill()
        {
            Magazine = Data.MagazineSize;
            Reserve  = Data.GetNormalizedReserveAmmo();
        }
    }
}
