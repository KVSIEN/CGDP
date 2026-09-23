using UnityEngine;

namespace CGD.Items
{
    // Quality → per-stat desirability via tradeoff axes.
    // base rises with quality (every stat improves); spread shrinks (tradeoffs soften).
    // Low tiers force sharp tradeoffs, high tiers are strong across the board.
    // Shared across categories — each category only picks which stats oppose each other.
    [CreateAssetMenu(fileName = "StatRollProfile", menuName = "CGD/Items/Stat Roll Profile")]
    public class StatRollProfile : ScriptableObject
    {
        [Header("Quality Curve")]
        [Tooltip("Desirability every stat starts from at quality 1")]
        [Range(0f, 1f)] public float BaseFloor = 0.10f;
        [Tooltip("Desirability added on top by the time quality reaches 100")]
        [Range(0f, 1f)] public float BaseGain = 0.70f;
        [Tooltip("How far a full lean moves a stat at quality 1")]
        [Range(0f, 1f)] public float SpreadCeil = 0.50f;
        [Tooltip("How much of that swing quality 100 removes")]
        [Range(0f, 1f)] public float SpreadDecay = 0.35f;

        [Header("Free Stats")]
        [Tooltip("Random wobble applied to stats that sit on no axis, so they aren't identical every roll")]
        [Range(0f, 0.5f)] public float FreeStatVariance = 0.10f;

        [Header("Tradeoffs")]
        public TradeoffAxis[] Axes;

        [Header("Attachments")]
        [Tooltip("Attachment slots granted per tier, indexed Common -> Legendary")]
        public int[] AttachmentSlotsPerTier = { 1, 1, 2, 3, 4 };

        public ItemRoll Roll(ItemTier tier) => Roll(ItemTiers.RollQuality(tier));

        public ItemRoll Roll(int quality)
        {
            float q      = ItemTiers.Normalize(quality);
            float basis  = BaseFloor + BaseGain * q;
            float spread = Mathf.Max(0f, SpreadCeil - SpreadDecay * q);

            var desirability = new float[ItemStatTraits.Count];
            var onAxis       = new bool[ItemStatTraits.Count];

            if (Axes != null)
            {
                foreach (TradeoffAxis axis in Axes)
                {
                    float lean = Random.Range(-1f, 1f);
                    Assign(axis.Gains, basis + lean * spread, desirability, onAxis);
                    Assign(axis.Costs, basis - lean * spread, desirability, onAxis);
                }
            }

            for (int i = 0; i < desirability.Length; i++)
            {
                if (onAxis[i]) continue;
                desirability[i] = Mathf.Clamp01(basis + Random.Range(-FreeStatVariance, FreeStatVariance));
            }

            return new ItemRoll(quality, desirability);
        }

        public int AttachmentSlots(ItemTier tier)
        {
            int index = (int)tier;
            if (AttachmentSlotsPerTier == null || index >= AttachmentSlotsPerTier.Length) return 0;
            return Mathf.Max(0, AttachmentSlotsPerTier[index]);
        }

        // A stat listed on more than one axis keeps the last assignment rather than
        // compounding, so overlapping axes degrade predictably instead of stacking
        // into an unreachable value.
        private static void Assign(ItemStat[] stats, float value, float[] desirability, bool[] onAxis)
        {
            if (stats == null) return;

            foreach (ItemStat stat in stats)
            {
                if (stat == ItemStat.None) continue;
                desirability[(int)stat] = Mathf.Clamp01(value);
                onAxis[(int)stat]       = true;
            }
        }

        // No stat may appear on two axes (Assign overwrites, so the first axis' lean is lost).
        // Every preset pins FireRate opposite Damage to prevent uncapped TTK.

        // Punch axis splits subtypes: lean negative → fast CQB, lean positive → slow long-range.
        [ContextMenu("Axes/Standard Firearm (SMG · AR · Pistol)")]
        public void ApplyStandardFirearmAxes()
        {
            Axes = new[]
            {
                new TradeoffAxis
                {
                    Name  = "Punch",
                    Gains = new[] { ItemStat.Damage, ItemStat.Range },
                    Costs = new[] { ItemStat.FireRate, ItemStat.Recoil },
                },
                new TradeoffAxis
                {
                    Name  = "Capacity",
                    Gains = new[] { ItemStat.MagazineSize },
                    Costs = new[] { ItemStat.ReloadTime },
                },
                new TradeoffAxis
                {
                    Name  = "Precision",
                    Gains = new[] { ItemStat.Spread },
                    Costs = new[] { ItemStat.DrawTime, ItemStat.Sway },
                },
            };
        }

        // Punch spans semi-auto DMR → bolt-action; Range costs handling weight.
        [ContextMenu("Axes/Precision Rifle (Sniper · DMR · Hand cannon)")]
        public void ApplyPrecisionRifleAxes()
        {
            Axes = new[]
            {
                new TradeoffAxis
                {
                    Name  = "Punch",
                    Gains = new[] { ItemStat.Damage },
                    Costs = new[] { ItemStat.FireRate, ItemStat.ReloadTime, ItemStat.MagazineSize },
                },
                new TradeoffAxis
                {
                    Name  = "Reach",
                    Gains = new[] { ItemStat.Range },
                    Costs = new[] { ItemStat.DrawTime, ItemStat.Sway },
                },
                new TradeoffAxis
                {
                    Name  = "Precision",
                    Gains = new[] { ItemStat.Spread },
                    Costs = new[] { ItemStat.Recoil },
                },
            };
        }

        // Suppression spans high-cyclic hose → slow heavy GPMG; Capacity costs handling.
        [ContextMenu("Axes/Automatic Support (LMG)")]
        public void ApplyAutomaticSupportAxes()
        {
            Axes = new[]
            {
                new TradeoffAxis
                {
                    Name  = "Suppression",
                    Gains = new[] { ItemStat.FireRate },
                    Costs = new[] { ItemStat.Damage, ItemStat.Recoil },
                },
                new TradeoffAxis
                {
                    Name  = "Capacity",
                    Gains = new[] { ItemStat.MagazineSize },
                    Costs = new[] { ItemStat.ReloadTime, ItemStat.DrawTime },
                },
                new TradeoffAxis
                {
                    Name  = "Handling",
                    Gains = new[] { ItemStat.Spread },
                    Costs = new[] { ItemStat.Sway },
                },
            };
        }

        // Cycle spans slow pump → fast auto-shotgun; Tube costs reload; Pattern costs kick.
        [ContextMenu("Axes/Shotgun")]
        public void ApplyShotgunAxes()
        {
            Axes = new[]
            {
                new TradeoffAxis
                {
                    Name  = "Cycle",
                    Gains = new[] { ItemStat.FireRate },
                    Costs = new[] { ItemStat.Damage, ItemStat.DrawTime },
                },
                new TradeoffAxis
                {
                    Name  = "Tube",
                    Gains = new[] { ItemStat.MagazineSize },
                    Costs = new[] { ItemStat.ReloadTime },
                },
                new TradeoffAxis
                {
                    Name  = "Pattern",
                    Gains = new[] { ItemStat.Spread },
                    Costs = new[] { ItemStat.Recoil },
                },
            };
        }
    }
}
