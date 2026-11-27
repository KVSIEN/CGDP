using System.Collections.Generic;
using CGD.Core;
using CGD.Level;
using CGD.Map;
using UnityEngine;

namespace CGD.Editor
{
    // Builds a RoomLabRecipe into a RoomSample through the level's own pipeline, so what the
    // Room Lab shows is what a level gets:
    //   - Recipe: a one-room map graph (RoomRecipeGraph) laid out by LevelLayoutBuilder.
    //     Only the room is furnished.
    //   - MapRoom: the whole saved graph laid out with its own seed, as LevelBuilder does when
    //     its seed is left empty, then the chosen node's room.
    // Furnishing uses LevelBuilder's seed layers (LevelSeeds.Contents → RoomFurnisher), and a
    // recipe's floor levels limit the room as the room generator scene's do (RoomRecipe.LayoutBuilder).
    public static class RoomSampleBuilder
    {
        private const int RoomId = RoomRecipeGraph.RoomId;

        public static RoomSample Build(RoomLabRecipe recipe) => Build(recipe, recipe.Seed);

        // `seedText` overrides the recipe's seed (the variations strip).
        public static RoomSample Build(RoomLabRecipe recipe, string seedText)
        {
            if (recipe.BuildSettings == null) return new RoomSample("Pick the Level Build Settings to build rooms with.");
            return recipe.Source == RoomLabRecipe.SourceKind.MapRoom ? BuildMapRoom(recipe) : BuildRecipe(recipe, seedText);
        }

        private static RoomSample BuildRecipe(RoomLabRecipe recipe, string seedText)
        {
            RoomRecipe room = recipe.ToRoomRecipe();
            Seed seed = Seed.Parse(seedText);
            LevelLayout layout = room.LayoutBuilder(recipe.BuildSettings).Build(RoomRecipeGraph.Build(room), Vector2.one, seed, recipe.Content);
            return Finish(layout, RoomId, recipe.BuildSettings, seed, seedText, recipe.Room.DoorCount);
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

            Seed contents = LevelSeeds.Contents(seed);
            RoomFurnishing furnishing = new RoomFurnisher(settings, contents).Furnish(room);
            // As RoomPopulator does, but without the keys and terminals that may take a tile first.
            int count = RoomEnemyPlanner.CountFor(settings, room);
            RoomEnemyPlan enemies = new RoomEnemyPlanner(settings).Plan(room, furnishing.Props, count,
                                                                        RoomEnemyPlanner.SeedFor(contents, room).Stream());
            return new RoomSample(layout, room, furnishing, enemies, settings, seedText, doorsWanted, WarningsAbout(layout, room));
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
