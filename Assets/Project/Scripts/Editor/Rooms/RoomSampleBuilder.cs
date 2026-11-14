using System.Collections.Generic;
using CGD.Core;
using CGD.Level;
using CGD.Map;
using UnityEngine;

namespace CGD.Editor
{
    // Builds a RoomLabRecipe into a RoomSample through the level's own pipeline, so what the
    // Room Lab shows is what a level gets:
    //   - Recipe: a one-room map graph — the room plus a small neighbour on each side that
    //     should have a doorway — laid out by LevelLayoutBuilder. Only the room is furnished.
    //   - MapRoom: the whole saved graph laid out with its own seed, as LevelBuilder does when
    //     its seed is left empty, then the chosen node's room.
    // Furnishing uses LevelBuilder's seed layers (seed → "contents" → RoomFurnisher).
    public static class RoomSampleBuilder
    {
        // Neighbours stand this many cells away: far enough that a two-cell room and a
        // two-cell neighbour never claim the same cell.
        private const int NeighbourCells = 3;
        private const int RoomId = 0;

        public static RoomSample Build(RoomLabRecipe recipe) => Build(recipe, recipe.Seed);

        // `seedText` overrides the recipe's seed (the variations strip).
        public static RoomSample Build(RoomLabRecipe recipe, string seedText)
        {
            if (recipe.BuildSettings == null) return new RoomSample("Pick the Level Build Settings to build rooms with.");
            return recipe.Source == RoomLabRecipe.SourceKind.MapRoom ? BuildMapRoom(recipe) : BuildRecipe(recipe, seedText);
        }

        private static RoomSample BuildRecipe(RoomLabRecipe recipe, string seedText)
        {
            MapGraph graph = OneRoomGraph(recipe);
            Seed seed = Seed.Parse(seedText);
            LevelLayout layout = new LevelLayoutBuilder(recipe.BuildSettings).Build(graph, Vector2.one, seed, recipe.Content);
            return Finish(layout, RoomId, recipe.BuildSettings, seed, seedText, recipe.DoorCount);
        }

        private static RoomSample BuildMapRoom(RoomLabRecipe recipe)
        {
            MapGraphAsset asset = recipe.Graph;
            if (asset == null || asset.Graph == null || asset.Graph.Nodes.Count == 0)
                return new RoomSample("Pick a Map Graph asset that has been generated.");
            if (!asset.Graph.TryGetNode(recipe.NodeId, out _))
                return new RoomSample($"The graph has no room #{recipe.NodeId}.");

            LevelLayout layout = new LevelLayoutBuilder(recipe.BuildSettings).Build(asset.Graph, asset.NodeSpacing, asset.Seed, asset.Content);
            return Finish(layout, recipe.NodeId, recipe.BuildSettings, asset.Seed, asset.SeedText, doorsWanted: -1);
        }

        private static RoomSample Finish(LevelLayout layout, int roomId, LevelBuildSettings settings, Seed seed, string seedText, int doorsWanted)
        {
            if (!layout.Rooms.TryGetValue(roomId, out LevelRoom room) || room.Structure == null)
                return new RoomSample(layout.Warnings.Count > 0 ? layout.Warnings[0] : "The room could not be laid out.");

            RoomFurnishing furnishing = new RoomFurnisher(settings, seed.Derive("contents")).Furnish(room);
            return new RoomSample(layout, room, furnishing, settings, seedText, doorsWanted, WarningsAbout(layout, room));
        }

        // The recipe's room at the origin and one Junction per doorway side; the corridor to
        // each comes out of the wall facing it.
        private static MapGraph OneRoomGraph(RoomLabRecipe recipe)
        {
            var graph = new MapGraph();
            MapNode room = graph.AddNode(recipe.Type, Vector2.zero);
            room.Place         = recipe.Place;
            room.Category      = recipe.Category;
            room.Tier          = recipe.Tier;
            room.Intensity     = recipe.Intensity;
            room.SetFaction(recipe.Faction, 1f);
            room.BreachFaction = recipe.Type.HasSecondFaction() ? recipe.BreachFaction : MapNode.NoFaction;

            // Node positions are in cells here (spacing 1); the editor's y grows downward.
            if (recipe.DoorNorth) AddNeighbour(graph, room, new Vector2(0f, -NeighbourCells));
            if (recipe.DoorEast)  AddNeighbour(graph, room, new Vector2(NeighbourCells, 0f));
            if (recipe.DoorSouth) AddNeighbour(graph, room, new Vector2(0f, NeighbourCells));
            if (recipe.DoorWest)  AddNeighbour(graph, room, new Vector2(-NeighbourCells, 0f));
            return graph;
        }

        private static void AddNeighbour(MapGraph graph, MapNode room, Vector2 position)
        {
            MapNode neighbour = graph.AddNode(MapNodeType.Junction, position);
            graph.Connect(room.Id, neighbour.Id);
        }

        // Layout warnings name rooms as "<Type> #<id>".
        private static List<string> WarningsAbout(LevelLayout layout, LevelRoom room)
        {
            string tag = $"#{room.Node.Id}";
            var warnings = new List<string>();
            foreach (string warning in layout.Warnings)
                if (ContainsId(warning, tag)) warnings.Add(warning);
            return warnings;
        }

        // "#1" must not match "#12".
        private static bool ContainsId(string text, string tag)
        {
            for (int at = text.IndexOf(tag, System.StringComparison.Ordinal); at >= 0;
                 at = text.IndexOf(tag, at + 1, System.StringComparison.Ordinal))
            {
                int after = at + tag.Length;
                if (after >= text.Length || !char.IsDigit(text[after])) return true;
            }
            return false;
        }
    }
}
