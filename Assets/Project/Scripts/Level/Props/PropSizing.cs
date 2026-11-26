using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Which size of a prop a placement puts down (PropSizeVariants), and what that size asks of
    // the room: where a group's members stand round it, and floor beyond the placement's own.
    internal static class PropSizing
    {
        // A tile is 3 m in the placeholder kit.
        private const float TileMetres = 3f;
        // A group's member this far or farther from the middle along an axis, in tiles, stands off
        // to that side, so another size of the group's piece moves it that way.
        private const float SideOffset = 0.2f;

        // For a placement with a prop that comes in several sizes, the prefab to stand on a
        // footprint of `size`: one of its prefabs or another size of it. Null otherwise, drawing
        // nothing. A stretched prefab keeps its own size. A cover spot never gets a size lower
        // than the listed one, which would hide nobody. A group never gets a size that would push
        // one of its pieces off the footprint, or one that needs more floor.
        public static GameObject Roll(RoomPropPlacement placement, Vector2Int size, float roomHeight, RandomStream random)
        {
            if (placement.Stretch || !HasSizes(placement)) return null;

            GameObject prefab = random.Pick(placement.Prefabs);
            if (prefab == null || !prefab.TryGetComponent(out PropSizeVariants sizes)) return prefab;

            float minHeight = placement.Selection == PropSelection.Cover ? PropHeights.Of(prefab) : 0f;
            return sizes.Pick(random, roomHeight, minHeight,
                              placement.HasMembers ? variant => MembersFit(placement, variant, size) : null);
        }

        // Whether `prefab` is another size of one of the placement's props rather than one it lists.
        public static bool IsVariant(RoomPropPlacement placement, GameObject prefab) =>
            prefab != null && Array.IndexOf(placement.Prefabs, prefab) < 0;

        // The listed prop `size` is another size of; `size` itself when it is listed.
        public static GameObject ListedFor(RoomPropPlacement placement, GameObject size) =>
            TryFind(placement, size, out GameObject listed, out _) ? listed : size;

        // The footprint the prefab needs on a footprint of `size`: larger where a size asks for it.
        public static Vector2Int FootprintFor(RoomPropPlacement placement, GameObject prefab, Vector2Int size) =>
            TryFind(placement, prefab, out _, out PropSizeVariant variant) ? Vector2Int.Max(size, variant.Footprint) : size;

        // The spots of footprint `size` (across, front to back) that cover a candidate's tiles,
        // turned as it is.
        public static IEnumerable<PropCandidate> Covering(PropCandidate candidate, Vector2Int size)
        {
            Vector2Int world = candidate.Rotation % 2 == 0 ? size : new Vector2Int(size.y, size.x);
            Vector2Int extra = world - candidate.Rect.size;
            for (int y = 0; y <= extra.y; y++)
                for (int x = 0; x <= extra.x; x++)
                    yield return new PropCandidate(new RectInt(candidate.Rect.min - new Vector2Int(x, y), world), candidate.Rotation);
        }

        // How far a group's members move, in metres per side, for the prefab it stands round.
        public static Vector2 MemberShift(RoomPropPlacement placement, GameObject prefab) =>
            TryFind(placement, prefab, out _, out PropSizeVariant variant) ? variant.MemberShift : Vector2.zero;

        // A member's offset, in tiles, round another size of the group's piece: moved out (or in)
        // on each side it stands off to, by the shift in metres.
        public static Vector2 Shifted(Vector2 offset, Vector2 shift) =>
            new(Shifted(offset.x, shift.x), Shifted(offset.y, shift.y));

        private static float Shifted(float offset, float metres) =>
            Mathf.Abs(offset) < SideOffset ? offset : Mathf.Sign(offset) * Mathf.Max(0f, Mathf.Abs(offset) + metres / TileMetres);

        private static bool HasSizes(RoomPropPlacement placement)
        {
            if (placement.Prefabs == null) return false;
            foreach (GameObject prefab in placement.Prefabs)
                if (prefab != null && prefab.TryGetComponent(out PropSizeVariants _)) return true;
            return false;
        }

        // Finds `size` among the other sizes of the placement's props.
        private static bool TryFind(RoomPropPlacement placement, GameObject size, out GameObject listed, out PropSizeVariant found)
        {
            listed = null;
            found = default;
            if (size == null || placement.Prefabs == null || !IsVariant(placement, size)) return false;

            foreach (GameObject prefab in placement.Prefabs)
            {
                if (prefab == null || !prefab.TryGetComponent(out PropSizeVariants sizes)) continue;
                foreach (PropSizeVariant variant in sizes.Variants)
                    if (variant.Prefab == size)
                    {
                        listed = prefab;
                        found = variant;
                        return true;
                    }
            }
            return false;
        }

        // Whether the footprint is enough for the variant, and every member its shift pushes
        // outward still stands on it.
        private static bool MembersFit(RoomPropPlacement placement, PropSizeVariant variant, Vector2Int size)
        {
            if (variant.Footprint.x > size.x || variant.Footprint.y > size.y) return false;

            foreach (PropMember member in placement.Members)
            {
                if (member == null) continue;
                Vector2 moved = Shifted(member.Offset, variant.MemberShift);
                if (PushedOff(member.Offset.x, moved.x, size.x) || PushedOff(member.Offset.y, moved.y, size.y)) return false;
            }
            return true;
        }

        private static bool PushedOff(float from, float to, int tiles) =>
            Mathf.Abs(to) > Mathf.Abs(from) && Mathf.Abs(to) > tiles * 0.5f;
    }
}
