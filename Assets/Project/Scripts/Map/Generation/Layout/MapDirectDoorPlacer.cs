using CGD.Core;

namespace CGD.Map
{
    // Turns links between neighbouring grid cells into direct doors: the level builder then
    // puts the two rooms side by side with a doorway between, instead of a hallway. Gates and
    // one-way links included (their door stands in that doorway), so it runs after them.
    // Draws nothing when the chance is 0.
    internal class MapDirectDoorPlacer
    {
        private readonly MapGenerationContext _context;
        private readonly RandomStream         _random;

        public MapDirectDoorPlacer(MapGenerationContext context, RandomStream random)
        {
            _context = context;
            _random  = random;
        }

        public void Place()
        {
            float chance = _context.Layout.DirectDoorChance;
            if (chance <= 0f) return;

            foreach (MapConnection connection in _context.Graph.Connections)
            {
                if (!AreNeighbours(connection)) continue;
                connection.Direct = _random.Chance(chance);
            }
        }

        private bool AreNeighbours(MapConnection connection) =>
            _context.Grid.Contains(connection.A) && _context.Grid.Contains(connection.B)
            && _context.Grid.Distance(connection.A, connection.B) == 1;
    }
}
