using System;
using System.Collections.Generic;

namespace CGD.Items
{
    // A saved Inventory: its stacks and its gear, with items named by ItemCatalog id.
    [Serializable]
    public class InventoryRecord
    {
        public List<StackRecord> stacks = new();
        public List<GearRecord>  gear   = new();

        // Entries that couldn't be recorded (see GearRecord.Capture) are left out and counted.
        public static InventoryRecord Capture(Inventory inventory, ItemCatalog catalog, out int skipped)
        {
            var record = new InventoryRecord();
            skipped = 0;

            foreach (ItemStack stack in inventory.Stacks)
            {
                string id = catalog.IdOf(stack.Definition);
                if (id == null) { skipped++; continue; }
                record.stacks.Add(new StackRecord { item = id, count = stack.Count });
            }

            foreach (ItemInstance item in inventory.Items)
            {
                GearRecord gear = GearRecord.Capture(item, catalog);
                if (gear == null) { skipped++; continue; }
                record.gear.Add(gear);
            }
            return record;
        }

        // Replaces what `into` holds. Returns how many entries couldn't be restored (items
        // removed from the project since, damaged records); everything else still loads.
        public int RestoreInto(Inventory into, ItemCatalog catalog)
        {
            into.Clear();
            int skipped = 0;

            foreach (StackRecord stack in stacks ?? new List<StackRecord>())
            {
                if (stack.count > 0 && catalog.TryGet(stack.item, out ItemDefinition definition)) into.Add(definition, stack.count);
                else skipped++;
            }

            foreach (GearRecord record in gear ?? new List<GearRecord>())
            {
                if (record != null && record.TryRebuild(catalog, out ItemInstance item)) into.Add(item);
                else skipped++;
            }
            return skipped;
        }
    }
}
