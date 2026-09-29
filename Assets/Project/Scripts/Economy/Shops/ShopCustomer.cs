using UnityEngine;
using CGD.Items;
using CGD.Player;

namespace CGD.Economy
{
    // Who's trading: where their money and goods live. Loadout is optional — without one
    // the customer can't take weapons.
    public class ShopCustomer
    {
        private ShopCustomer(GameObject actor, Inventory inventory, PlayerWeaponLoadout loadout)
        {
            Actor     = actor;
            Inventory = inventory;
            Loadout   = loadout;
        }

        public GameObject          Actor     { get; }
        public Inventory           Inventory { get; }
        public PlayerWeaponLoadout Loadout   { get; }

        public static bool TryFrom(GameObject actor, out ShopCustomer customer)
        {
            customer = null;
            if (actor == null || !actor.TryGetComponent(out PlayerInventory inventory)) return false;

            actor.TryGetComponent(out PlayerWeaponLoadout loadout);
            customer = new ShopCustomer(actor, inventory.Inventory, loadout);
            return true;
        }
    }
}
