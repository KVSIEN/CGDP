using CGD.Items;
using CGD.Weapons;

namespace CGD.Economy
{
    // One specific, already-rolled weapon. It goes straight into the loadout: the first
    // free slot, or in place of the active weapon, which the shop then takes in trade.
    public class WeaponListing : ShopListing
    {
        public WeaponListing(WeaponItem weapon, int price) : base(price, 1) => Weapon = weapon;

        public WeaponItem Weapon { get; }

        public override string   Name    => Weapon.DisplayName;
        public override ItemTier Tier    => Weapon.Tier;
        public override string   Details => Weapon.Perks.Count > 0
            ? $"{Weapon.Definition?.DisplayName} · Q{Weapon.Quality} · {Weapon.PerkNames()}"
            : $"{Weapon.Definition?.DisplayName} · Q{Weapon.Quality}";

        public override string CannotDeliverReason(ShopCustomer customer) =>
            customer.Loadout == null ? "No weapon slots" : null;

        public override ItemInstance WouldDisplace(ShopCustomer customer)
        {
            var loadout = customer.Loadout;
            if (loadout == null) return null;

            foreach (WeaponItem slot in loadout.Slots)
                if (slot == null) return null;

            return loadout.Slots[System.Math.Max(0, loadout.ActiveSlot)];
        }

        public override ItemInstance Deliver(ShopCustomer customer) => customer.Loadout.AddWeapon(Weapon);
    }
}
