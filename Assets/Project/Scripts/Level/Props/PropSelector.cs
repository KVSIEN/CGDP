using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Picks which candidate spot a placement tries next, by its PropSelection: anywhere,
    // spread out, in patches, in a row, or across the longest open sightline. Candidates that
    // have stopped being placeable are dropped from the pool on the way.
    internal sealed class PropSelector
    {
        // Spread looks at this many candidates per pick and takes the one farthest from the rest.
        private const int SpreadSamples = 8;
        // Cluster looks at this many and takes the one nearest the rest, and now and then starts a new patch.
        private const int ClusterSamples = 12;
        private const float NewPatchChance = 0.12f;
        // For Line, a step across the room costs this many steps along it, so a row fills before another starts.
        private const float LineAcrossWeight = 6f;
        // Cover only goes where it breaks an open stretch at least this many tiles long.
        private const int CoverMinRun = 4;

        private readonly PropPlacementRules _rules;
        private readonly SightlineMap _sight;
        private readonly RandomStream _random;
        private readonly RectInt _bounds;
        // The middle of everything placed, for Spread.
        private readonly List<Vector2> _centres = new();

        public PropSelector(PropPlacementRules rules, SightlineMap sight, RandomStream random, RectInt bounds)
        {
            _rules  = rules;
            _sight  = sight;
            _random = random;
            _bounds = bounds;
        }

        public void NotePlaced(Vector2 centre) => _centres.Add(centre);

        // The index of the candidate to try next, or -1 when none is left. Candidates that stopped
        // being placeable are dropped on the way (floor only ever gets taken, never freed).
        internal int Select(RoomPropPlacement placement, List<PropCandidate> pool, List<RectInt> own) => placement.Selection switch
        {
            PropSelection.Spread => SelectSpread(placement, pool, own),
            PropSelection.Cover  => SelectCover(placement, pool, own),
            PropSelection.Cluster => SelectCluster(placement, pool, own),
            PropSelection.Line   => SelectLine(placement, pool, own),
            _                    => SelectRandom(placement, pool, own),
        };

        // The pool is shuffled, so its last usable entry is a random one.
        private int SelectRandom(RoomPropPlacement placement, List<PropCandidate> pool, List<RectInt> own)
        {
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (!_rules.Placeable(pool[i], placement, own)) pool.RemoveAt(i);
                else if (_rules.Eligible(pool[i], placement)) return i;
            }
            return -1;
        }

        // Mitchell's best candidate: of a few random candidates, the one farthest from every prop
        // already placed — an approximation of blue noise that is cheap and never clumps.
        private int SelectSpread(RoomPropPlacement placement, List<PropCandidate> pool, List<RectInt> own)
        {
            int best = -1;
            float bestDistance = -1f;
            int seen = 0;
            for (int i = pool.Count - 1; i >= 0 && seen < SpreadSamples; i--)
            {
                if (!_rules.Placeable(pool[i], placement, own))
                {
                    pool.RemoveAt(i);
                    if (best > i) best--;
                    continue;
                }
                if (!_rules.Eligible(pool[i], placement)) continue;

                seen++;
                float distance = DistanceToPlaced(pool[i].Rect.center, placement, own);
                if (distance <= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }
            return best;
        }

        // Of a few random candidates, the one nearest what this placement has already put down, so
        // props gather in patches; the odd one starts a new patch elsewhere.
        private int SelectCluster(RoomPropPlacement placement, List<PropCandidate> pool, List<RectInt> own)
        {
            if (own.Count == 0 || _random.Chance(NewPatchChance)) return SelectRandom(placement, pool, own);

            int best = -1;
            float bestDistance = float.MaxValue;
            int seen = 0;
            for (int i = pool.Count - 1; i >= 0 && seen < ClusterSamples; i--)
            {
                if (!_rules.Placeable(pool[i], placement, own))
                {
                    pool.RemoveAt(i);
                    if (best > i) best--;
                    continue;
                }
                if (!_rules.Eligible(pool[i], placement)) continue;

                seen++;
                float distance = DistanceToOwn(pool[i].Rect.center, own);
                if (distance >= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }
            return best;
        }

        // The candidate that carries on the row already started: the nearest to what is placed,
        // counting a step across the room as dearer than one along it. The first one goes anywhere.
        private int SelectLine(RoomPropPlacement placement, List<PropCandidate> pool, List<RectInt> own)
        {
            if (own.Count == 0) return SelectRandom(placement, pool, own);

            pool.RemoveAll(candidate => !_rules.Placeable(candidate, placement, own));

            int best = -1;
            float bestCost = float.MaxValue;
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (!_rules.Eligible(pool[i], placement)) continue;

                float cost = LineCost(pool[i].Rect.center, own);
                if (cost >= bestCost) continue;
                best = i;
                bestCost = cost;
            }
            return best;
        }

        private float LineCost(Vector2 point, List<RectInt> own)
        {
            bool wide = _bounds.width >= _bounds.height;
            float nearest = float.MaxValue;
            foreach (RectInt rect in own)
            {
                Vector2 apart = rect.center - point;
                float along  = Mathf.Abs(wide ? apart.x : apart.y);
                float across = Mathf.Abs(wide ? apart.y : apart.x);
                nearest = Mathf.Min(nearest, along + LineAcrossWeight * across);
            }
            return nearest;
        }

        // The candidate on the longest open line of sight, so each piece cuts the room's worst
        // firing lane; stops when nothing is left to break.
        private int SelectCover(RoomPropPlacement placement, List<PropCandidate> pool, List<RectInt> own)
        {
            pool.RemoveAll(candidate => !_rules.Placeable(candidate, placement, own));

            int best = -1;
            int bestRun = CoverMinRun - 1;
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (!_rules.Eligible(pool[i], placement)) continue;

                int run = _sight.LongestRun(pool[i].Rect.min);
                if (run <= bestRun) continue;
                best = i;
                bestRun = run;
            }
            return best;
        }

        // How far a point is from the nearest prop: of everything in the room for furniture, of this
        // placement's own for decorations (which aren't in the room's list).
        private float DistanceToPlaced(Vector2 point, RoomPropPlacement placement, List<RectInt> own)
        {
            if (placement.Decor) return DistanceToOwn(point, own);

            float nearest = float.MaxValue;
            foreach (Vector2 centre in _centres)
                nearest = Mathf.Min(nearest, (centre - point).magnitude);
            return nearest;
        }

        private static float DistanceToOwn(Vector2 point, List<RectInt> own)
        {
            float nearest = float.MaxValue;
            foreach (RectInt rect in own)
                nearest = Mathf.Min(nearest, (rect.center - point).magnitude);
            return nearest;
        }
    }
}
