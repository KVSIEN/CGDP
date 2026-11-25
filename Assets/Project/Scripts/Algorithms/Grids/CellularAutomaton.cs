using UnityEngine;
using CGD.Core;

namespace CGD.Algorithms
{
    // A cellular automaton: a grid of cells, each alive or dead, updated in rounds by one local
    // rule. Conway's Game of Life is the famous one; this is the "cave" rule used in level
    // generation.
    //
    // How it works: start from random noise (RandomFill: each cell alive by chance). Each round
    // (Step), every cell counts its 8 living neighbours; a dead cell comes alive with at least
    // `birthLimit` of them, a living cell survives with at least `survivalLimit`. Every cell is
    // decided from the previous round's grid, all at once. Lonely cells die and crowded gaps fill
    // in, so after 4 or 5 rounds the noise has settled into smooth, natural blobs.
    //
    // Why it's useful: organic shapes from a few lines of code. Read "alive" as rock and you get
    // caves; as growth and you get patches of moss, fungus or creep spreading over a floor; as
    // water and you get lakes. Use a higher fill chance for tighter caves, more rounds for
    // smoother edges. Follow with GridDistanceMap or DisjointSet to find and join (or drop)
    // pockets that ended up cut off.
    public static class CellularAutomaton
    {
        // A width × height grid, each cell alive with `aliveChance` (0..1). Seeded, so repeatable.
        public static bool[,] RandomFill(int width, int height, float aliveChance, RandomStream random)
        {
            var cells = new bool[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    cells[x, y] = random.Chance(aliveChance);
            return cells;
        }

        // One round. `outsideIsAlive`: whether cells past the edge count as living neighbours
        // (true walls a cave in; false lets growth thin out at the edges).
        public static bool[,] Step(bool[,] cells, int birthLimit = 5, int survivalLimit = 4, bool outsideIsAlive = true)
        {
            int width = cells.GetLength(0), height = cells.GetLength(1);
            var next = new bool[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    int living = LivingNeighbours(cells, x, y, outsideIsAlive);
                    next[x, y] = cells[x, y] ? living >= survivalLimit : living >= birthLimit;
                }
            return next;
        }

        // Several rounds.
        public static bool[,] Run(bool[,] cells, int rounds, int birthLimit = 5, int survivalLimit = 4, bool outsideIsAlive = true)
        {
            for (int round = 0; round < rounds; round++) cells = Step(cells, birthLimit, survivalLimit, outsideIsAlive);
            return cells;
        }

        public static int LivingNeighbours(bool[,] cells, int x, int y, bool outsideIsAlive)
        {
            int width = cells.GetLength(0), height = cells.GetLength(1), living = 0;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    bool inside = nx >= 0 && ny >= 0 && nx < width && ny < height;
                    if (inside ? cells[nx, ny] : outsideIsAlive) living++;
                }
            return living;
        }
    }
}
