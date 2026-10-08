using CGD.Core;

namespace CGD.Map
{
    // Turns some plain links between neighbouring grid cells into direct doors: the level
    // builder then puts the two rooms side by side with a doorway between, instead of a
    // hallway. Runs after the gates, so a gate, shortcut or one-way link always keeps its
    // hallway (and its door), and draws nothing when the chance is 0 so existing seeds
    // are unchanged.
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
                if (connection.Type != ConnectionType.Normal || connection.OneWay) continue;
                if (!AreNeighbours(connection)) continue;
                connection.Direct = _random.Chance(chance);
            }
        }

        private bool AreNeighbours(MapConnection connection) =>
            _context.Grid.Contains(connection.A) && _context.Grid.Contains(connection.B)
            && _context.Grid.Distance(connection.A, connection.B) == 1;
    }
}
