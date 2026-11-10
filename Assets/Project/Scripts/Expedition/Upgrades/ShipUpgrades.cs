using System;
using System.Collections.Generic;
using CGD.Crafting;
using CGD.Items;

namespace CGD.Expedition
{
    // The levels the ship owns of each upgrade, by id, and the rules for buying the next one:
    // levels are bought in order, each paid in full from the hold or not at all. Ids the game
    // no longer knows are kept as they are, so an upgrade taken out and put back is still owned.
    public class ShipUpgrades
    {
        private readonly Dictionary<string, int> _levels = new();

        public event Action Changed;

        public IReadOnlyDictionary<string, int> Owned => _levels;

        public int LevelOf(ShipUpgrade upgrade) =>
            upgrade != null && _levels.TryGetValue(upgrade.Id, out int level) ? Math.Min(level, upgrade.MaxLevel) : 0;

        public bool IsMaxed(ShipUpgrade upgrade) => LevelOf(upgrade) >= upgrade.MaxLevel;

        // The level that would be bought next, or null once every level is owned.
        public ShipUpgradeLevel Next(ShipUpgrade upgrade) => upgrade.Level(LevelOf(upgrade) + 1);

        public bool CanAfford(ShipUpgrade upgrade, Inventory hold)
        {
            ShipUpgradeLevel next = Next(upgrade);
            if (next == null) return false;

            foreach (ItemAmount cost in next.Cost)
                if (cost.Item != null && !hold.Has(cost.Item, Needed(next, cost.Item))) return false;
            return true;
        }

        public bool TryBuy(ShipUpgrade upgrade, Inventory hold)
        {
            if (upgrade == null || !CanAfford(upgrade, hold)) return false;

            foreach (ItemAmount cost in Next(upgrade).Cost)
                if (cost.Item != null) hold.Remove(cost.Item, cost.SafeCount);

            _levels[upgrade.Id] = LevelOf(upgrade) + 1;
            Changed?.Invoke();
            return true;
        }

        // Every level owned, for the run's player: the bonus of each, lowest first.
        public void CollectBonuses(ShipUpgradeCatalog catalog, List<ShipUpgradeLevel> into)
        {
            into.Clear();
            if (catalog == null) return;

            foreach (ShipUpgrade upgrade in catalog.Upgrades)
            {
                if (upgrade == null) continue;
                int owned = LevelOf(upgrade);
                for (int level = 1; level <= owned; level++) into.Add(upgrade.Level(level));
            }
        }

        public void Restore(IEnumerable<KeyValuePair<string, int>> owned)
        {
            _levels.Clear();
            foreach ((string id, int level) in owned)
                if (!string.IsNullOrEmpty(id) && level > 0) _levels[id] = level;
            Changed?.Invoke();
        }

        public void Clear()
        {
            if (_levels.Count == 0) return;
            _levels.Clear();
            Changed?.Invoke();
        }

        // A cost may list the same item twice; it's paid as the sum.
        private static int Needed(ShipUpgradeLevel level, ItemDefinition item)
        {
            int total = 0;
            foreach (ItemAmount cost in level.Cost)
                if (cost.Item == item) total += cost.SafeCount;
            return total;
        }
    }
}
