using UnityEngine;

namespace CGD.Level
{
    // How a furnished room reads by what can be seen from everywhere in it, in every direction
    // (RoomVisibility), for room tools to compare rooms and tune the trait bands by:
    //   - Exposure: how much of the open floor a typical tile sees, 0–1. A hall read at a glance
    //     is near 1; a maze of corridors, or a room full of pillars, is low.
    //   - Enclosed: the share of open tiles that see no more than a quarter of it: pockets and
    //     corners that hide whoever stands there.
    //   - Sightline: the longest line of sight in the room, in tiles, along any direction (the
    //     trait measure's lane only looks along rows and columns).
    // A room with no open floor reads as zero on all three.
    public readonly struct RoomSightReading
    {
        // A tile that sees no more than this share of the open floor is shut in.
        public const float EnclosedShare = 0.25f;

        public RoomSightReading(float exposure, float enclosed, float sightline)
        {
            Exposure  = exposure;
            Enclosed  = enclosed;
            Sightline = sightline;
        }

        public float Exposure  { get; }
        public float Enclosed  { get; }
        public float Sightline { get; }

        public static RoomSightReading Of(RoomStructure structure) => Of(structure, new RoomVisibility(structure));

        // With the visibility already kept current for this structure.
        internal static RoomSightReading Of(RoomStructure structure, RoomVisibility visibility)
        {
            int open = 0;
            long seen = 0;
            float sightline = 0f;
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                int area = visibility.Area(tile);
                if (area == 0) continue;

                open++;
                seen += area;
                sightline = Mathf.Max(sightline, visibility.Reach(tile));
            }
            if (open == 0) return default;

            int enclosed = 0;
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                int area = visibility.Area(tile);
                if (area > 0 && area <= EnclosedShare * open) enclosed++;
            }
            return new RoomSightReading(seen / ((float)open * open), enclosed / (float)open, sightline);
        }
    }
}
