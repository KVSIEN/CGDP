# Weapon Balance

Living design document for CSS Paradise weapon tuning. Every ranged weapon should
be measurable against the targets in this file before it lands in a scene. If a
weapon feels wrong, its measured TTK against the targets here tells you which
knob to turn.

Two independent balance problems. This document owns both:

1. **Inter-category balance** — an AR must not outclass an SMG in the SMG's own
   niche, and vice versa. Handled below via **TTK targets per category at optimal
   range** plus **off-range falloff expectations**.
2. **Intra-category tier balance** — a Legendary AK-47 should feel stronger than
   a Common AK-47 but not so much that low-tier gear is meaningless. Handled by
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
- **Best at**: precise ranged fire with follow-up shots; picks between Sniper and AR range bands. Hard-hitting variants (low RoF, heavy recoil, damage rolled to the top of the range) drop into two-shot territory to body — one-shot to head — trading cadence for burst damage.
- **Weakness**: outclassed at extreme range by sniper, at close quarters by AR/SMG. StandardRounds shared with AR/LMG.
- **What a Legendary DMR feels like**: near-sniper accuracy at half the cycle time.

### AR
- **Models** (`Models` on the category, all equal weight, so each series is 20%):
  - **M-Series** (AR platform; precise, low recoil): M4 (auto carbine; 20% "Burst M4"), M16 (burst marksman; 20% semi "AR-15"), M14 (battle rifle, semi; 20% "Auto M14", 20 rds), M27 (automatic rifle: lowest recoil, slow reload)
  - **AK-Series** (hard-hitting, heavy kick): AK-47 (heaviest climb and drift), AK-74u (short carbine), AKM (tamer AK-47), AK-12 (modern, accurate; 30% "Burst AK-12")
  - **B-Series** (bullpups: rifle range, compact handling): FAMAS (1000 RPM burst; 20% "Auto FAMAS", 25 rds), AUG (cleanest recoil, tightest optic), TAR-21 (close quarters, 900 RPM), F2000 (low recoil, mid range)
  - **H-Series** (battle rifles, 20 rds): FAL (70% semi, 15% burst, 15% auto), SCAR-H, G3 (heaviest auto), HK416 (the series' 5.56 all-rounder, 30 rds)
  - **C-Series** (compact: fast handling, short range): G36C (fastest fire rate), Galil (35 rds), AM-17 (fastest draw and ADS, shortest range), QBZ-95
  - Each model also has `Leans` (−1…1 per stat), added to the range's bias: its window stays the same but the odds shift toward its style, so a model's signature stats land on its style's side of its T1 value about two thirds of the time instead of half (M14 damage, G36C fire rate, M27 recoil). A lean is 1.5 × (scale − 1), capped at ±0.6; stats within 5% of the midpoint stay neutral. A firing variant that rescales its model (AR-15, Auto FAMAS, M712, the Glock 18's semi Glock 17, every "Burst" variant) has its own `Leans` (`OwnLeans`), worked out the same way from its combined scale, so it leans toward its own style instead of the model's main one — the semi Glock 17 no longer inherits the Glock 18's fast, soft-kicking lean. Variants that only change reload or pellets (speedloaders, the Judge) keep the model's.
  - Each model's scales are its T1 row divided by the category midpoints (26 dmg, 675 RPM, 60 m, 30 rds, 1.45 recoil, 3.15° hip spread), so generated rifles of a name centre on their T1 numbers. Burst and semi models (M16, FAMAS, M14, FAL) beat the 0.35 s target on paper; landing every round of two bursts, or tapping at the cap, is their price.
- **Best at**: no other class beats it at 30–60m; the reliable choice.
- **Weakness**: outclassed at every other range's optimum by that range's specialist.
- **What a Legendary AR feels like**: cleaner recoil pattern, faster reload, no accuracy tax on sustained fire.

### LMG
- **Best at**: sustained fire against multiple enemies; holding a chokepoint.
- **Weakness**: slow draw, slow reload, poor handling, wide first-shot spread, StandardRounds shared with AR/DMR (competition).
- **What a Legendary LMG feels like**: tighter first-mag accuracy, less recoil heat buildup, faster ADS.

