using UnityEngine;

namespace CGD.Items
{
    // Turns a quality score into a set of per-stat outcomes.
    //
    // Quality answers "how much power does this item have"; the tradeoff axes answer
    // "how is that power distributed". Keeping them separate is what lets the GDD's
    // two requirements coexist — low tiers forced into clear tradeoffs, top tiers
    // strong across the board — which a plain range shift can't express.
    //
    //   base   = BaseFloor  + BaseGain    * quality   (lifts every stat)
    //   spread = SpreadCeil - SpreadDecay * quality   (how hard tradeoffs bite)
    //
    //   desirability = base +/- lean * spread
    //
    // At quality 10 (base .17, spread .47) a full lean buys a strong stat by pinning
    // its opposite to the floor. At quality 95 (base .77, spread .17) the same lean
    // still shapes the weapon's character but the cost side stays perfectly usable.
    //
    // Shared across categories on purpose: one profile defines the game's power
    // curve, and a category only chooses which stats oppose each other.
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

        // ── Family presets (see WEAPON_BALANCE.md for the design rationale) ──
        //
        // Every preset below obeys one rule: no stat appears on two axes. Assign()
        // overwrites rather than compounds, so a stat on two axes silently loses
        // the first axis' lean and that axis stops being a tradeoff at all.
        //
        // Each preset also pins FireRate opposite Damage, directly or through a
        // shared axis. That pairing is what keeps a widened category band safe —
        // without it a roll can take the top of the RPM band and the top of the
        // damage band together and land at a fraction of the category's TTK target.

        // Punch carries Range as well as Damage, which is what splits a category
        // into subtypes: lean negative and you get a fast, tight, short-ranged CQB
        // weapon (carbine, Glock, PDW); lean positive and you get a slow, kicking,
        // long-ranged one (battle rifle, hand cannon, heavy SMG). The category's
        // authored band sets how far apart those two poles sit.
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

        // Snipers / DMRs / hand cannons — damage costs cycle rate, reload and mag
        // capacity, because bigger rounds chamber slower and pack fewer per
        // magazine. That single axis is what spans the category: lean negative for
        // a semi-auto DMR (fast cycle, modest damage, deep magazine), lean positive
        // for a bolt-action anti-materiel rifle (one huge round at a time).
        // Range costs handling weight: a long barrel is unwieldy in hand.
        //
        // FireRate moved onto Punch from the old Precision axis. It was previously
        // on Precision alone, which left it free to roll high alongside high damage.
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

        // LMGs — cyclic rate costs per-round punch and recoil control, which spans
        // the category from a high-cyclic MG42-class hose (fast, light rounds,
        // wild) to a heavy GPMG (slow, hard-hitting, controllable). Mag capacity
        // costs handling, because a 200-round belt is dead weight to swap and swing.
        //
        // Damage moved onto Suppression from its own "Weight" axis. On a separate
        // axis it could roll to the top of the band at the same time as FireRate,
        // which at the widened 500–1200 RPM band produced a ~0.1s TTK.
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

        // Shotguns — cycle speed costs per-pellet punch, spanning the category from
        // a slow pump firing heavy buck (one-shot at contact range) to a fast
        // auto-shotgun trading per-shell damage for follow-up. Tube capacity costs
        // shell-by-shell reload; a tighter pattern costs kick.
        //
        // Damage moved onto Cycle from the old "Slug" axis for the same reason as
        // the LMG: uncoupled, a roll could take 18 dmg × 12 pellets at 260 RPM and
        // one-shot on full auto.
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
