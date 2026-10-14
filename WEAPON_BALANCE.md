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
- **Models** (`Models` on the category, all equal weight, so each series is 20%):
  - **B-Series** (big bore: .50 anti-materiel; one body shot at any range, 40% armor penetration on the T1s): Barrett M82A1 (semi, 10 rds), AS50 (faster, lighter semi), Tac-50 (bolt, biggest single hit in the game: 165), M200 Intervention (bolt, longest reach, deepest scope)
  - **C-Series** (classics; older bolt-actions loaded one round per step): Kar98k, Mosin-Nagant (hardest-hitting classic, the only one-shot body kill), Lee-Enfield (fastest bolt, 10 rds — a slow full reload), Springfield M1903 (most accurate, least recoil)
  - **D-Series** (designated marksman rifles: semi-auto, 2–3 shots, **Standard Rounds** — shared with ARs/LMGs, not the sniper pool): M110 (all-rounder, 20 rds), M21 EBR (faster, more kick), PSG1 (precision: lowest recoil, tightest cone), QBU-88 (bullpup: 260 RPM, 3-shot kill, quickest handling)
  - **S-Series** (Soviet and Russian): SVD Dragunov (semi), SVU (bullpup SVD; 20% the full-auto "SVU-A"), VSS (integrally suppressed — heard at 12 m — short range, 20 rds, on Standard Rounds), SV-98 (the series' bolt-action)
  - **T-Series** (tactical: modern bolt-actions, one body shot): L96A1 (20% "AWP" — the .338 magnum AWM that Counter-Strike's AWP and the British L115A3 are: ~132 dmg × 38 RPM, 216 m, more kick), M40 (fastest modern bolt; 20% "M2010", .300 Win Mag), DSR-1 (bullpup: fastest draw and ADS; 20% "SRS"), JNG-90 (10-round modern bolt)
  - Scales and leans are built like the AR's, against the sniper midpoints (107.5 dmg, 150 RPM, 135 m, 6.67 rds — the median of its low-weighted 5–12 window — 3.9 recoil, 10.5° hip spread). The category's fire rate, damage, range and magazine windows were narrowed to AR-like widths around those midpoints (RPM 100–200, damage 75–140, range 70–200 m, magazine 5–12) because a model's window is the category's times its scale: the old DMR-to-AMR spans (35–260 RPM, 45–170 dmg) gave a Kar98k 11–81 RPM. The low magazine weighting stacks with a small-magazine model's lean, so 5-round rifles mostly roll 4.
- **Best at**: single-target elimination from >80m; Time To First Kill from ambush.
- **Weakness**: sustained fire, close quarters, slow bolt cycle, small magazine, HeavyRounds are scarce (shared with hand cannons).
- **What a Legendary sniper feels like**: same one-shot potential, better bolt cycle (lower RPM tax), tighter first-shot scope, faster reload.

### DMR
- **Models**: the sniper D-Series (M110, M21 EBR, PSG1, QBU-88), plus the S-Series' SVD, SVU and VSS.
- **Best at**: precise ranged fire with follow-up shots; picks between Sniper and AR range bands. Hard-hitting variants (low RoF, heavy recoil, damage rolled to the top of the range) drop into two-shot territory to body — one-shot to head — trading cadence for burst damage.
- **Weakness**: outclassed at extreme range by sniper, at close quarters by AR/SMG. StandardRounds shared with AR/LMG.
- **What a Legendary DMR feels like**: near-sniper accuracy at half the cycle time.

### AR
- **Models** (`Models` on the category, all equal weight, so each series is 20%):
  - **M-Series** (AR platform; precise, low recoil): M4 (auto carbine; 20% "Burst M4"), M16 (burst marksman; 20% the full-auto "M16A3" — full M16 reach at ~23 × 800, with more kick than the M27; the M14 is the series' semi-auto), M14 (battle rifle, semi; 20% "Auto M14", 20 rds), M27 (automatic rifle: lowest recoil, slow reload)
  - **AK-Series** (hard-hitting, heavy kick): AK-47 (heaviest climb and drift), AK-74u (short carbine), AKM (tamer AK-47), AK-12 (modern, accurate; 30% "Burst AK-12")
  - **B-Series** (bullpups: rifle range, compact handling): FAMAS (1000 RPM burst; 20% "Auto FAMAS", 25 rds), AUG (cleanest recoil, tightest optic), TAR-21 (close quarters, 900 RPM), F2000 (low recoil, mid range)
  - **H-Series** (battle rifles, 20 rds): FAL (70% semi, 15% burst, 15% auto), SCAR-H, G3 (heaviest auto), HK416 (the series' 5.56 all-rounder, 30 rds)
  - **C-Series** (compact: fast handling, short range): G36C (fastest fire rate), Galil (35 rds), AM-17 (fastest draw and ADS, shortest range), QBZ-95
  - Each model also leans (−1…1 per stat, added to the range's bias): its window stays the same but the odds shift toward its style, so a model's signature stats land on its style's side of its T1 value about two thirds of the time instead of half (M14 damage, G36C fire rate, M27 recoil). Leans aren't authored — `WeaponGenerator` derives them from the rolled gun's scales (model × firing variant) as 1.5 × (scale − 1), capped at ±0.6, with scales within 5% of the midpoint neutral. Tuning a model or variant is therefore only ever its `Scales`; a variant that rescales its model (Auto FAMAS, Auto M14, M16A3, M712, the semi Glock 17, every "Burst") automatically leans toward its own style. Categories without models (the Modular Pistol) don't lean.
  - Each model's scales are its T1 row divided by the category midpoints (26 dmg, 675 RPM, 60 m, 30 rds, 1.45 recoil, 3.15° hip spread), so generated rifles of a name centre on their T1 numbers. Burst and semi models (M16, FAMAS, M14, FAL) beat the 0.35 s target on paper; landing every round of two bursts, or tapping at the cap, is their price.
- **Best at**: no other class beats it at 30–60m; the reliable choice.
- **Weakness**: outclassed at every other range's optimum by that range's specialist.
- **What a Legendary AR feels like**: cleaner recoil pattern, faster reload, no accuracy tax on sustained fire.

### LMG
- **Best at**: sustained fire against multiple enemies; holding a chokepoint.
- **Weakness**: slow draw, slow reload, poor handling, wide first-shot spread, StandardRounds shared with AR/DMR (competition).
- **What a Legendary LMG feels like**: tighter first-mag accuracy, less recoil heat buildup, faster ADS.
- **Models** (`Models` on the category, all equal weight, so each series is 20%; most land at 4 STK in 0.20–0.30 s, the 3-STK guns fire at 500–550 RPM for 0.22–0.27 s; magazines scale with each model's real capacity, so the classics roll small and the heavies large):
  - **M-Series** (modern squad automatics): M249 SAW (20% "Mk 48" — 7.62: 1.2× damage, 0.85× fire rate, more kick), Negev (900 RPM, jumpy zig-zag), Ultimax 100 (constant recoil: the softest kick in the category, barely flips), Stoner 63 (precise, controllable)
  - **S-Series** (Soviet and Russian): RPK (AK-pattern: smallest magazine of the series, kicks up and right like the AKs), PKM (20% "PKP Pecheneg" — 0.85× recoil and spread), RPD (steady drum-fed belt), DP-27 (pan magazine: 38 × 550, accurate)
  - **G-Series** (general-purpose 7.62 belt-feds): MG42 (1200 RPM "buzzsaw", 24 dmg, 5 STK in 0.20 s, wildest climb), M60 (42 × 550, climbs straight up, flips hardest), FN MAG (steady all-rounder), HK21 (G3-based: kicks up and right)
  - **C-Series** (classic squad automatics, small magazines): BAR (45 × 550, 20 rds, hardest-hitting standard LMG), Lewis Gun (47-round pan), Bren (most accurate, softest classic, 30 rds), MG34 (900 RPM, the classic belt-fed)
  - **H-Series** (heavy machine guns: slowest handling, biggest belts): Maxim and Vickers (water-cooled: near-zero climb and flip, longest range), Browning M1919, M2 Browning (.50 on **HeavyRounds**: 45 × 450, hardest kick, 30% armor penetration on the T1)
  - Scales are built against the LMG midpoints (34.5 dmg, 850 RPM, 57.5 m, 86 rds — the median of its low-weighted 50–200 window — 1.7 recoil, 4.75° hip spread).

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
- **Reload**: each model sets its own — tube-fed models (pumps, semi-autos, the H-Series, the Model 1887 and Model 12) load one shell per step (the Mossberg 940 two), the M-Series magazine-fed and the break-actions both barrels at once; each step takes ReloadTime ÷ steps for a full load (about 0.45–0.6 s), so the Tube axis trades capacity for a slower full reload but not slower shells. Loaded shells survive any interrupt, which enables blast → one shell → bash-cancel → blast. The full reload is the category's ReloadTime whatever the capacity, so a big tube (KSG, Striker) loads each shell faster rather than taking longer overall. Sniper rifles are the reverse, 4 : 1 magazine to one-round-per-step: only the C-Series classics (Kar98k, Mosin-Nagant, Lee-Enfield, Springfield M1903) load a round at a time. Revolvers (pistol H-Series) weight one round per step 85 : 15 against a speedloader full load.
- **Models** (`Models` on the category, all equal weight, so each series is 20%; tube pumps are 1-shot kills up close at T1, semis and autos 2-shot at 0.20–0.35 s):
  - **P-Series** (pump-actions; slow, one-shot): Remington 870 (the baseline), Mossberg 590A1 (9 shells; 20% "Shockwave" — stockless: smaller tube, shorter reach, wider cone, wild kick of its own), SPAS-12 (hardest-hitting pump; 30% roll its semi-auto mode, "Semi SPAS-12": ~190 RPM at 0.8× damage), UTS-15 (bullpup pump, 15 shells)
  - **A-Series** (semi-autos; quick 2-shot follow-ups): Benelli M4 (all-rounder), Beretta 1301 (fastest cycle, 5 shells), Mossberg 940 (10 shells, loads two per step), Beretta Xtrema 2 (magnum semi: hardest hits and least kick of the series, slowest cycle)
  - **M-Series** (magazine-fed; whole-magazine reloads): AA-12 (full-auto 300 RPM, near-recoilless, barely flips; 20% "Drum AA-12", 2.5× magazine), USAS-12 (slower, harder-hitting full-auto that climbs up and right), Saiga-12 (AK-pattern semi, kicks up and right), STF-12 (AR-pattern semi: lightest and most controllable)
  - **H-Series** (high capacity; one shell per step): KSG (twin-tube bullpup pump, 14 shells), Tavor TS12 (triple-tube bullpup semi, 15 shells), Striker (12-round rotary drum semi), Neostead 2000 (front-pump twin-tube bullpup: fastest pump of the series, tightest)
  - **C-Series** (classics and break-actions): Double Barrel (2 shells, fired as fast as you pull: 16 × 10 at 300 RPM, both reloaded at once; 20% "Sawed-Off" — shorter reach, wider cone, a harder kick of its own), Browning Superposed (over-under: tightest cone and longest reach in the category, 2 shells), Model 1887 (lever-action: hardest pump-speed hits, 5 shells), Model 12 (slam-fire trench gun: the fastest-cycling pump, 90 RPM)
  - The break-actions use `FixedMagazineSize` 2, so the magazine roll never gives them one barrel or three.

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

### Bow
- **Best at**: silent picks — nearly inaudible (~8 m), one-shot headshots at every tier, no reload.
- **Weakness**: every shot is a draw (0.6–1.2 s at the category's middle, more in the vertical stance), arrows arc and must be led, one arrow nocked at a time.
- **What a Legendary bow feels like**: faster draw (charge time follows quality on the draw-speed axis), harder hits, tighter hip cone.
- **Models** (`Models` on the category, all equal weight, so each series is 20%; every T1 one-shots a head, and the heavy hitters — ≥ 87 damage — one-shot a body at a full vertical draw (×1.15)):
  - **L-Series** (longbows: slow draw, heaviest hits, arcing arrows): English Longbow (the baseline heavy bow), Warbow (107 dmg, the hardest-hitting bow; slowest draw, 1.22 s), Yumi (precise: tight cone, longest longbow reach), Flatbow (slowest arrow, the most arc)
  - **R-Series** (recurves: balanced, accurate): Olympic Recurve (tightest traditional cone, softest kick), Takedown Recurve (the all-rounder), Scythian Bow (quick, short reflex), Turkish Flight Bow (fastest traditional arrow and longest reach in the category, lightest hits)
  - **H-Series** (horsebows: quickest draw, short range): Mongol Bow and Magyar Bow (multishot by even odds: 1, 2 or 3 arrows ⅓ each — T1s loose 2), Gakgung (small horn bow with a fast, far-flying arrow; volleys less often), Manchu Bow (heavy arrows: big close hits, slow and arcing)
  - **S-Series** (shortbows: light and quick): Shortbow (lightest hits, quick), Hunting Bow (steady middle), Pyramid Bow (tapered limbs: the smoothest shortbow), Self Bow (hits hardest of the series but the loosest cone)
  - **C-Series** (compounds: fastest, flattest arrows, near-dead in the hand): Hunting Compound (all-rounder), Target Compound (tightest cone in the game's bows, slower draw), Speed Compound (fastest arrow, 94 m/s at T1, but the hardest kick), Short-Axle Compound (quick draw, shorter reach)
  - Horsebows set their own volley odds through a firing style (`PelletCount` 1–3; bias 0 for the Mongol and Magyar, −0.4 for the Gakgung and Manchu); every other bow keeps the category's (mostly single arrows).
  - Bows lean on two scales firearms leave at 1: `ChargeTime` (time to full draw) and `ProjectileSpeed` (arrow speed — a faster arrow drops less over the same distance). Scales are built against the bow midpoints (82.5 dmg, 160 RPM, 45 m, 1.1° hip spread, 0.9 s draw, 65 m/s).

### Crossbow
- **Best at**: heavy, armour-piercing single shots that fly flat and quiet (~10 m); most T1s one-shot a body.
- **Weakness**: spanning after every bolt (most hold one), so a miss costs 1.4–3.2 s; weak hipfire.
- **What a Legendary crossbow feels like**: faster spanning, harder hits, tighter cone.
- **Models** (`Models` on the category, all equal weight, so each series is 20% — autoloaders are now one in five, down from the category's old one in three; every model sets its own fire mode, so the category's Fire Modes no longer apply):
  - **L-Series** (light: quick span, light bolts): Pistol Crossbow (72 dmg, 1.38 s — the fastest single-shot reload), Hand Crossbow, Goat's-Foot Crossbow (lever-spanned), Sporting Crossbow (102 dmg: the first to one-shot a body)
  - **W-Series** (war: single-shot, slowest span, hardest hits): Arbalest (150 dmg, 3 s), Windlass Crossbow, Cranequin Crossbow (rack-and-pinion: the quickest war crossbow), Hand Ballista (the torsion manuballista: 174 dmg, 3.2 s, hardest kick)
  - **R-Series** (repeaters, semi-auto): Chu-ko-nu (66 dmg, 231 RPM, magazine scale 2.4 — T1 holds 10), Double Crossbow (always 2 bolts via `FixedMagazineSize`, 248 RPM back to back), Repeating Crossbow (magazine scale 1.6 — T1 holds 5), Revolving Crossbow (fixed six-bolt cylinder, loads one bolt per step)
  - **X-Series** (tech crossbows, on the automatic 6–15 magazine): XBow (burst; 20% "Auto XBow"), Rail Crossbow (semi: 160 m/s, 81 m, tightest cone, small magazine), Bolt Caster (light full-auto: 54 × 231), Bolt Thrower (heavy full-auto, magazine scale 1.5, slow reload)
  - **C-Series** (compounds: fast, flat bolts, quick span): Hunting Crossbow (the category's middle: 120 dmg), Reverse-Draw Crossbow (130 m/s), Tactical Crossbow (fastest compound span), Recurve Crossbow
  - Crossbows lean on `Reload` (spanning time; full and tactical reload alike) and `ProjectileSpeed`. Armour penetration stays the category's (25–50%). Scales are built against the crossbow midpoints (120 dmg, 165 RPM, 60 m, 1.75° hip spread, 100 m/s, 2.3 s reload).

## Recoil Patterns

Each model carries a `RecoilPattern` (on `WeaponModel`; a firing variant can carry its own with
`OwnRecoil`). Generated weapons take it as is; quality and leans still scale the kick itself.

- **Bias** (−1 left … +1 right): the lean of every shot, × the horizontal kick.
- **Drift**: *Alternating* swings back when the sideways drift reaches its cap (zig-zag);
  *OneWay* keeps pushing toward the bias and holds at the cap.
- **Jitter**: shot-to-shot sideways randomness as a fraction of the horizontal kick — low reads as a
  laser, high as a jumpy gun.
- **Horizontal**: sideways kick relative to the category's usual share for this much recoil. Set from
  the T1: (T1 horizontal ÷ category horizontal midpoint) ÷ (T1 vertical ÷ category vertical midpoint).
- **Reach**: the sideways cap relative to the usual. The usual caps are far wider than a magazine's
  drift (≈10° on an AR), so a zig-zag only swings when Reach is low (0.15–0.2: AK-47, AK-74u, Galil,
  PP2000, Scorpion EVO, PP-19 Bizon, CZ 75 Auto, M249; MG42 0.3 and Negev 0.4 swing wider).

| Shape | Pattern | Models |
|---|---|---|
| Up and right, one way | bias +0.2…+0.6, OneWay | **Uzi** (+0.6), Glock 18, Mauser C96 / M712, .44 Magnum, AKM, Thompson, G3, Desert Eagle, MP9, SVD, VSS, G36C, APC9, M21 EBR, TAR-21, M4, AA-12 |
| Up and left, one way | bias −0.2…−0.3, OneWay | SCAR-H, QBZ-95, MPL, UMP-45, PM12, QCW-05, QBU-88 |
| Zig-zag (right, then back left) | bias +0.25…+0.5, Alternating, low Reach | **AK-47**, Galil, AK-74u, PP2000, Scorpion EVO, PP-19 Bizon, CZ 75 Auto, M249, MG42, Negev |
| Jumpy, no clear lean | jitter 0.5–0.7 | MAC-10 (0.7), TEC-9, AK-74u, AM-17, FAL, M14, SVU |
| Nearly straight up | bias 0…±0.1, jitter ≤ 0.25 | Vector, AUG, MP5, MP7, M27, HK416, F2000 (slightly left), P90 (bouncy), Ruger Mark IV, Chiappa Rhino, PSG1, DSR-1 |

**MuzzleFlip** (0–1, visual only; also on `WeaponData`): how the held gun moves on each shot.
`WeaponVisuals` shares the vertical kick between rotation (muzzle rise and hop, × 2·flip) and
pushback (× 2·(1 − flip)), so 0.5 looks as before. Pushback is per degree of vertical kick, so a
hand cannon shoves back further than a .22. Low bore axis or an inline stock pushes back (Vector
0.05, Chiappa Rhino 0.15, Glock / M9 / CZ 75 / P90 / AA-12 / DSR-1 0.2–0.25, AR platform and
bullpups 0.25–0.35); a high bore axis, big calibre or dropped stock flips (M1911 0.8, Tac-45 0.75,
Desert Eagle / Judge 0.85, .44 Magnum 0.95, Mauser C96 0.75 / M712 0.85, AKs and battle rifles
0.5–0.65, classic bolt-actions 0.5–0.6). From the hip a Glock 17 now rears 1.2° and shoves back
4.8 cm; an M1911 rears 6.2° and shoves 1.6 cm (both were ~3–4° and 3 cm before).

Semi-autos, pistols and bolt-actions get small biases (0–0.3) and mostly use jitter for character:
per-shot scatter matters more than drift when the caps reset between shots. Recoil simulated on the
T1s (ADS, 20 rounds, averaged): Uzi ends +5° right for 10° up; AK-47 reaches +1.5° by round 8 and
crosses to −1.3° by round 20; SCAR-H ends −2° left for 21° up; Vector stays at 0°.

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
| Sniper  | Semi-auto DMR — 260 RPM, 45 dmg, 50 m | Bolt / AMR — 35 RPM, 170 dmg, 220 m   | 20 models in five series (see Sniper); e.g. `Kar98k_T1` 45×95, `QBU88_T1` 260×45, `Tac50_T1` 38×165 |
| LMG     | High-cyclic MG — 1200 RPM, 24 dmg    | Heavy GPMG — 500 RPM, 45 dmg           | 20 models in five series (see LMG); e.g. `M249_T1` 700×30, `MG42_T1` 1200×24, `BAR_T1` 550×45 |
| Shotgun | Auto — 260 RPM, 9 dmg × 6 pellets    | Pump — 60 RPM, 18 dmg × 8 pellets      | 20 models in five series (see Shotgun); e.g. `Remington870_T1` 65×13×8, `AA12_T1` 300×9×7, `DoubleBarrel_T1` 300×16×10 (2 shells) |

The Sniper band is where the **DMR row** in the category table above comes from.
There is no separate DMR `WeaponType` — marksman rifles are the sniper D-Series
(and the S-Series semi-autos), the fast end of Sniper. The poles above are the
band the models span; the category's own windows are narrower (see Sniper).

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
`Assets/Project/Data/Weapons/Ranged/T1/` — one per model for the AR, SMG,
pistol, sniper, shotgun, LMG, bow and crossbow rosters. They are the concrete example of what "Common feels like" and the
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
| `BarrettM82A1_T1`     | Sniper · B-Series | 130 × 120 (semi)    | HeavyRounds    | 1 / one shot | 180 m, 10 rds, draw 0.95s, 40% armor pen |
| `AS50_T1`             | Sniper · B-Series | 115 × 160 (semi)    | HeavyRounds    | 1 / one shot | 160 m, 5 rds, draw 0.85s, 40% armor pen |
| `Tac50_T1`            | Sniper · B-Series | 165 × 38 (bolt)     | HeavyRounds    | 1 / one shot | 220 m, 5 rds, draw 1.0s, 40% armor pen |
| `M200_T1`             | Sniper · B-Series | 150 × 45 (bolt)     | HeavyRounds    | 1 / one shot | 220 m, 7 rds, draw 0.95s, 40% armor pen |
| `Kar98k_T1`           | Sniper · C-Series | 95 × 45 (bolt)      | HeavyRounds    | 2 / 1.33s    | 120 m, 5 rds, draw 0.7s, one round per step |
| `MosinNagant_T1`      | Sniper · C-Series | 100 × 40 (bolt)     | HeavyRounds    | 1 / one shot | 130 m, 5 rds, draw 0.75s, one round per step |
| `LeeEnfield_T1`       | Sniper · C-Series | 85 × 60 (bolt)      | HeavyRounds    | 2 / 1.00s    | 110 m, 10 rds, draw 0.7s, one round per step |
| `SpringfieldM1903_T1` | Sniper · C-Series | 95 × 45 (bolt)      | HeavyRounds    | 2 / 1.33s    | 140 m, 5 rds, draw 0.7s, one round per step |
| `M110_T1`             | Sniper · D-Series | 55 × 170 (semi)     | StandardRounds | 2 / 0.35s    | 90 m, 20 rds, draw 0.65s |
| `M21EBR_T1`           | Sniper · D-Series | 50 × 190 (semi)     | StandardRounds | 2 / 0.32s    | 80 m, 20 rds, draw 0.65s |
| `PSG1_T1`             | Sniper · D-Series | 70 × 160 (semi)     | StandardRounds | 2 / 0.38s    | 110 m, 10 rds, draw 0.7s |
| `QBU88_T1`            | Sniper · D-Series | 45 × 260 (semi)     | StandardRounds | 3 / 0.46s    | 70 m, 10 rds, draw 0.6s |
| `SVD_T1`              | Sniper · S-Series | 75 × 160 (semi)     | HeavyRounds    | 2 / 0.38s    | 110 m, 10 rds, draw 0.75s |
| `SVU_T1`              | Sniper · S-Series | 65 × 180 (semi)     | HeavyRounds    | 2 / 0.33s    | 80 m, 10 rds, draw 0.65s |
| `VSS_T1`              | Sniper · S-Series | 45 × 240 (semi)     | StandardRounds | 3 / 0.50s    | 50 m, 20 rds, draw 0.55s, suppressed (heard at 12 m) |
| `SV98_T1`             | Sniper · S-Series | 105 × 50 (bolt)     | HeavyRounds    | 1 / one shot | 150 m, 10 rds, draw 0.75s |
| `L96A1_T1`            | Sniper · T-Series | 110 × 45 (bolt)     | HeavyRounds    | 1 / one shot | 180 m, 5 rds, draw 0.8s |
| `M40_T1`              | Sniper · T-Series | 100 × 55 (bolt)     | HeavyRounds    | 1 / one shot | 160 m, 5 rds, draw 0.75s |
| `DSR1_T1`             | Sniper · T-Series | 105 × 50 (bolt)     | HeavyRounds    | 1 / one shot | 150 m, 5 rds, draw 0.6s |
| `JNG90_T1`            | Sniper · T-Series | 105 × 45 (bolt)     | HeavyRounds    | 1 / one shot | 170 m, 10 rds, draw 0.8s |
| `M249_T1`           | LMG · M-Series | 30 × 700            | StandardRounds | 4 / 0.26s    | 55 m, 100 rds, draw 0.9s, 6.5s reload |
| `Negev_T1`          | LMG · M-Series | 27 × 900            | StandardRounds | 4 / 0.20s    | 50 m, 150 rds, draw 0.9s |
| `Ultimax100_T1`     | LMG · M-Series | 28 × 650            | StandardRounds | 4 / 0.28s    | 50 m, 100 rds, draw 0.8s, softest kick |
| `Stoner63_T1`       | LMG · M-Series | 29 × 750            | StandardRounds | 4 / 0.24s    | 55 m, 75 rds, draw 0.85s |
| `RPK_T1`            | LMG · S-Series | 32 × 600            | StandardRounds | 4 / 0.30s    | 55 m, 45 rds, draw 0.8s, 4.5s reload |
| `PKM_T1`            | LMG · S-Series | 32 × 650            | StandardRounds | 4 / 0.28s    | 65 m, 100 rds, draw 1.0s |
| `RPD_T1`            | LMG · S-Series | 33 × 650            | StandardRounds | 4 / 0.28s    | 60 m, 100 rds, draw 0.95s |
| `DP27_T1`           | LMG · S-Series | 38 × 550            | StandardRounds | 3 / 0.22s    | 70 m, 47 rds, draw 1.0s |
| `MG42_T1`           | LMG · G-Series | 24 × 1200           | StandardRounds | 5 / 0.20s    | 55 m, 120 rds, draw 1.05s, wildest climb |
| `M60_T1`            | LMG · G-Series | 42 × 550            | StandardRounds | 3 / 0.22s    | 65 m, 100 rds, draw 1.05s, 8s reload |
| `FNMAG_T1`          | LMG · G-Series | 33 × 700            | StandardRounds | 4 / 0.26s    | 70 m, 100 rds, draw 1.05s |
| `HK21_T1`           | LMG · G-Series | 33 × 800            | StandardRounds | 4 / 0.23s    | 65 m, 100 rds, draw 0.9s |
| `BAR_T1`            | LMG · C-Series | 45 × 550            | StandardRounds | 3 / 0.22s    | 70 m, 20 rds, draw 0.8s, 4s reload |
| `LewisGun_T1`       | LMG · C-Series | 38 × 550            | StandardRounds | 3 / 0.22s    | 65 m, 47 rds, draw 1.0s |
| `Bren_T1`           | LMG · C-Series | 40 × 520            | StandardRounds | 3 / 0.23s    | 75 m, 30 rds, draw 0.85s |
| `MG34_T1`           | LMG · C-Series | 33 × 900            | StandardRounds | 4 / 0.20s    | 60 m, 75 rds, draw 0.95s |
| `Maxim_T1`          | LMG · H-Series | 40 × 550            | StandardRounds | 3 / 0.22s    | 75 m, 150 rds, draw 1.15s, 8.5s reload |
| `Vickers_T1`        | LMG · H-Series | 42 × 500            | StandardRounds | 3 / 0.24s    | 80 m, 150 rds, draw 1.15s, 8.5s reload |
| `BrowningM1919_T1`  | LMG · H-Series | 40 × 500            | StandardRounds | 3 / 0.24s    | 70 m, 125 rds, draw 1.1s |
| `M2Browning_T1`     | LMG · H-Series | 45 × 450            | HeavyRounds    | 3 / 0.27s    | 80 m, 100 rds, HeavyRounds, 30% armor pen, draw 1.15s |
| `Remington870_T1`       | Shotgun · P-Series | 13 × 8 × 65 (pump)  | ShotgunShells  | 1 / one shot | 10 m, 6 shells, draw 0.6s, one shell per step |
| `Mossberg590A1_T1`      | Shotgun · P-Series | 13 × 8 × 60 (pump)  | ShotgunShells  | 1 / one shot | 11 m, 9 shells, draw 0.7s, one shell per step |
| `SPAS12_T1`             | Shotgun · P-Series | 14 × 8 × 60 (pump)  | ShotgunShells  | 1 / one shot | 10 m, 8 shells, draw 0.75s, one shell per step |
| `UTS15_T1`              | Shotgun · P-Series | 12.5 × 8 × 65 (pump) | ShotgunShells  | 1 / one shot | 9 m, 15 shells, draw 0.65s, one shell per step |
| `BenelliM4_T1`          | Shotgun · A-Series | 10 × 8 × 200 (semi) | ShotgunShells  | 2 / 0.30s    | 11 m, 7 shells, draw 0.65s, one shell per step |
| `Beretta1301_T1`        | Shotgun · A-Series | 9.5 × 8 × 240 (semi) | ShotgunShells  | 2 / 0.25s    | 9 m, 5 shells, draw 0.55s, one shell per step |
| `Mossberg940_T1`        | Shotgun · A-Series | 10 × 8 × 220 (semi) | ShotgunShells  | 2 / 0.27s    | 12 m, 10 shells, draw 0.7s, two shells per step |
| `BerettaXtrema2_T1`     | Shotgun · A-Series | 12 × 8 × 170 (semi) | ShotgunShells  | 2 / 0.35s    | 13 m, 6 shells, draw 0.8s, one shell per step |
| `AA12_T1`               | Shotgun · M-Series | 9 × 7 × 300 (auto)  | ShotgunShells  | 2 / 0.20s    | 9 m, 8 shells, draw 0.75s, magazine |
| `USAS12_T1`             | Shotgun · M-Series | 11 × 7 × 240 (auto) | ShotgunShells  | 2 / 0.25s    | 10 m, 10 shells, draw 0.85s, magazine |
| `Saiga12_T1`            | Shotgun · M-Series | 10.5 × 8 × 230 (semi) | ShotgunShells  | 2 / 0.26s    | 11 m, 8 shells, draw 0.7s, magazine, 2.6s reload |
| `STF12_T1`              | Shotgun · M-Series | 9.5 × 8 × 250 (semi) | ShotgunShells  | 2 / 0.24s    | 10 m, 5 shells, draw 0.6s, magazine, 2.4s reload |
| `KSG_T1`                | Shotgun · H-Series | 12.5 × 8 × 70 (pump) | ShotgunShells  | 1 / one shot | 10 m, 14 shells, draw 0.65s, one shell per step |
| `TavorTS12_T1`          | Shotgun · H-Series | 9.5 × 8 × 210 (semi) | ShotgunShells  | 2 / 0.29s    | 10 m, 15 shells, draw 0.7s, one shell per step |
| `Striker_T1`            | Shotgun · H-Series | 10 × 8 × 200 (semi) | ShotgunShells  | 2 / 0.30s    | 9 m, 12 shells, draw 0.85s, one shell per step |
| `Neostead2000_T1`       | Shotgun · H-Series | 12.5 × 8 × 75 (pump) | ShotgunShells  | 1 / one shot | 11 m, 12 shells, draw 0.65s, one shell per step |
| `DoubleBarrel_T1`       | Shotgun · C-Series | 16 × 10 × 300 (semi) | ShotgunShells  | 1 / one shot | 8 m, 2 shells (both barrels reload at once), draw 0.55s |
| `BrowningSuperposed_T1`  | Shotgun · C-Series | 15 × 8 × 240 (semi) | ShotgunShells  | 1 / one shot | 15 m, 2 shells, tightest cone (2° ADS), draw 0.6s |
| `Model1887_T1`          | Shotgun · C-Series | 15 × 8 × 75 (lever) | ShotgunShells  | 1 / one shot | 10 m, 5 shells, draw 0.7s, one shell per step |
| `Model12_T1`            | Shotgun · C-Series | 13 × 8 × 90 (pump)  | ShotgunShells  | 1 / one shot | 9 m, 6 shells, draw 0.6s, one shell per step |
| `EnglishLongbow_T1` | Bow · L-Series | 95 × 144 | Arrows | 1 vertical / 2 | draw 1.08s, 58 m/s, 50 m, hip 1.1° |
| `Warbow_T1` | Bow · L-Series | 107 × 128 | Arrows | 1 vertical / 2 | draw 1.22s, 62 m/s, 52 m, hip 1.26° |
| `Yumi_T1` | Bow · L-Series | 91 × 144 | Arrows | 1 vertical / 2 | draw 1.03s, 62 m/s, 54 m, hip 0.83° |
| `Flatbow_T1` | Bow · L-Series | 87 × 152 | Arrows | 1 vertical / 2 | draw 0.95s, 55 m/s, 45 m, hip 1.1° |
| `OlympicRecurve_T1` | Bow · R-Series | 82 × 160 | Arrows | 2 | draw 0.9s, 68 m/s, 52 m, hip 0.72° |
| `TakedownRecurve_T1` | Bow · R-Series | 78 × 168 | Arrows | 2 | draw 0.81s, 65 m/s, 45 m, hip 0.99° |
| `ScythianBow_T1` | Bow · R-Series | 74 × 184 | Arrows | 2 | draw 0.72s, 65 m/s, 40 m, hip 1.1° |
| `TurkishFlightBow_T1` | Bow · R-Series | 66 × 160 | Arrows | 2 | draw 0.85s, 81 m/s, 61 m, hip 1.04° |
| `MongolBow_T1` | Bow · H-Series | 78 × 184 | Arrows | 2 | draw 0.68s, 65 m/s, 40 m, hip 1.21°, 2-arrow volley |
| `MagyarBow_T1` | Bow · H-Series | 70 × 192 | Arrows | 2 | draw 0.63s, 62 m/s, 38 m, hip 1.26°, 2-arrow volley |
| `Gakgung_T1` | Bow · H-Series | 74 × 192 | Arrows | 2 | draw 0.63s, 72 m/s, 47 m, hip 1.1° |
| `ManchuBow_T1` | Bow · H-Series | 95 × 144 | Arrows | 1 vertical / 2 | draw 0.85s, 55 m/s, 36 m, hip 1.32° |
| `Shortbow_T1` | Bow · S-Series | 66 × 184 | Arrows | 2 | draw 0.72s, 58 m/s, 36 m, hip 1.21° |
| `HuntingBow_T1` | Bow · S-Series | 78 × 160 | Arrows | 2 | draw 0.81s, 62 m/s, 40 m, hip 1.04° |
| `PyramidBow_T1` | Bow · S-Series | 70 × 176 | Arrows | 2 | draw 0.77s, 58 m/s, 38 m, hip 1.04° |
| `SelfBow_T1` | Bow · S-Series | 82 × 168 | Arrows | 2 | draw 0.77s, 55 m/s, 38 m, hip 1.38° |
| `HuntingCompound_T1` | Bow · C-Series | 87 × 160 | Arrows | 1 vertical / 2 | draw 0.9s, 84 m/s, 50 m, hip 0.94° |
| `TargetCompound_T1` | Bow · C-Series | 82 × 144 | Arrows | 2 | draw 1.03s, 84 m/s, 56 m, hip 0.66° |
| `SpeedCompound_T1` | Bow · C-Series | 91 × 160 | Arrows | 1 vertical / 2 | draw 0.95s, 94 m/s, 54 m, hip 1.1° |
| `ShortAxleCompound_T1` | Bow · C-Series | 78 × 184 | Arrows | 2 | draw 0.72s, 78 m/s, 40 m, hip 1.04° |
| `PistolCrossbow_T1` | Crossbow · L-Series | 72 × 165 | Arrows | 2 / 1.38s reload | 1 bolt, reload 1.38s, 80 m/s, 42 m |
| `HandCrossbow_T1` | Crossbow · L-Series | 78 × 165 | Arrows | 2 / 1.49s reload | 1 bolt, reload 1.49s, 85 m/s, 45 m |
| `GoatsFootCrossbow_T1` | Crossbow · L-Series | 96 × 165 | Arrows | 2 / 1.61s reload | 1 bolt, reload 1.61s, 90 m/s, 51 m |
| `SportingCrossbow_T1` | Crossbow · L-Series | 102 × 165 | Arrows | 1 / — | 1 bolt, reload 1.84s, 95 m/s, 54 m |
| `Arbalest_T1` | Crossbow · W-Series | 150 × 165 | Arrows | 1 / — | 1 bolt, reload 2.99s, 110 m/s, 72 m |
| `WindlassCrossbow_T1` | Crossbow · W-Series | 138 × 165 | Arrows | 1 / — | 1 bolt, reload 2.76s, 105 m/s, 69 m |
| `CranequinCrossbow_T1` | Crossbow · W-Series | 132 × 165 | Arrows | 1 / — | 1 bolt, reload 2.42s, 105 m/s, 66 m |
| `HandBallista_T1` | Crossbow · W-Series | 174 × 165 | Arrows | 1 / — | 1 bolt, reload 3.22s, 115 m/s, 75 m |
| `ChuKoNu_T1` | Crossbow · R-Series | 66 × 231 | Arrows | 2 / 0.26s | 10 bolts, reload 2.53s, 75 m/s, 42 m |
| `DoubleCrossbow_T1` | Crossbow · R-Series | 114 × 248 | Arrows | 1 / — | 2 bolts, reload 2.3s, 95 m/s, 54 m |
| `RepeatingCrossbow_T1` | Crossbow · R-Series | 90 × 165 | Arrows | 2 / 0.36s | 5 bolts, reload 2.3s, 85 m/s, 48 m |
| `RevolvingCrossbow_T1` | Crossbow · R-Series | 84 × 182 | Arrows | 2 / 0.33s | 6 bolts, reload 2.76s, 85 m/s, 48 m, one bolt per step |
| `XBow_T1` | Crossbow · X-Series | 72 × 165 (Burst) | Arrows | 2 / 0.15s (one burst) | 10 bolts, reload 2.3s, 100 m/s, 51 m |
| `RailCrossbow_T1` | Crossbow · X-Series | 126 × 132 | Arrows | 1 / — | 2 bolts, reload 2.07s, 160 m/s, 81 m |
| `BoltCaster_T1` | Crossbow · X-Series | 54 × 231 (Auto) | Arrows | 2 / 0.26s | 10 bolts, reload 2.07s, 90 m/s, 48 m |
| `BoltThrower_T1` | Crossbow · X-Series | 78 × 165 (Auto) | Arrows | 2 / 0.36s | 15 bolts, reload 2.99s, 100 m/s, 54 m |
| `HuntingCrossbow_T1` | Crossbow · C-Series | 120 × 165 | Arrows | 1 / — | 1 bolt, reload 1.95s, 120 m/s, 63 m |
| `ReverseDrawCrossbow_T1` | Crossbow · C-Series | 126 × 165 | Arrows | 1 / — | 1 bolt, reload 2.07s, 130 m/s, 69 m |
| `TacticalCrossbow_T1` | Crossbow · C-Series | 102 × 165 | Arrows | 1 / — | 1 bolt, reload 1.61s, 110 m/s, 60 m |
| `RecurveCrossbow_T1` | Crossbow · C-Series | 108 × 165 | Arrows | 1 / — | 1 bolt, reload 1.84s, 100 m/s, 60 m |

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
