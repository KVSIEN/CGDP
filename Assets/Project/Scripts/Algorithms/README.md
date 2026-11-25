# Algorithms

General-purpose algorithms, independent of any one feature, in `namespace CGD.Algorithms`.
Each file starts with what the algorithm is, how it works and what it's good for. Read that
first; the code underneath follows the same steps. Everything is plain C# (Unity's vector
types aside) and deterministic: anything random takes a seed or a `CGD.Core.RandomStream`, so
the same seed always gives the same result.

Tests: `Tests/EditMode/Algorithms/`.

## What's here

| Folder | File | In one line | Good for |
|---|---|---|---|
| Noise | `CoordinateHash` | A random-looking number for any coordinate, looked up rather than drawn in order | Per-tile randomness without storing anything; the base of every noise here |
| | `ValueNoise` | Random values on a grid's corners, blended smoothly | The simplest noise: flicker, wander, rough variation |
| | `PerlinNoise` | Random directions on a grid's corners, blended: natural hills and valleys | Terrain, dirt, clouds, camera shake, wind, idle wobble |
| | `SimplexNoise` | Perlin's successor on a triangle grid: cheaper, no grid artefacts | Anything Perlin does, especially when sampled a lot |
| | `WorleyNoise` (+ `WorleySample`) | Distance to scattered feature points: cells and their borders | Cracks, cobbles, scales, cell growth, territories |
| | `FractalNoise` (+ `FractalMode`) | Several sizes of a noise added up (fBm); billowy or ridged | Detail: rock, bark, rough floors, clouds, mountain ridges |
| | `DomainWarpNoise` | A noise sampled where another noise pushed the point | Organic, flowing, creeping shapes: overgrowth, marble, rivers |
| | `INoise2D`, `NoiseMath` | The shape every noise shares; the fade curve | Swapping one noise for another; layering any of them |
| Sampling | `PoissonDiskSampler` | Random points never closer than a set distance (blue noise) | Even, natural scatter: trees, rocks, pickups, spawns, decor |
| Collections | `MinHeap` | Always hands out the lowest-priority item next | A*, Dijkstra, schedulers, "nearest first" |
| | `ShuffleBag` | Random draws without repeats until every item has come out | Fair loot, barks and voice lines, variety without streaks |
| | `DisjointSet` | Union-find: which things are connected, answered instantly | Connectivity checks, Kruskal, mazes |
| Graphs | `MinimumSpanningTree` (+ `GraphEdge`) | Kruskal: the cheapest links that connect everything | Connecting rooms, caves or towns; add a few skipped links back for loops |
| Grids | `GridDistanceMap` | Steps from every tile to the nearest source (a "Dijkstra map") | Chase and flee for many agents at once; "how far from a doorway?" |
| | `GridPathfinder` | A*: the shortest path between two tiles, with optional costs and diagonals | Tile-based movement, checking a level can be crossed |
| | `GridLine` | Bresenham: the tiles a straight line crosses | Tile line of sight, shots, drawing walls and roads |
| | `CellularAutomaton` | Cells living or dying by their neighbours, in rounds | Caves, lakes, patches of spreading growth |
| | `DrunkardsWalk` | A random walk that keeps every tile it stood on | Tunnels, connected blobs, creep spreading from a point |
| | `GridNeighbours` | The 4 and 8 steps to a tile's neighbours | Shared by the grid algorithms |
| Spatial | `SpatialHashGrid` | Buckets by position, so "what's near here?" checks only a few | Crowds, flocking, separation, hearing, pick-ups |
| Curves | `Easing` | Reshaped 0..1 progress: ease in, out, overshoot, bounce | Polish on any movement or UI transition |
| | `BezierCurve` | Smooth curves pulled toward control points | Arcs (grenades, leaps), swoops, hanging cables |
| | `CatmullRomSpline` | A smooth curve through every one of its points | Patrol routes, camera rails, smoothing a path |
| Motion | `SpringDamper` | A value chasing a target like a weight on a spring | Weapon sway, camera follow, UI that pops and settles |
| Growth | `LSystem` | Rewrites a string by rules, round after round | Plants, vines, roots, cracks, coral |
| | `Turtle2D` | Draws an L-system's string as lines | Turning that growth into geometry |

## Using them

```csharp
using CGD.Algorithms;

// Rolling, creeping dirt height for a point (x, z) in metres: warped fractal Perlin noise, 6 m lumps.
INoise2D dirt = new DomainWarpNoise(
    new FractalNoise(new PerlinNoise(seed), octaves: 4),
    new FractalNoise(new SimplexNoise(seed + 1), octaves: 2),
    strength: 1f);
float height = NoiseMath.ToUnitRange(dirt.Sample(x / 6f, z / 6f)) * 0.3f;

// Bushes scattered at least 2 m apart over a 20 x 14 m floor, off the walkway.
List<Vector2> bushes = PoissonDiskSampler.Sample(20f, 14f, 2f, random, spot => !OnWalkway(spot));

// Fair loot: every item drops once before any repeats.
var drops = new ShuffleBag<LootItem>(random);
drops.Add(commonItem, copies: 3);
drops.Add(rareItem);
LootItem next = drops.Next();
```

Noises return about -1..1 with features about 1 unit across: divide the input to make them
bigger, and use `NoiseMath.ToUnitRange` for 0..1.

## Elsewhere in the project

Some algorithms live with the feature they serve:

- `Core/Random/RandomStream`, `Seed`: the seeded random number generator everything here draws from.
- `Core/Random/WeightedChoice`: a weighted random pick in one pass (reservoir sampling).
- `Level/Layout/CorridorRouter`: A* specialised for corridors (turns cost extra).
- `Level/Overgrowth/OvergrowthNoise`: warped fractal noise on Unity's `Mathf.PerlinNoise`.
- `Level/Overgrowth/GridSlopeLimit`: a chamfer distance transform, used to keep slopes walkable.
