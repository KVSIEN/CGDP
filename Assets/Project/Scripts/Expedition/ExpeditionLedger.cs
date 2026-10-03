using System.Collections.Generic;
using CGD.Items;

namespace CGD.Expedition
{
    // The extraction loop's bookkeeping (GDD › Core Loop), Unity-independent:
    //   Storage — the docked ship's hold; survives every run.
    //   Kit     — what the player has packed to bring next; still theirs until deployed.
    //   Brought — what went into the current run from the ship.
    // Deploy hands the kit to the run. Extract moves everything carried into storage;
    // Die keeps storage as it is — what was brought and gathered is gone. An emergency
    // extraction is in between: worn gear and half of every stack make it, loose gear doesn't.
    public class ExpeditionLedger
    {
        public Inventory Storage { get; } = new();
        public Inventory Kit     { get; } = new();

        public bool IsRunning { get; private set; }
        public int  Brought   { get; private set; }
        // Null until a run has ended.
        public RunReport? LastReport { get; private set; }

        // Packing moves things between the hold and the kit; nothing is lost either way.
        public bool Pack(ItemDefinition definition, int count)   => Move(Storage, Kit, definition, count);
        public bool Unpack(ItemDefinition definition, int count) => Move(Kit, Storage, definition, count);
        public bool Pack(ItemInstance item)   => Move(Storage, Kit, item);
        public bool Unpack(ItemInstance item) => Move(Kit, Storage, item);

        // Starts a run with the packed kit: returns what to give the player and empties the kit.
        public Inventory Deploy()
        {
            var handed = new Inventory();
            TransferAll(Kit, handed);
            Brought   = CountUnits(handed);
            IsRunning = true;
            return handed;
        }

        // The player made it out with `carried`: all of it goes into the hold.
        public RunReport Extract(Inventory carried) => End(RunOutcome.Extracted, carried);

        // Out through an escape pod: `worn` (weapons in hand, armour) is kept whole, of the
        // `pack` only half of each stack (rounded down) — its loose gear is left behind.
        public RunReport ExtractEmergency(Inventory worn, Inventory pack)
        {
            var kept = new Inventory();
            TransferAll(worn, kept);
            foreach (ItemStack stack in new List<ItemStack>(pack.Stacks))
            {
                int half = stack.Count / 2;
                if (half > 0 && pack.Remove(stack.Definition, half)) kept.Add(stack.Definition, half);
            }

            var report = new RunReport(RunOutcome.EmergencyExtracted, CountUnits(kept), Brought, CountUnits(pack));
            TransferAll(kept, Storage);
            Finish(report);
            return report;
        }

        // The player died carrying `carried`: none of it comes back.
        public RunReport Die(Inventory carried) => End(RunOutcome.Died, carried);

        private RunReport End(RunOutcome outcome, Inventory carried)
        {
            var report = new RunReport(outcome, CountUnits(carried), Brought);
            if (outcome == RunOutcome.Extracted) TransferAll(carried, Storage);
            Finish(report);
            return report;
        }

        private void Finish(RunReport report)
        {
            IsRunning  = false;
            Brought    = 0;
            LastReport = report;
        }

        public static int CountUnits(Inventory inventory)
        {
            int units = inventory.Items.Count;
            foreach (ItemStack stack in inventory.Stacks) units += stack.Count;
            return units;
        }

        // Moves every stack and gear piece from `from` into `to`.
        public static void TransferAll(Inventory from, Inventory to)
        {
            var stacks = new List<ItemStack>(from.Stacks);
            foreach (ItemStack stack in stacks)
                if (from.Remove(stack.Definition, stack.Count)) to.Add(stack.Definition, stack.Count);

            var items = new List<ItemInstance>(from.Items);
            foreach (ItemInstance item in items)
                if (from.Remove(item)) to.Add(item);
        }

        private static bool Move(Inventory from, Inventory to, ItemDefinition definition, int count)
        {
            if (!from.Remove(definition, count)) return false;
            to.Add(definition, count);
            return true;
        }

        private static bool Move(Inventory from, Inventory to, ItemInstance item)
        {
            if (!from.Remove(item)) return false;
            to.Add(item);
            return true;
        }
    }
}
