using UnityEngine;

namespace CGD.Level
{
    // All the level's walls as boxes, one per LevelWallPlan segment (tile-edge runs, room
    // outlines, chamfered corridor bends), standing just outside the floor. Doorways into
    // rooms taller than the corridor get a lintel to close the gap above.
    public class LevelWallBuilder
    {
        private readonly LevelBuildSettings _settings;
        private readonly LevelLayout _layout;
        private readonly BoxMeshBuilder _walls;

        public LevelWallBuilder(LevelBuildSettings settings, LevelLayout layout, BoxMeshBuilder walls)
        {
            _settings = settings;
            _layout   = layout;
            _walls    = walls;
        }

        private float Tile      => _layout.TileSize;
        private float Thickness => _settings.WallThickness;

        public void Build(LevelWallPlan plan)
        {
            foreach (LevelWallPlan.Segment segment in plan.Segments)
                AddSegment(segment);
            AddLintels();
        }

        // The floor is on the segment's left; the wall sits just outside the line.
        private void AddSegment(LevelWallPlan.Segment segment)
        {
            Vector3 a = new Vector3(segment.From.x, 0f, segment.From.y) * Tile;
            Vector3 b = new Vector3(segment.To.x, 0f, segment.To.y) * Tile;
            Vector3 along = b - a;
            float length = along.magnitude;
            if (length < 0.001f) return;

            Vector3 dir = along / length;
            Vector3 outward = new(dir.z, 0f, -dir.x);
            Vector3 position = (a + b) * 0.5f + outward * (Thickness * 0.5f) + Vector3.up * (segment.Height * 0.5f);
            float overlap = segment.SquareEnds ? Thickness * 2f : Thickness;
            _walls.AddBox(position, new Vector3(Thickness, segment.Height, length + overlap), Quaternion.LookRotation(dir));
        }

        // Room walls stand taller than the corridor's; above each doorway the wall carries on.
        private void AddLintels()
        {
            float corridorHeight = _settings.WallHeight;
            foreach (LevelDoorway doorway in _layout.Doorways)
            {
                float extra = doorway.Room.WallHeight - corridorHeight;
                if (extra <= 0.01f) continue;

                Vector2Int dir = doorway.Outward;
                Vector3 outward = new(dir.x, 0f, dir.y);
                Vector3 position = _layout.TileToLocal(doorway.RoomTile) + outward * ((Tile + Thickness) * 0.5f)
                                 + Vector3.up * (corridorHeight + extra * 0.5f);
                Vector3 size = dir.x != 0 ? new Vector3(Thickness, extra, Tile) : new Vector3(Tile, extra, Thickness);
                _walls.AddBox(position, size);
            }
        }
    }
}
