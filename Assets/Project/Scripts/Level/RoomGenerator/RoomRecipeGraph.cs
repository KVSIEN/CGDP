using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // Turns a RoomRecipe into a map graph of one room: the room, a small Junction on each side
    // that should have a doorway, and a Start past the first so the room is entered from there.
    // Node positions are in cells (lay it out with a node spacing of one). The room is node
    // RoomId; the Room Lab and the room generator scene both build their rooms this way.
    public static class RoomRecipeGraph
    {
        public const int RoomId = 0;

        // Neighbours stand this many cells away: far enough that a two-cell room and a
        // two-cell neighbour never claim the same cell.
        private const int NeighbourCells = 3;

        // The recipe's room at the origin and one Junction per doorway side; the corridor to
        // each comes out of the wall facing it.
        public static MapGraph Build(RoomRecipe recipe)
        {
            var graph = new MapGraph();
            MapNode room = graph.AddNode(recipe.Type, Vector2.zero);
            room.Place         = recipe.Place;
            room.Layout        = recipe.Layout;
            room.Category      = recipe.Category;
            room.Tier          = recipe.Tier;
            room.Intensity     = recipe.Intensity;
            room.SetFaction(recipe.Faction, recipe.Influence);
            room.BreachFaction = recipe.BreachFaction;
            room.Section       = recipe.Section;

            // The editor's y grows downward.
            if (recipe.DoorNorth) AddNeighbour(graph, room, new Vector2(0f, -NeighbourCells));
            if (recipe.DoorEast)  AddNeighbour(graph, room, new Vector2(NeighbourCells, 0f));
            if (recipe.DoorSouth) AddNeighbour(graph, room, new Vector2(0f, NeighbourCells));
            if (recipe.DoorWest)  AddNeighbour(graph, room, new Vector2(-NeighbourCells, 0f));
            AddStartBeyondFirst(graph, room);
            return graph;
        }

        // A Start past the first neighbour, so the room is entered from that side as in a map:
        // its doorway there leads back toward Start (the room's way in, RoomFlowPlanner) and the
        // others lead onward.
        private static void AddStartBeyondFirst(MapGraph graph, MapNode room)
        {
            if (graph.Nodes.Count < 2) return;
            MapNode first = graph.Nodes[1];
            MapNode start = graph.AddNode(MapNodeType.Start, first.Position + (first.Position - room.Position));
            graph.Connect(first.Id, start.Id);
        }

        // A Breach room's first neighbour belongs to the breaching faction, so the breach
        // comes in through that doorway as it would from the faction's territory.
        private static void AddNeighbour(MapGraph graph, MapNode room, Vector2 position)
        {
            bool first = graph.Nodes.Count == 1;
            MapNode neighbour = graph.AddNode(MapNodeType.Junction, position);
            if (first && room.HasBreachFaction) neighbour.SetFaction(room.BreachFaction, 1f);
            graph.Connect(room.Id, neighbour.Id);
        }
    }
}
