using System;
using System.Collections.Generic;
using System.Globalization;
using CGD.Core;

namespace CGD.Items
{
    // A saved piece of gear. Only what rebuilds it is stored — the definition, tier, seed and
    // rerolled layers give back the exact same roll (stats, name, perks) — plus the attachments
    // fitted since. Values are text so a changed enum order or a 64-bit seed can't corrupt them.
    [Serializable]
    public class GearRecord
    {
        public string       item;
        public string       tier;
        public string       seed;
        public string       variants;
        public List<string> attachments = new();

        // Null for gear that can't be rebuilt from a roll: hand-placed scene weapons, or a
        // definition missing from the catalog.
        public static GearRecord Capture(ItemInstance gear, ItemCatalog catalog)
        {
            if (gear.Definition is not GearDefinition) return null;

            string id = catalog.IdOf(gear.Definition);
            if (id == null) return null;

            var record = new GearRecord
            {
                item     = id,
                tier     = gear.Tier.ToString(),
                seed     = gear.Seed.Value.ToString(CultureInfo.InvariantCulture),
                variants = gear.Variants.ToString(),
            };
            foreach (AttachmentDefinition attachment in gear.Attachments)
            {
                string attachmentId = catalog.IdOf(attachment);
                if (attachmentId != null) record.attachments.Add(attachmentId);
            }
            return record;
        }

        // False when the item no longer exists or the record is damaged. Attachments that no
        // longer exist or no longer fit are dropped; the gear itself still comes back.
        public bool TryRebuild(ItemCatalog catalog, out ItemInstance gear)
        {
            gear = null;
            if (!catalog.TryGet(item, out ItemDefinition definition) || definition is not GearDefinition gearDefinition) return false;
            if (!Enum.TryParse(tier, out ItemTier itemTier)) return false;
            if (!ulong.TryParse(seed, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong seedValue)) return false;
            if (!SeedVariants.TryParse(variants, out SeedVariants layerVariants)) return false;

            gear = gearDefinition.CreateInstance(gearDefinition.Roll(itemTier, new Seed(seedValue), layerVariants));
            foreach (string attachmentId in attachments)
                if (catalog.TryGet(attachmentId, out ItemDefinition attachment) && attachment is AttachmentDefinition fitted)
                    gear.TryAttach(fitted);
            return true;
        }
    }
}
