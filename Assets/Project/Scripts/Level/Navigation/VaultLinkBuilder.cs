using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CGD.Level
{
    // Lets enemies hop a generated level's low cover (EnemyVault does the hop): once the NavMesh
    // is built, links go across the low inner walls (VaultLinkPlanner) and across low, thin
    // furniture (barriers, counters), measured from what was placed, and one way down off open
    // ledges where the floor changes level (DropLinkPlanner). A link is only kept where
    // both ends are on the NavMesh and it saves a real detour — over a divider rather than round
    // it — so enemies still walk round a lone crate.
    public sealed class VaultLinkBuilder
    {
        // Furniture no taller than low cover, no deeper than this, can be hopped.
        public const float MaxPropHeight = CoverHeights.LowMax + 0.15f;
        public const float MaxPropDepth  = 1.2f;
        // Kept only when walking round is at least this many times the hop.
        public const float MinDetour = 2.5f;

        private const float EndClearance = 0.5f;
        private const float EndSearch    = 0.5f;
        private const float FloorSlack   = 0.3f;

        private readonly NavMeshPath _path = new();

        public VaultLinks Build(LevelLayout layout, Transform level, IEnumerable<GameObject> furniture, int agentTypeId)
        {
            var holder = level.gameObject.AddComponent<VaultLinks>();
            foreach (VaultLink link in VaultLinkPlanner.Plan(layout))
                TryAdd(holder, level.TransformPoint(link.From), level.TransformPoint(link.To), link.Width, agentTypeId);
            foreach (VaultLink link in DropLinkPlanner.Plan(layout))
                TryAdd(holder, level.TransformPoint(link.From), level.TransformPoint(link.To), link.Width, agentTypeId, link.OneWay);
            foreach (GameObject piece in furniture)
                if (TryAcross(piece, level.position.y, out Vector3 from, out Vector3 to, out float width))
                    TryAdd(holder, from, to, width, agentTypeId);
            return holder;
        }

        // Low and thin enough to hop, standing on the floor: a link across its thin side.
        private static bool TryAcross(GameObject piece, float floor, out Vector3 from, out Vector3 to, out float width)
        {
            from = to = default;
            width = 0f;
            if (piece == null || !TryBounds(piece, out Bounds bounds)) return false;
            if (bounds.size.y > MaxPropHeight || bounds.min.y > floor + FloorSlack) return false;

            bool thinAlongX = bounds.size.x <= bounds.size.z;
            float depth = thinAlongX ? bounds.size.x : bounds.size.z;
            if (depth > MaxPropDepth) return false;

            Vector3 axis = thinAlongX ? Vector3.right : Vector3.forward;
            var middle = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            from  = middle - axis * (depth * 0.5f + EndClearance);
            to    = middle + axis * (depth * 0.5f + EndClearance);
            width = (thinAlongX ? bounds.size.z : bounds.size.x) * VaultLinkPlanner.WidthShare;
            return true;
        }

        private static bool TryBounds(GameObject piece, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (Collider collider in piece.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger) continue;
                if (any) bounds.Encapsulate(collider.bounds);
                else bounds = collider.bounds;
                any = true;
            }
            return any;
        }

        private void TryAdd(VaultLinks holder, Vector3 from, Vector3 to, float width, int agentTypeId, bool oneWay = false)
        {
            if (!NavMesh.SamplePosition(from, out NavMeshHit start, EndSearch, NavMesh.AllAreas)) return;
            if (!NavMesh.SamplePosition(to, out NavMeshHit end, EndSearch, NavMesh.AllAreas)) return;
            if (!SavesDetour(start.position, end.position)) return;

            var data = new NavMeshLinkData
            {
                startPosition = start.position,
                endPosition   = end.position,
                width         = width,
                bidirectional = !oneWay,
                costModifier  = -1f,
                area          = 0,
                agentTypeID   = agentTypeId,
            };
            NavMeshLinkInstance link = NavMesh.AddLink(data);
            if (link.valid) holder.Add(link);
        }

        // Unreachable on foot, or a walk round far longer than the hop.
        private bool SavesDetour(Vector3 from, Vector3 to)
        {
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, _path) || _path.status != NavMeshPathStatus.PathComplete) return true;
            return PathLength(_path) >= Vector3.Distance(from, to) * MinDetour;
        }

        private static float PathLength(NavMeshPath path)
        {
            Vector3[] corners = path.corners;
            float length = 0f;
            for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
            return length;
        }
    }
}
