using UnityEngine;

namespace CGD.Algorithms
{
    // Worley noise, also called cellular or Voronoi noise (Steven Worley, 1996).
    //
    // How it works: every grid cell holds one random "feature point" somewhere inside it. For any
    // point on the plane, find the feature points nearest to it (they can only be in its own cell
    // or the 8 around it) and measure the distances. The nearest distance makes round blobs
    // around each feature point; the gap between the nearest and second-nearest is 0 on the
    // borders where two points are equally near, which traces the cells' outlines (a Voronoi
    // diagram). See WorleySample for what each value gives.
    //
    // Why it's useful: anything made of cells. Cracked floors and walls, cobblestones, scales,
    // cell membranes and fleshy growth (BIO), crystal clusters, a city split into districts, a
    // territory map where each cell belongs to one faction (CellValue picks which), or spots to
    // scatter one thing each (the feature points themselves).
    public sealed class WorleyNoise : INoise2D
    {
        private readonly uint _seed;
        private readonly float _jitter;

        // `jitter` 0..1: how far each feature point may stray from its cell's middle. 0 gives a
        // regular grid of identical cells, 1 the fully random, organic look.
        public WorleyNoise(int seed, float jitter = 1f)
        {
            _seed   = (uint)seed;
            _jitter = Mathf.Clamp01(jitter);
        }

        // INoise2D: the distance to the nearest feature point, as -1 (on it) to 1 (far from it).
        public float Sample(float x, float y) => Mathf.Clamp01(Evaluate(x, y).Nearest) * 2f - 1f;

        public WorleySample Evaluate(float x, float y)
        {
            var point = new Vector2(x, y);
            int cellX = Mathf.FloorToInt(x), cellY = Mathf.FloorToInt(y);

            float nearest = float.MaxValue, secondNearest = float.MaxValue;
            Vector2Int nearestCell = default;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var cell = new Vector2Int(cellX + dx, cellY + dy);
                    float distance = Vector2.Distance(point, FeaturePoint(cell));
                    if (distance < nearest)
                    {
                        secondNearest = nearest;
                        nearest       = distance;
                        nearestCell   = cell;
                    }
                    else if (distance < secondNearest)
                    {
                        secondNearest = distance;
                    }
                }

            return new WorleySample(nearest, secondNearest, nearestCell, CoordinateHash.Value01(nearestCell.x, nearestCell.y, _seed + 2));
        }

        // The cell's feature point: its middle, pushed a random amount each way by the jitter.
        public Vector2 FeaturePoint(Vector2Int cell)
        {
            float offsetX = (CoordinateHash.Value01(cell.x, cell.y, _seed)     - 0.5f) * _jitter;
            float offsetY = (CoordinateHash.Value01(cell.x, cell.y, _seed + 1) - 0.5f) * _jitter;
            return new Vector2(cell.x + 0.5f + offsetX, cell.y + 0.5f + offsetY);
        }
    }
}
