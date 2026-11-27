using System;
using UnityEngine;
using CGD.Core;
using CGD.Map;

namespace CGD.Level
{
    // Turns a RoomRecipe into a map graph: the room, and a neighbour on each side that should
    // have a doorway. A neighbour joins only to the room, so the room's doorway is the only
    // one it has and nothing builds on past it. Node positions are in cells (lay it out with a
    // node spacing of one) and every link is a direct door. The room is node RoomId; the Room Lab and the room generator scene
    // both build their rooms this way.
    //
    // Given a random stream (the room generator) the neighbours are rooms of their own to look
    // at, held by the room's faction: the first is the Start the room is entered from (as in a
    // map: its doorway there leads back toward Start, the room's way in, RoomFlowPlanner), the
    // others are rolled rooms. Without one (the Room Lab) they are small Junctions and a Start
    // past the first.
    public static class RoomRecipeGraph
    {
        public const int RoomId = 0;

        // Neighbours are placed this many cells away before they are pulled in: far enough that a two-cell room and a
        // two-cell neighbour never claim the same cell.
        private const int NeighbourCells = 3;

        // Rolled neighbours are rooms that play as themselves and need no second faction,
        // which only the room itself is given.
        private static readonly MapNodeType[] NeighbourTypes = Array.FindAll(
            RoomChoice.RollableTypes, type => !type.HasSecondFaction());

        // The recipe's room at the origin and one neighbour per doorway side; the corridor to
        // each is on the wall facing it.
        public static MapGraph Build(RoomRecipe recipe, RandomStream neighbourRolls = null)
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
            if (recipe.DoorNorth) AddNeighbour(graph, room, recipe, new Vector2(0f, -NeighbourCells), neighbourRolls);
            if (recipe.DoorEast)  AddNeighbour(graph, room, recipe, new Vector2(NeighbourCells, 0f), neighbourRolls);
            if (recipe.DoorSouth) AddNeighbour(graph, room, recipe, new Vector2(0f, NeighbourCells), neighbourRolls);
            if (recipe.DoorWest)  AddNeighbour(graph, room, recipe, new Vector2(-NeighbourCells, 0f), neighbourRolls);
            if (neighbourRolls == null) AddStartBeyondFirst(graph, room);
            return graph;
        }

        // A Start past the first neighbour, so the room is entered from that side.
        private static void AddStartBeyondFirst(MapGraph graph, MapNode room)
        {
            if (graph.Nodes.Count < 2) return;
            MapNode first = graph.Nodes[1];
            MapNode start = graph.AddNode(MapNodeType.Start, first.Position + (first.Position - room.Position));
            ConnectDirect(graph, first.Id, start.Id);
        }

        // A Breach room's first neighbour belongs to the breaching faction, so the breach
        // comes in through that doorway as it would from the faction's territory.
        private static void AddNeighbour(MapGraph graph, MapNode room, RoomRecipe recipe, Vector2 position, RandomStream rolls)
        {
            bool first = graph.Nodes.Count == 1;
            MapNodeType type = rolls == null ? MapNodeType.Junction
                             : first ? MapNodeType.Start
                             : rolls.Pick(NeighbourTypes);
            MapNode neighbour = graph.AddNode(type, position);
            if (rolls != null)
            {
                neighbour.Tier      = recipe.Tier;
                neighbour.Intensity = recipe.Intensity;
                neighbour.Section   = recipe.Section;
                neighbour.SetFaction(recipe.Faction, recipe.Influence);
            }
            if (first && room.HasBreachFaction) neighbour.SetFaction(room.BreachFaction, 1f);
            ConnectDirect(graph, room.Id, neighbour.Id);
        }

        // The level builder pulls the rooms together, so they meet as a map's neighbours do.
        private static void ConnectDirect(MapGraph graph, int a, int b)
        {
            if (graph.Connect(a, b)) graph.Connections[graph.Connections.Count - 1].Direct = true;
        }
    }
}
