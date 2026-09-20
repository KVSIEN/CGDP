# Weapon Balance

Living design document for CSS Paradise weapon tuning. Every ranged weapon should
be measurable against the targets in this file before it lands in a scene. If a
weapon feels wrong, its measured TTK against the targets here tells you which
knob to turn.

Two independent balance problems. This document owns both:

1. **Inter-category balance** — an AR must not outclass an SMG in the SMG's own
   niche, and vice versa. Handled below via **TTK targets per category at optimal
   range** plus **off-range falloff expectations**.
2. **Intra-category tier balance** — a Legendary M4A1 should feel stronger than
   a Common M4A1 but not so much that low-tier gear is meaningless. Handled by
   the shared `StatRollProfile` quality curve and per-family tradeoff axes.

Neither problem gets solved without the other.

---

## The Target Enemy

Low-tier enemies across all realities hover around **100 total effective HP**
(GDD → Damage Calculation → Enemy Effective Health Baseline). Distribution
varies by reality:

| Reality | Distribution | Example |
|---------|-------------|---------|
| TECH    | Armor > Shields > Health   | 30 HP, 50 Armor, 20 Shield |
| BIO     | Health > Armor > Shields   | 80 HP, 20 Armor, 0 Shield  |
| VOID    | Shields > Health > Armor   | 50 HP, 0 Armor, 50 Shield  |

**All TTK targets below are set against this 100-EHP baseline.** Reality
matchups (Lightning vs shielded VOID, Ice vs armored TECH, BIO's Bleed +
Poison bypasses) create ±20–40% swings on top of the base numbers — those
come from the affinity table, not from per-weapon tuning.

Higher-tier enemies scale up from 100 EHP; the targets below still hold as
*relative* per-category shape.

---

## Category Targets @ Optimal Range

Each category is best in exactly one dimension of engagement and average or
worse in every other. This is the primary balance constraint.

| Category    | Optimal range | Shots-to-kill | TTK          | Best-in-class niche               |
|-------------|---------------|---------------|--------------|-----------------------------------|
| Sniper      | 80–200 m      | 1 shot        | 0.6 – 1.2 s  | TTFK from ambush, range           |
| DMR         | 40–120 m      | 2 – 3         | ~0.4 s       | Ranged accuracy at RPM            |
| AR          | 30–60 m       | 4 – 5         | ~0.35 s      | All-rounder                       |
| LMG         | 30–70 m       | 3 – 4         | ~0.25 s      | Sustained TTK, multi-target       |
| SMG         | 5–20 m        | 5 – 6         | ~0.35 s      | Mobile close-range TTK            |
| Shotgun     | 3–8 m         | 1 shot        | 0.15 – 0.25 s| Instant close-range kill          |
| Pistol      | 15–25 m       | 3 – 4         | ~0.5 s       | Always-ready backup               |
| Hand cannon | 15–30 m       | 2             | ~0.4 s       | Punchy secondary (HeavyRounds)    |

- **TTFK** = Time To First Kill (includes any windup, draw or first-shot delay)
- **TTK** = Time To Kill (click-to-kill in sustained fire, ignores draw)

Shots-to-kill are against 100 EHP with a headshot multiplier of ×1 (body
shots). Headshot kills should be 1–2 shots faster on non-shotguns.

---

## Off-Range TTK

Every category must fall off dramatically outside its optimum. Rough multipliers
on the optimum TTK from the table above:

| Weapon → Range | 3–8 m | 15–25 m | 40–60 m | 80–120 m | 150 m+ |
|----------------|-------|---------|---------|----------|--------|
| Shotgun        | 1×    | 3×      | 8×      | —        | —      |
| SMG            | 1×    | 1.3×    | 3–4×    | —        | —      |
| Pistol         | 1.2×  | 1×      | 2×      | —        | —      |
| Hand cannon    | 1.2×  | 1×      | 1.5×    | 3×       | —      |
| AR             | 1.3×  | 1×      | 1×      | 2×       | 4×     |
| LMG            | 1.5×  | 1.2×    | 1×      | 1.5×     | 3×     |
| DMR            | 1.5×  | 1.2×    | 1×      | 1×       | 1.3×   |
| Sniper         | 2×    | 1.5×    | 1.2×    | 1×       | 1×     |

"—" means the weapon cannot meaningfully engage there — pellets scatter to
nothing, bullets do too little damage to matter within one magazine.