### SMG
- **Models** (`Models` on the category, all equal weight, so each series is 20%):
  - **MP-Series** (German MPs; controllable, precise): MP5 (20% "Burst MP5"), MP5K (compact, short range), MP7 (40 rds, lowest recoil of the series), MPL (slow 650 RPM, harder hits)
  - **V-Series** (vintage; slow cyclic, heavy hits, big magazines, heavier handling): Thompson (50-round drum, heaviest kick), MP40 (slow and steady), Uzi (short range, jumpy), PM12 (40 rds)
  - **X-Series** (machine pistols; extreme fire rate, close range): MAC-10 (1150 RPM, 12 m, wildest recoil), Scorpion EVO (1150 RPM; 20% "Burst"), MP9, PP2000 (fastest draw and ADS, 20 rds)
  - **T-Series** (modern tactical; versatile, longest reach): UMP-45 (650 RPM, hard hits, 32 m; 20% "Burst"), MPX, APC9 (1000 RPM), PMX
  - **P-Series** (unconventional designs): P90 (50 rds, slow top-loaded reload), Vector (1200 RPM, near-recoilless, 25 rds, 18 m; 20% "Burst"), QCW-05 (50 rds, suppressed: T1 is heard at 12 m), PP-19 Bizon (64-round helical magazine)
  - Scales and leans are built like the AR's, against the SMG midpoints (20.5 dmg, 900 RPM, 26 m, 27 rds — the median of its low-weighted magazine window — 0.925 recoil, 2.25° hip spread). Most models land at 0.30–0.40 s; the MAC-10 (0.26 s) and Vector (0.25 s) pay with range, magazine and, for the MAC-10, recoil.
- **Best at**: fast TTK inside 20m while moving.
- **Weakness**: falls apart at 30m+, burns LightRounds fast (shared with pistols).
- **What a Legendary SMG feels like**: tighter hip cone, larger mag, better range roll.

### Shotgun
- **Best at**: one-shot kill at 3–8m; punishes anyone who enters.
- **Weakness**: pellet spread makes 15m+ shots miss most damage; ShotgunShells is its own scarce pool.
- **What a Legendary shotgun feels like**: tighter pellet pattern, faster follow-up, higher per-pellet damage roll.
- **Reload**: `ReloadStyles` 4 : 1 — pump/tube models one shell per step, the AA-12 magazine-fed; each step takes ReloadTime ÷ steps for a full load (about 0.45–0.6 s), so the Tube axis trades capacity for a slower full reload but not slower shells. Loaded shells survive any interrupt, which enables blast → one shell → bash-cancel → blast. Sniper rifles are the reverse, 4 : 1 magazine to one-round-per-step (only older bolt-actions: Kar98k, Mosin-Nagant). Revolvers (pistol H-Series) weight one round per step 85 : 15 against a speedloader full load.

