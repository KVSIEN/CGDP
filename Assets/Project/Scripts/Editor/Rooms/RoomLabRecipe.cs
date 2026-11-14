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
        public MapNodeType Type = MapNodeType.Combat;
        [Tooltip("Pinned place. Empty = picked from the category, as the level does")]
        public RoomFunction Place;
        public RoomCategory Category;
        [Tooltip("Factions come from this content's list")]
        public MapContentSettings Content;
        public int Faction = MapNode.NoFaction;
        public int BreachFaction = MapNode.NoFaction;
        [Range(1, 3)] public int Tier = 1;
        [Range(0f, 1f)] public float Intensity = 0.5f;
        public bool DoorNorth = true, DoorEast = true, DoorSouth = true, DoorWest;

        // --- Map room -------------------------------------------------------------------
        public MapGraphAsset Graph;
        public int NodeId;

        public int DoorCount => (DoorNorth ? 1 : 0) + (DoorEast ? 1 : 0) + (DoorSouth ? 1 : 0) + (DoorWest ? 1 : 0);

        public RoomLabRecipe Clone() => (RoomLabRecipe)MemberwiseClone();

        // A recipe for the room a map graph node becomes, so it can be tried on its own with
        // other doorways, places or seeds. Doorways keep the sides the node's neighbours lie on.
        public static RoomLabRecipe FromMapNode(MapGraphAsset graph, MapNode node, LevelBuildSettings settings)
        {
            var recipe = new RoomLabRecipe
            {
                Source        = SourceKind.Recipe,
                BuildSettings = settings,
                Seed          = graph.SeedText,
                Type          = node.Type,
                Place         = node.Place as RoomFunction,
                Category      = node.Category,
                Content       = graph.Content,
                Faction       = node.Faction,
                BreachFaction = node.BreachFaction,
                Tier          = node.Tier,
                Intensity     = node.Intensity,
                DoorNorth = false, DoorEast = false, DoorSouth = false, DoorWest = false,
            };

            foreach (MapConnection connection in graph.Graph.Connections)
            {
                if (!connection.Connects(node.Id) || !graph.Graph.TryGetNode(connection.Other(node.Id), out MapNode other)) continue;

                // The editor's y grows downward, so a neighbour drawn above lies north.
                Vector2 to = other.Position - node.Position;
                if (Mathf.Abs(to.x) >= Mathf.Abs(to.y)) { if (to.x >= 0f) recipe.DoorEast = true; else recipe.DoorWest = true; }
                else if (to.y <= 0f) recipe.DoorNorth = true;
                else recipe.DoorSouth = true;
            }
            return recipe;
        }
    }
}
