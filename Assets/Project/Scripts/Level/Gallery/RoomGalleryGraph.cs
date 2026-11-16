using System.Collections.Generic;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // Turns RoomGallerySettings into a map graph LevelBuilder can build: Start, one room per
    // place, the showcase fight rooms, a frontier Combat room per faction, then Exit, in a
    // grid of rows joined in a snake so the walk visits every room once. Node positions are in cells (lay it out with a node
    // spacing of one), as the Room Lab's one-room graphs are.
    public static class RoomGalleryGraph
    {
        // The faction painter's lowest influence: a room on the edge of its territory.
        private const float FrontierInfluence = 0.35f;

        public static MapGraph Build(RoomGallerySettings gallery, LevelBuildSettings settings)
        {
            var graph = new MapGraph();
            int factions = gallery.Content != null ? gallery.Content.Factions.Count : 0;
            int sections = gallery.Content != null ? gallery.Content.Sections.Count : 0;
            MapNode previous = Add(graph, gallery, MapNodeType.Start);

            int index = 0;
            foreach (RoomFunction place in PlacesOf(gallery, settings))
            {
                MapNode node = Add(graph, gallery, gallery.PlaceType);
                node.Place    = place;
                node.Category = place.Category;
                if (factions > 0) node.SetFaction(index % factions, 1f);
                // Factions and sections turn at different rates, so a place meets each pairing in time.
                if (sections > 0) node.Section = index / Mathf.Max(1, factions) % sections;
                previous = Link(graph, previous, node);
                index++;
            }

            foreach (MapNodeType type in gallery.ShowcaseTypes)
                for (int faction = 0; faction < factions; faction++)
                {
                    MapNode node = Add(graph, gallery, type);
                    node.SetFaction(faction, 1f);
                    if (type.HasSecondFaction() && factions > 1) node.BreachFaction = (faction + 1) % factions;
                    previous = Link(graph, previous, node);
                }

            if (gallery.FrontierShowcase)
                for (int faction = 0; faction < factions; faction++)
                {
                    MapNode node = Add(graph, gallery, MapNodeType.Combat);
                    node.SetFaction(faction, FrontierInfluence);
                    previous = Link(graph, previous, node);
                }

            Link(graph, previous, Add(graph, gallery, MapNodeType.Exit));
            return graph;
        }

        private static IEnumerable<RoomFunction> PlacesOf(RoomGallerySettings gallery, LevelBuildSettings settings)
        {
            IReadOnlyList<RoomFunction> places = gallery.Places.Count > 0 ? gallery.Places : settings.Functions;
            foreach (RoomFunction place in places)
                if (place != null) yield return place;
        }

        // The next spot in the snake: rows run left to right, then right to left.
        private static MapNode Add(MapGraph graph, RoomGallerySettings gallery, MapNodeType type)
        {
            int k = graph.Nodes.Count;
            int perRow = gallery.RoomsPerRow;
            int row = k / perRow;
            int column = row % 2 == 0 ? k % perRow : perRow - 1 - k % perRow;
            MapNode node = graph.AddNode(type, new Vector2(column, row) * gallery.CellsApart);
            node.Tier = gallery.Tier;
            return node;
        }

        private static MapNode Link(MapGraph graph, MapNode from, MapNode to)
        {
            graph.Connect(from.Id, to.Id);
            return to;
        }
    }
}
