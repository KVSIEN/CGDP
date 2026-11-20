using UnityEngine;
using CGD.Enemies;

namespace CGD.Level
{
    // Makes a placed piece of furniture something idle enemies use (RoomPropPlacement.Activity):
    // a PointOfInterest of the activity's kind on it, with a place to stand for each user in a
    // row along its front (+Z), just clear of it. The kind decides who comes and for how long;
    // they face the piece while they are there.
    public static class PropActivity
    {
        // Metres between the piece's front face and where a user stands, and between users.
        private const float StandOff = 0.6f;
        private const float MaxSpacing = 0.9f;

        public static void Attach(GameObject placed, PlannedProp prop, float tileSize)
        {
            InterestKind kind = prop.Activity;
            if (placed == null || kind == null) return;

            // The footprint's sides in the piece's own frame: an odd quarter turn swaps them.
            bool turned = Mathf.RoundToInt(Mathf.Repeat(prop.Yaw, 180f) / 90f) % 2 == 1;
            float across = (turned ? prop.Tiles.height : prop.Tiles.width) * tileSize;
            float depth  = (turned ? prop.Tiles.width : prop.Tiles.height) * tileSize;
            if (prop.Tiles.width == 0) across = depth = tileSize;

            Vector3[] local = StandSpots(kind.Capacity, across, depth);
            var spots = new Transform[local.Length];
            Transform piece = placed.transform;
            for (int i = 0; i < local.Length; i++)
            {
                var spot = new GameObject($"{kind.DisplayName} spot {i + 1}").transform;
                spot.SetParent(piece, false);
                spot.position = piece.position + piece.rotation * local[i];
                spots[i] = spot;
            }
            placed.AddComponent<PointOfInterest>().Configure(kind, spots);
        }

        // Spots in the piece's own frame, unscaled: `count` side by side in front of a piece
        // `across` wide and `depth` deep, centred on it.
        internal static Vector3[] StandSpots(int count, float across, float depth)
        {
            count = Mathf.Max(1, count);
            float spacing = Mathf.Min(MaxSpacing, across / count);
            var spots = new Vector3[count];
            for (int i = 0; i < count; i++)
                spots[i] = new Vector3((i - (count - 1) * 0.5f) * spacing, 0f, depth * 0.5f + StandOff);
            return spots;
        }
    }
}
