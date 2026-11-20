using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // One hunt for a lost target, on an occupancy map (OccupancyMap) kept up to date as time
    // passes. A squad's members share one (EnemySquad.SharedSearch): what any of them sees
    // clears the map for all, and each claims the spot it heads for, so they fan out to
    // different places instead of checking the same corner. A lone enemy keeps its own.
    internal sealed class OccupancySearch
    {
        public const float CellSize = 1.5f;
        // The map reaches this many times the profile's search radius round the last sighting.
        public const float RadiusScale = 2f;
        // Spreading: a step this often, each passing this share of a cell's chance on — about
        // the pace of someone creeping away, not sprinting, so a search stays local.
        public const float StepSeconds = 0.6f;
        public const float SpreadRate  = 0.3f;
        // Below this much chance left round here, the target has got away.
        public const float GiveUpBelow = 0.12f;
        // A fresh lead this far from the last one starts the map over.
        private const float ReseedDistance = 3f;
        // Picking where to look next: a cell this far off counts half as much as one at hand;
        // cells this close are already in view; another searcher's spot keeps others this far.
        private const float DistanceScale = 6f;
        private const float InView        = 1.5f;
        private const float ClaimRadius   = 3f;

        private readonly OccupancyMap _map;
        private readonly Dictionary<object, Vector3> _claims = new();
        private readonly System.Predicate<int> _claimedByOther;
        private object _picking;
        private float _steppedTo;

        public OccupancySearch(OccupancyGrid grid, Vector3 lost, Vector3 heading, float now)
        {
            _map = new OccupancyMap(grid);
            _claimedByOther = ClaimedByOther;
            Reseed(lost, heading, now);
        }

        // Laid over the NavMesh round `lost`.
        public static OccupancySearch Start(Vector3 lost, Vector3 heading, float searchRadius, float now) =>
            new(OccupancyGridBuilder.Around(lost, searchRadius * RadiusScale, CellSize), lost, heading, now);

        public OccupancyMap Map => _map;
        public Vector3 SeededAt { get; private set; }
        public bool IsValid { get; private set; }
        // Nothing likely left to look at: the target has slipped away.
        public bool IsExhausted => !IsValid || _map.Remaining < GiveUpBelow;

        // Whether a lead at `lost` is this same hunt (on the map, near where it started).
        public bool Covers(Vector3 lost) => IsValid && _map.Grid.TryCell(lost, out int cell) && _map.Grid.Walkable(cell);

        // A new lead: start over there, unless it is close to where this one did.
        public void Follow(Vector3 lost, Vector3 heading, float now)
        {
            if (Vector3.Distance(lost, SeededAt) > ReseedDistance) Reseed(lost, heading, now);
        }

        // Spreads the chance for the time that has passed; any member may call it, the steps
        // are counted on the clock so they are never done twice.
        public void Advance(float now)
        {
            while (_steppedTo + StepSeconds <= now)
            {
                _map.Diffuse(SpreadRate);
                _steppedTo += StepSeconds;
            }
        }

        // Where `searcher` should look next from `from`, kept clear of the spots the others are
        // heading for while anything else is left; false when nothing likely is left at all.
        public bool TryNext(object searcher, Vector3 from, out Vector3 spot)
        {
            spot = default;
            if (IsExhausted) return false;

            _picking = searcher;
            bool found = _map.TryBest(from, DistanceScale, InView, out int cell, _claimedByOther)
                      || _map.TryBest(from, DistanceScale, InView, out cell);
            _picking = null;
            if (!found) return false;

            spot = _map.Grid.Center(cell);
            _claims[searcher] = spot;
            return true;
        }

        // The searcher has stopped (found the target, given up, been called away).
        public void Release(object searcher) => _claims.Remove(searcher);

        private bool ClaimedByOther(int cell)
        {
            Vector3 center = _map.Grid.Center(cell);
            foreach (KeyValuePair<object, Vector3> claim in _claims)
                if (!ReferenceEquals(claim.Key, _picking) && Vector3.Distance(claim.Value, center) < ClaimRadius) return true;
            return false;
        }

        private void Reseed(Vector3 lost, Vector3 heading, float now)
        {
            SeededAt   = lost;
            IsValid    = _map.Seed(lost, heading);
            _steppedTo = now;
        }
    }
}