**The rule to enforce:** at each other category's optimum range, your
weapon's TTK should be *significantly worse* than that category's TTK there.
A sniper at 10m still works (quickscope), but an AR at 150m should struggle
enough that a sniper is clearly the better tool.

The `RangeOptimal → RangeFalloffEnd → DamageFalloffMin` curve is the primary
lever here. If falloff feels too gentle, lower `DamageFalloffMin` before
lowering base damage.

---

## Category Niches

### Sniper
- **Best at**: single-target elimination from >80m; Time To First Kill from ambush.
- **Weakness**: sustained fire, close quarters, slow bolt cycle, small magazine, HeavyRounds are scarce (shared with hand cannons).
- **What a Legendary sniper feels like**: same one-shot potential, better bolt cycle (lower RPM tax), tighter first-shot scope, faster reload.

### DMR
- **Best at**: precise ranged fire with follow-up shots; picks between Sniper and AR range bands.
- **Weakness**: outclassed at extreme range by sniper, at close quarters by AR/SMG. StandardRounds shared with AR/LMG.
- **What a Legendary DMR feels like**: near-sniper accuracy at half the cycle time.

### AR
- **Best at**: no other class beats it at 30–60m; the reliable choice.
- **Weakness**: outclassed at every other range's optimum by that range's specialist.
- **What a Legendary AR feels like**: cleaner recoil pattern, faster reload, no accuracy tax on sustained fire.

### LMG
- **Best at**: sustained fire against multiple enemies; holding a chokepoint.
- **Weakness**: slow draw, slow reload, poor handling, wide first-shot spread, StandardRounds shared with AR/DMR (competition).
- **What a Legendary LMG feels like**: tighter first-mag accuracy, less recoil heat buildup, faster ADS.

### SMG
- **Best at**: fast TTK inside 20m while moving.
- **Weakness**: falls apart at 30m+, burns LightRounds fast (shared with pistols).
- **What a Legendary SMG feels like**: tighter hip cone, larger mag, better range roll.

### Shotgun
- **Best at**: one-shot kill at 3–8m; punishes anyone who enters.
- **Weakness**: pellet spread makes 15m+ shots miss most damage; ShotgunShells is its own scarce pool.
- **What a Legendary shotgun feels like**: tighter pellet pattern, faster follow-up, higher per-pellet damage roll.

### Pistol
- **Best at**: always-ready backup; LightRounds abundance means it never runs dry.
- **Weakness**: low damage per shot, small mag, sustained fire loses to any primary.
- **What a Legendary pistol feels like**: faster ADS, larger mag, tighter first-shot accuracy.

### Hand cannon (Deagle-class pistol, magnum revolver)
- **Best at**: high per-shot damage in the pistol chassis; two-shot kills at close range.
- **Weakness**: HeavyRounds share pool with snipers — carrying both = severe ammo pressure; slow cycle, small mag.
- **What a Legendary hand cannon feels like**: less recoil per shot, faster follow-up, tighter first-shot spread.

---

## Deriving Damage and RPM from a TTK Target

For a canonical 100 EHP enemy and a target of *N* shots to kill:

```
Damage = ceil(100 / N)
RPM    = 60 × (N − 1) / TTK
```

The `(N − 1)` form accounts for the fact that the first shot fires at t=0 and
each subsequent shot is (60/RPM) seconds later, so `TTK = (N − 1) × 60/RPM`.

**Example — AR at 4 shots, 0.35 s TTK:**
```
Damage = 100 / 4                 = 25
RPM    = 60 × 3 / 0.35           = 514
```

Round to something realistic. Then set the `Damage` and `RPM` **FloatRanges**
on `WeaponCategoryData` to span roughly ±20% of that midpoint. That gives the
roll system room to produce meaningful variance without any single roll
missing the TTK target by more than a shot.

**Shotguns:** account for pellets.
```
Damage_per_pellet = 100 / (N × pellet_count)
```

**Bursts:** the burst window is what matters for TTFK. A 3-round burst that
kills in one trigger pull has TTFK = 2 × BurstInterval + shot latency.

---

## Tier Feel Goals

The `StatRollProfile` quality curve encodes the GDD's item quality table.
When tuning, roll 20 weapons of a category at each tier and feel-check:

| Tier         | Quality | Feel goal |
|--------------|---------|-----------|
| Common       | 1–20    | Every shot fired feels like a compromise. Damage rolls come with hard recoil and low mag; snappy weapons feel underpowered. Stepping stone. |
| Uncommon     | 21–40   | Tradeoffs still visible but softer. A common and an uncommon of the same category should feel meaningfully different in a single burst. |
| Rare         | 41–60   | One stat is unambiguously strong; the rest are serviceable. Kept unless something better rolls. |
| Epic         | 61–80   | Multiple strong stats. Tradeoffs are minor; most rolls are keepers. |
| Legendary    | 81–100  | High budget everywhere. Player builds around these. Difference between rare and legendary is closer to "polished vs raw" than "strong vs weak". |

Two knobs on `StatRollProfile` control this:

- `BaseFloor + BaseGain × quality` — the power floor every stat starts at.
- `SpreadCeil − SpreadDecay × quality` — how hard tradeoff axes bite.

**Diagnostic:** if a Common feels close to a Legendary, spread is too small
or floor is too high. If Legendaries feel identical to each other, spread is
too small (nothing to distinguish rolls) or free-stat variance is too low.

---

## Per-Category Roll Profiles

`GearDefinition` (parent of `WeaponCategoryData`) already carries an optional
`_rollProfile` field. Assigning a category-specific profile lets each family
use different tradeoff axes without a code change.

`StatRollProfile` has ContextMenu presets for each weapon family:

- **Standard Firearm** (SMG, AR, Pistol): Punch (Damage ↔ FireRate + Recoil),
  Capacity (Mag ↔ ReloadTime), Precision (Spread ↔ DrawTime + Sway).
- **Precision Rifle** (Sniper, DMR, Hand cannon): Punch (Damage ↔ ReloadTime +
  Mag), Reach (Range ↔ DrawTime + Sway), Precision (Spread ↔ FireRate).
- **Automatic Support** (LMG): Capacity (Mag ↔ ReloadTime + DrawTime),
  Suppression (FireRate ↔ Recoil + Sway), Weight (Damage ↔ Spread).
- **Shotgun**: Slug (Damage ↔ Spread), Tube (Mag ↔ ReloadTime), Recovery
  (FireRate ↔ Recoil + DrawTime).

**Recommended asset layout:** one `StatRollProfile` per family in
`Assets/Project/Data/Items/Profiles/` (e.g. `StandardFirearmProfile.asset`,
`PrecisionRifleProfile.asset`, `AutomaticSupportProfile.asset`,
`ShotgunProfile.asset`). Each `WeaponCategoryData` asset assigns the matching
one to its `_rollProfile` field. All four profiles share the same quality
curve knobs (`BaseFloor`/`BaseGain`/`SpreadCeil`/`SpreadDecay`) so the
game-wide power curve stays uniform — only the axes differ.

Without a profile, `WeaponCategoryData.Roll()` returns `ItemRoll.Unrolled()`
and stats roll uniformly (no tier variance). That's fine for early
prototyping but should be filled in before shipping.

---

## Balancing Workflow

For each new or modified weapon:

1. **Identify the category's TTK target** from the table above.
2. **Roll the weapon a few times** and measure TTK vs a 100-HP target in Play
   mode (target dummy in the sandbox scene).
3. **Compare against target.** If TTK is off by more than ~30%:
   - Too fast → lower `Damage` or `RPM` range on the category
   - Too slow → raise them
4. **Check the falloff.** Fire from the next-category-up's optimum range.
   If TTK is competitive with that category, `DamageFalloffMin` is too
   generous.
5. **Check tier variance.** Roll one weapon at Common and one at Legendary.
   If they play similarly, adjust `StatRollProfile.SpreadCeil` up or
   `SpreadDecay` down.
6. **Repeat after any StatRollProfile change** — a profile tweak affects
   every category that references it.

---

## Open Questions

- Higher-tier enemy EHP scaling (from GDD: "specific values per tier are TBD"). Once locked, add a scaling row for each tier here.
- Shotguns, energy weapons and melee not yet baselined in the GDD's Weapon Damage Targets table. Shotguns are proposed above; energy weapons and melee still need their own rows once designed.
- Reality-matchup swings (Lightning vs shields, Ice vs armor, BIO Bleed vs everything) haven't been quantified — currently a hand-wave "±20–40%". A follow-up doc could formalize per-affinity multipliers.
