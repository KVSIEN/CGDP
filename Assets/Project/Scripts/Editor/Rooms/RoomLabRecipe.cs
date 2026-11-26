using System;
using CGD.Level;
using CGD.Map;
using UnityEngine;

namespace CGD.Editor
{
    // What the Room Lab builds. Either a room made up here (Recipe: a node type, place,
    // faction, tier and which walls get doorways, left blank to let the generator pick as it
    // would) or a room of a saved map graph (MapRoom: exactly the room that node becomes in
    // the level, corridors and neighbours included).
    [Serializable]
    public class RoomLabRecipe
    {
        public enum SourceKind { Recipe, MapRoom }

        public SourceKind Source = SourceKind.Recipe;
        public LevelBuildSettings BuildSettings;
        public string Seed = "1";

        // --- Recipe ---------------------------------------------------------------------
        // The made-up room (type, place, layout, faction, tier, levels, doorways…), as the
        // room generator scene resolves its own to.
        public RoomRecipe Room = new();
        [Tooltip("Factions come from this content's list")]
        public MapContentSettings Content;

        // --- Map room -------------------------------------------------------------------
        public MapGraphAsset Graph;
        public int NodeId;

        public RoomLabRecipe Clone()
        {
            var copy = (RoomLabRecipe)MemberwiseClone();
            copy.Room = Room.Clone();
            return copy;
        }

        // The made-up room alone, for RoomRecipeGraph: a second faction only where the type has one.
        public RoomRecipe ToRoomRecipe()
        {
            RoomRecipe room = Room.Clone();
            if (!room.Type.HasSecondFaction()) room.BreachFaction = MapNode.NoFaction;
            return room;
        }

        // A recipe for the room a map graph node becomes, so it can be tried on its own with
        // other doorways, places or seeds. Doorways keep the sides the node's neighbours lie on.
        public static RoomLabRecipe FromMapNode(MapGraphAsset graph, MapNode node, LevelBuildSettings settings)
        {
            var recipe = new RoomLabRecipe
            {
                Source        = SourceKind.Recipe,
                BuildSettings = settings,
                Seed          = graph.SeedText,
                Content       = graph.Content,
                Room = new RoomRecipe
                {
                    Type          = node.Type,
                    Place         = node.Place as RoomFunction,
                    Layout        = node.Layout as RoomLayout,
                    Category      = node.Category,
                    Faction       = node.Faction,
                    BreachFaction = node.BreachFaction,
                    Influence     = node.FactionInfluence,
                    Section       = node.Section,
                    Tier          = node.Tier,
                    Intensity     = node.Intensity,
                    DoorNorth = false, DoorEast = false, DoorSouth = false, DoorWest = false,
                },
            };

            foreach (MapConnection connection in graph.Graph.Connections)
            {
                if (!connection.Connects(node.Id) || !graph.Graph.TryGetNode(connection.Other(node.Id), out MapNode other)) continue;

                // The editor's y grows downward, so a neighbour drawn above lies north.
                Vector2 to = other.Position - node.Position;
                if (Mathf.Abs(to.x) >= Mathf.Abs(to.y)) { if (to.x >= 0f) recipe.Room.DoorEast = true; else recipe.Room.DoorWest = true; }
                else if (to.y <= 0f) recipe.Room.DoorNorth = true;
                else recipe.Room.DoorSouth = true;
            }
            return recipe;
        }
    }
}