### Pistol
- **Models** (`Models` on the category, all equal weight, so each series is 20%):
  - **S-Series** (service pistols; reliable all-rounders): Makarov (8 rds, quickest draw and reload), M9, USP (hardest hits, 12 rds, steadiest), MP-443 (18 rds, fastest of the four)
  - **C-Series** (classics; older designs, small magazines): M1911 (34 dmg, 3-shot kill, 7 rds), Luger P08 (low recoil, accurate), Walther PP (weakest and quickest to draw), Mauser C96 (30 m reach, reloads from 10-round stripper clips; 20% are the full-auto "M712")
  - **A-Series** (automatics): Glock 18 (1100 RPM; 20% are the semi-auto "Glock 17" — 25 × 400, 17 rds), TEC-9 (24 rds, loosest), Beretta 93R (3-round burst at 0.055 s — the category's burst count is 3–3, interval 0.05–0.07 s), CZ 75 Auto (fastest TTK of the series, 16 rds)
  - **H-Series** (high power; magnums and revolvers): Desert Eagle (55 dmg, 2-shot kill), .44 Magnum (60 dmg, slowest cycle), Taurus Judge (a 6-pellet shotgun revolver on ShotgunShells; 2-shot kill inside 8 m), Chiappa Rhino (.357; the fast, low-recoil revolver — 42 × 320, 3-shot kill)
  - **T-Series** (tactical; modern high-end): Ruger Mark IV (.22 — near-recoilless, tightest spread, quietest), Five-seveN (20 rds, 30% armor penetration on the T1), Tac-45 (34 dmg, 3-shot kill, 10 rds), P250 (fastest draw)
  - Models set their own ammo: the H-Series fires HeavyRounds (the Judge ShotgunShells) while the category stays LightRounds. Revolvers load one round per step; 15% of them roll with a speedloader that loads the whole cylinder in one step.
  - Scales and leans are built like the AR's, against the pistol midpoints (40 dmg, 450 RPM, 23.5 m, 9.67 rds, 2.25 recoil, 2.65° hip spread). Semi-auto service pistols land at 0.43–0.50 s, automatics at 0.24–0.32 s, and the hand cannons at 0.20–0.38 s, paid for with ammo pool, magazine and recoil.
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

- **Standard Firearm** (SMG, AR, Pistol): Punch (Damage + **Range** ↔ FireRate +
  Recoil), Capacity (Mag ↔ ReloadTime), Precision (Spread ↔ DrawTime + Sway).
- **Precision Rifle** (Sniper, DMR, Hand cannon): Punch (Damage ↔ **FireRate** +
  ReloadTime + Mag), Reach (Range ↔ DrawTime + Sway), Precision (Spread ↔ Recoil).
- **Automatic Support** (LMG): Suppression (FireRate ↔ **Damage** + Recoil),
  Capacity (Mag ↔ ReloadTime + DrawTime), Handling (Spread ↔ Sway).
- **Shotgun**: Cycle (FireRate ↔ **Damage** + DrawTime), Tube (Mag ↔ ReloadTime),
  Pattern (Spread ↔ Recoil).

Two invariants hold across every preset. Breaking either one silently unbalances
the category, so check them after any axis edit:

1. **No stat appears on two axes.** `StatRollProfile.Assign` overwrites rather
   than compounds, so the later axis wins and the earlier one stops being a
   tradeoff at all.
2. **Damage and FireRate always sit on opposite sides of one axis.** This is what
   makes the wide subtype bands below safe. Left uncoupled, a roll takes the top
   of the RPM band and the top of the damage band together and lands at a
   fraction of the category's TTK target.

### Subtype bands

Each category asset's ranges (`Data/Weapons/Categories`) span a *band between two
subtypes*, not a window around one reference weapon. The Punch/Suppression/Cycle
axis decides which end a weapon lands on, so the band reads as a spectrum:

| Category | Fast pole (lean −) | Slow pole (lean +) | T1 anchor |
|---|---|---|---|
| AR      | CQB carbine — 900 RPM, 18 dmg, 30 m  | Battle rifle — 450 RPM, 34 dmg, 90 m   | 20 models in five series (see AR); e.g. `AUG_T1` 680×23, `G36C_T1` 850×19, `FAL_T1` 450×34 |
| SMG     | PDW / machine pistol — 1200 RPM, 13 dmg, 12 m | Heavy SMG — 600 RPM, 28 dmg, 40 m | 20 models in five series (see SMG); e.g. `MP5_T1` 850×18, `MAC10_T1` 1150×19, `Thompson_T1` 650×24 |
| Pistol  | Glock — 600 RPM, 20 dmg, 12 m        | Hand cannon — 200 RPM, 60 dmg, 35 m    | 20 models in five series (see Pistol); e.g. `Makarov_T1` 380×27, `Glock18_T1` 1100×18, `DesertEagle_T1` 300×55 |
| Sniper  | Semi-auto DMR — 260 RPM, 45 dmg, 50 m | Bolt / AMR — 35 RPM, 170 dmg, 220 m   | `Kar98k_T1` 45×95, 120 m |
| LMG     | High-cyclic MG — 1200 RPM, 24 dmg    | Heavy GPMG — 500 RPM, 45 dmg           | `M249_T1` 700×30, 55 m |
| Shotgun | Auto — 260 RPM, 9 dmg × 6 pellets    | Pump — 60 RPM, 18 dmg × 8 pellets      | `M870_T1` 60×12×8, 10 m |

The Sniper band is where the **DMR row** in the category table above comes from.
There is no separate DMR `WeaponType` — marksman rifles are the fast end of
Sniper, reached by a negative Punch lean.

Shotgun `PelletCount` stays ≥ 6 deliberately. It rolls uniformly rather than
through an axis, so a 1-pellet "slug" roll could not have its damage
compensated. Slugs need their own category or a PelletCount↔Damage axis first.

Pole TTKs measured with the doc's `(N−1) × 60/RPM` formula against 100 EHP:

| Category | Fast pole | Slow pole | Target |
|---|---|---|---|
| AR      | 6 STK, 0.33 s | 3 STK, 0.27 s | ~0.35 s |
| SMG     | 8 STK, 0.35 s | 4 STK, 0.30 s | ~0.35 s |
| Pistol  | 5 STK, 0.40 s | 2 STK, 0.30 s | ~0.50 s / hand cannon ~0.40 s |
| Sniper  | 3 STK, 0.46 s | 1 STK        | ~0.40 s DMR / 0.6–1.2 s bolt |
| LMG     | 5 STK, 0.20 s | 3 STK, 0.24 s | ~0.25 s |
| Shotgun | 2 STK, 0.23 s | 1 STK        | 0.15–0.25 s |

The pistol slow pole is the one outlier: 0.30 s against the hand-cannon row's
~0.40 s target. `DesertEagle_T1` is already hotter still (300 RPM × 55 dmg =
2 STK, 0.20 s), so the band is not introducing the problem — but the hand-cannon
cycle rate wants a playtest pass.

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

## Tier 1 Reference Weapons

Hand-authored Common-tier (Tier 1) assets live in
`Assets/Project/Data/Weapons/Ranged/T1/` — one per model for the AR, SMG and
pistol rosters, one per remaining category. They are the concrete example of what "Common feels like" and the
first thing to compare a new weapon against.

| Asset             | Category    | Damage × RPM        | Ammo           | STK / TTK    | Notes                                        |
|-------------------|-------------|---------------------|----------------|--------------|----------------------------------------------|
| `M4_T1`           | AR · M-Series  | 22 × 800            | StandardRounds | 5 / 0.30s    | 45 m, 30 rds, draw 0.55s |
| `M16_T1`          | AR · M-Series  | 25 × 800 (Burst)    | StandardRounds | 4 / 0.23s    | 65 m, 30 rds, draw 0.65s |
| `M14_T1`          | AR · M-Series  | 34 × 400 (Semi)     | StandardRounds | 3 / 0.30s    | 85 m, 20 rds, draw 0.8s |
| `M27_T1`          | AR · M-Series  | 23 × 700            | StandardRounds | 5 / 0.34s    | 60 m, 30 rds, draw 0.7s |
| `AK47_T1`         | AR · AK-Series | 28 × 600            | StandardRounds | 4 / 0.30s    | 45 m, 30 rds, draw 0.7s |
| `AK74u_T1`        | AR · AK-Series | 21 × 700            | StandardRounds | 5 / 0.34s    | 28 m, 30 rds, draw 0.5s |
| `AKM_T1`          | AR · AK-Series | 27 × 600            | StandardRounds | 4 / 0.30s    | 50 m, 30 rds, draw 0.7s |
| `AK12_T1`         | AR · AK-Series | 24 × 700            | StandardRounds | 5 / 0.34s    | 55 m, 30 rds, draw 0.65s |
| `FAMAS_T1`        | AR · B-Series  | 20 × 1000 (Burst)   | StandardRounds | 5 / 0.24s    | 45 m, 25 rds, draw 0.55s |
| `AUG_T1`          | AR · B-Series  | 23 × 680            | StandardRounds | 5 / 0.35s    | 55 m, 30 rds, draw 0.6s |
| `TAR21_T1`        | AR · B-Series  | 20 × 900            | StandardRounds | 5 / 0.27s    | 35 m, 30 rds, draw 0.55s |
| `F2000_T1`        | AR · B-Series  | 22 × 800            | StandardRounds | 5 / 0.30s    | 50 m, 30 rds, draw 0.6s |
| `FAL_T1`          | AR · H-Series  | 34 × 450 (Semi)     | StandardRounds | 3 / 0.27s    | 90 m, 20 rds, draw 0.8s |
| `SCARH_T1`        | AR · H-Series  | 31 × 600            | StandardRounds | 4 / 0.30s    | 75 m, 20 rds, draw 0.7s |
| `G3_T1`           | AR · H-Series  | 33 × 550            | StandardRounds | 4 / 0.33s    | 80 m, 20 rds, draw 0.8s |
| `HK416_T1`        | AR · H-Series  | 24 × 750            | StandardRounds | 5 / 0.32s    | 60 m, 30 rds, draw 0.6s |
| `G36C_T1`         | AR · C-Series  | 19 × 850            | StandardRounds | 6 / 0.35s    | 30 m, 30 rds, draw 0.5s |
| `Galil_T1`        | AR · C-Series  | 24 × 700            | StandardRounds | 5 / 0.34s    | 40 m, 35 rds, draw 0.6s |
| `AM17_T1`         | AR · C-Series  | 22 × 750            | StandardRounds | 5 / 0.32s    | 25 m, 30 rds, draw 0.45s |
| `QBZ95_T1`        | AR · C-Series  | 21 × 800            | StandardRounds | 5 / 0.30s    | 35 m, 30 rds, draw 0.55s |
| `MP5_T1`          | SMG · MP-Series | 18 × 850            | LightRounds    | 6 / 0.35s    | 25 m, 30 rds, draw 0.45s |
| `MP5K_T1`         | SMG · MP-Series | 17 × 900            | LightRounds    | 6 / 0.33s    | 18 m, 30 rds, draw 0.38s |
| `MP7_T1`          | SMG · MP-Series | 17 × 950            | LightRounds    | 6 / 0.32s    | 28 m, 40 rds, draw 0.42s |
| `MPL_T1`          | SMG · MP-Series | 22 × 650            | LightRounds    | 5 / 0.37s    | 25 m, 32 rds, draw 0.45s |
| `Thompson_T1`     | SMG · V-Series  | 24 × 650            | LightRounds    | 5 / 0.37s    | 30 m, 50 rds, draw 0.58s |
| `MP40_T1`         | SMG · V-Series  | 24 × 600            | LightRounds    | 5 / 0.40s    | 28 m, 32 rds, draw 0.5s |
| `Uzi_T1`          | SMG · V-Series  | 22 × 650            | LightRounds    | 5 / 0.37s    | 20 m, 32 rds, draw 0.42s |
| `PM12_T1`         | SMG · V-Series  | 23 × 600            | LightRounds    | 5 / 0.40s    | 24 m, 40 rds, draw 0.48s |
| `MAC10_T1`        | SMG · X-Series  | 19 × 1150           | LightRounds    | 6 / 0.26s    | 12 m, 30 rds, draw 0.35s |
| `ScorpionEVO_T1`  | SMG · X-Series  | 15 × 1150           | LightRounds    | 7 / 0.31s    | 15 m, 30 rds, draw 0.38s |
| `MP9_T1`          | SMG · X-Series  | 15 × 1000           | LightRounds    | 7 / 0.36s    | 16 m, 30 rds, draw 0.36s |
| `PP2000_T1`       | SMG · X-Series  | 17 × 900            | LightRounds    | 6 / 0.33s    | 14 m, 20 rds, draw 0.35s |
| `UMP45_T1`        | SMG · T-Series  | 24 × 650            | LightRounds    | 5 / 0.37s    | 32 m, 25 rds, draw 0.48s |
| `MPX_T1`          | SMG · T-Series  | 19 × 850            | LightRounds    | 6 / 0.35s    | 30 m, 30 rds, draw 0.45s |
| `APC9_T1`         | SMG · T-Series  | 18 × 1000           | LightRounds    | 6 / 0.30s    | 28 m, 30 rds, draw 0.45s |
| `PMX_T1`          | SMG · T-Series  | 19 × 900            | LightRounds    | 6 / 0.33s    | 26 m, 30 rds, draw 0.45s |
| `P90_T1`          | SMG · P-Series  | 17 × 900            | LightRounds    | 6 / 0.33s    | 26 m, 50 rds, draw 0.48s |
| `Vector_T1`       | SMG · P-Series  | 18 × 1200           | LightRounds    | 6 / 0.25s    | 18 m, 25 rds, draw 0.42s |
| `QCW05_T1`        | SMG · P-Series  | 17 × 900            | LightRounds    | 6 / 0.33s    | 24 m, 50 rds, draw 0.45s |
| `Bizon_T1`        | SMG · P-Series  | 20 × 700            | LightRounds    | 5 / 0.34s    | 22 m, 64 rds, draw 0.5s |
| `Makarov_T1`      | Pistol · S-Series | 27 × 380 (Semi)     | LightRounds    | 4 / 0.47s    | 16 m, 8 rds, draw 0.3s, fastest reload of the series |
| `M9_T1`           | Pistol · S-Series | 26 × 380 (Semi)     | LightRounds    | 4 / 0.47s    | 20 m, 15 rds, draw 0.38s |
| `USP_T1`          | Pistol · S-Series | 28 × 360 (Semi)     | LightRounds    | 4 / 0.50s    | 22 m, 12 rds, draw 0.4s |
| `MP443_T1`        | Pistol · S-Series | 25 × 420 (Semi)     | LightRounds    | 4 / 0.43s    | 17 m, 18 rds, draw 0.36s |
| `M1911_T1`        | Pistol · C-Series | 34 × 330 (Semi)     | LightRounds    | 3 / 0.36s    | 22 m, 7 rds, draw 0.42s |
| `LugerP08_T1`     | Pistol · C-Series | 27 × 400 (Semi)     | LightRounds    | 4 / 0.45s    | 22 m, 8 rds, draw 0.4s |
| `WaltherPP_T1`    | Pistol · C-Series | 22 × 450 (Semi)     | LightRounds    | 5 / 0.53s    | 14 m, 8 rds, draw 0.3s |
| `MauserC96_T1`    | Pistol · C-Series | 30 × 380 (Semi)     | LightRounds    | 4 / 0.47s    | 30 m, 10 rds, draw 0.5s, 10-round stripper clip per step |
| `Glock18_T1`      | Pistol · A-Series | 18 × 1100           | LightRounds    | 6 / 0.27s    | 12 m, 20 rds, draw 0.35s |
| `Glock17_T1`      | Pistol · A-Series | 25 × 400 (Semi)     | LightRounds    | 4 / 0.45s    | The Glock 18's semi variant (20%): 18 m, 17 rds, draw 0.35s |
| `TEC9_T1`         | Pistol · A-Series | 18 × 950            | LightRounds    | 6 / 0.32s    | 13 m, 24 rds, draw 0.38s |
| `Beretta93R_T1`   | Pistol · A-Series | 22 × 1100 (Burst)   | LightRounds    | 5 / two bursts | 18 m, 20 rds, draw 0.4s |
| `CZ75Auto_T1`     | Pistol · A-Series | 20 × 1000           | LightRounds    | 5 / 0.24s    | 16 m, 16 rds, draw 0.38s |
| `DesertEagle_T1`  | Pistol · H-Series | 55 × 300 (Semi)     | HeavyRounds    | 2 / 0.20s    | 25 m, 7 rds, draw 0.55s, 15% armor pen |
| `Magnum44_T1`     | Pistol · H-Series | 60 × 200 (Semi)     | HeavyRounds    | 2 / 0.30s    | 30 m, 6 rds, draw 0.55s, one round per step, 15% armor pen |
| `TaurusJudge_T1`  | Pistol · H-Series | 9 × 6 pellets × 220 (Semi) | ShotgunShells  | 2 / 0.27s    | 8 m, 5 rds, draw 0.5s, one round per step — 2-shot inside 8 m |
| `ChiappaRhino_T1` | Pistol · H-Series | 42 × 320 (Semi)     | HeavyRounds    | 3 / 0.38s    | 24 m, 6 rds, draw 0.45s, one round per step |
| `RugerMarkIV_T1`  | Pistol · T-Series | 20 × 500 (Semi)     | LightRounds    | 5 / 0.48s    | 20 m, 10 rds, draw 0.32s, quietest (15 m) |
| `FiveSeveN_T1`    | Pistol · T-Series | 25 × 400 (Semi)     | LightRounds    | 4 / 0.45s    | 22 m, 20 rds, draw 0.35s, 30% armor pen |
| `Tac45_T1`        | Pistol · T-Series | 34 × 360 (Semi)     | LightRounds    | 3 / 0.33s    | 22 m, 10 rds, draw 0.35s |
| `P250_T1`         | Pistol · T-Series | 26 × 420 (Semi)     | LightRounds    | 4 / 0.43s    | 18 m, 13 rds, draw 0.32s |
| `Kar98k_T1`       | Sniper      | 95 × 45 (bolt)      | HeavyRounds    | 1 / 1.3s     | Body-shot two-taps only if damage rolls hot  |
| `M249_T1`         | LMG         | 30 × 700            | StandardRounds | 4 / 0.26s    | Best DPS, worst handling & 6.5s reload       |
| `M870_T1`         | Shotgun     | 12 × 8 pellets × 60 | ShotgunShells  | 1 / 0.15s    | All-pellets-hit at ≤10m; drops to 2-shot past that |

Higher tiers (T2–T5) aren't authored yet — those variants come from the
`WeaponGenerator` rolling `WeaponCategoryData` with a `StatRollProfile` and
its quality curve, once each category has its profile assigned.

The T1 assets are hand-tuned rather than rolled because we want a known
Common-tier baseline for every category *before* the roll system produces
per-weapon variance. When higher tiers land, they should be measurably
stronger than the T1 asset in the same category — that comparison is the
diagnostic for whether the quality curve is doing its job.

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
