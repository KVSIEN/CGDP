using UnityEngine;

namespace CGD.Algorithms
{
    // What WorleyNoise finds at a point: the distances to its nearest and second-nearest feature
    // points, and which cell the nearest one belongs to.
    public readonly struct WorleySample
    {
        public WorleySample(float nearest, float secondNearest, Vector2Int cell, float cellValue)
        {
            Nearest       = nearest;
            SecondNearest = secondNearest;
            Cell          = cell;
            CellValue     = cellValue;
        }

        // Distance to the nearest feature point ("F1"): 0 at the point, growing toward the cell's
        // edge. Gives round blobs: stones, scales, bubbles, craters.
        public float Nearest { get; }

        // Distance to the second-nearest ("F2").
        public float SecondNearest { get; }

        // The grid cell whose feature point is nearest: every point of one Voronoi cell shares it.
        public Vector2Int Cell { get; }

        // A random 0..1 value belonging to that cell: give each cell its own colour, height or kind.
        public float CellValue { get; }

        // F2 - F1: 0 exactly on the border between two cells, growing toward a cell's middle.
        // Thresholded low, it draws a network of cracks, veins, cobble mortar or dried mud.
        public float EdgeDistance => SecondNearest - Nearest;
    }
}
