using System.Collections.Generic;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // Turns RoomGallerySettings into a map graph LevelBuilder can build: Start, the place
    // rooms (a place that sits on several ship sides once per side, a place with several
    // layouts once per layout — as many rooms as the larger of the two), a wing per faction —
    // Combat rooms at its depth steps, frontier first, then its showcase fights — and the
    // Exit, in a grid of rows joined in a snake so the walk visits every room once. Node
    // positions are in cells (lay it out with a node spacing of one), as the Room Lab's
    // one-room graphs are.
    public static class RoomGalleryGraph
    {
        public static MapGraph Build(RoomGallerySettings gallery, LevelBuildSettings settings)
        {
            var graph = new MapGraph();
            MapContentSettings content = gallery.Content;
            int factions = content != null ? content.Factions.Count : 0;
            int sections = content != null ? content.Sections.Count : 0;
            MapNode previous = Add(graph, gallery, MapNodeType.Start);

            int turn = 0;
            foreach (RoomFunction place in PlacesOf(gallery, settings))
            {
                // Factions and sections turn at different rates, so a place meets each pairing in time.
                int section = sections > 0 ? turn / Mathf.Max(1, factions) % sections : MapNode.NoSection;
                List<int> placeSections = SectionsFor(place, gallery, section);
                List<RoomLayout> layouts = LayoutsFor(place, gallery);
                int rooms = Mathf.Max(placeSections.Count, layouts.Count);
                for (int i = 0; i < rooms; i++)
                {
                    MapNode node = Add(graph, gallery, gallery.PlaceType);
                    node.Place    = place;
                    node.Layout   = layouts.Count > 0 ? layouts[i % layouts.Count] : null;
                    node.Category = place.Category;
                    node.Section  = placeSections[i % placeSections.Count];
                    if (factions > 0) node.SetFaction(turn % factions, 1f);
                    previous = Link(graph, previous, node);
                }
                turn++;
            }

            for (int faction = 0; faction < factions; faction++)
                previous = AddWing(graph, gallery, previous, faction, factions);

            Link(graph, previous, Add(graph, gallery, MapNodeType.Exit));
            return graph;
        }

        // One faction's run: from the edge of its territory to its heart, then what its fights
        // look like there.
        private static MapNode AddWing(MapGraph graph, RoomGallerySettings gallery, MapNode previous, int faction, int factions)
        {
            foreach (float influence in gallery.DepthSteps)
            {
                MapNode node = Add(graph, gallery, MapNodeType.Combat);
                node.SetFaction(faction, influence);
                previous = Link(graph, previous, node);
            }

            foreach (MapNodeType type in gallery.ShowcaseTypes)
            {
                MapNode node = Add(graph, gallery, type);
                node.SetFaction(faction, 1f);
                if (type.HasSecondFaction() && factions > 1) node.BreachFaction = (faction + 1) % factions;
                previous = Link(graph, previous, node);
            }
            return previous;
        }

        // The section of each room a place gets: its turn's section, or with Every Side one per
        // side it lists, in a section of that side (one the content lacks keeps the turn's).
        private static List<int> SectionsFor(RoomFunction place, RoomGallerySettings gallery, int turnSection)
        {
            var sections = new List<int>();
            if (!gallery.EverySide || place.Sides.Count < 2 || gallery.Content == null)
            {
                sections.Add(turnSection);
                return sections;
            }

            foreach (ShipSide side in place.Sides)
            {
                int section = SectionOf(gallery.Content, side);
                sections.Add(section != MapNode.NoSection ? section : turnSection);
            }
            return sections;
        }

        // Each layout pinned in turn with Every Layout; none (picked by seed) otherwise.
        private static List<RoomLayout> LayoutsFor(RoomFunction place, RoomGallerySettings gallery)
        {
            var layouts = new List<RoomLayout>();
            if (!gallery.EveryLayout) return layouts;
            foreach (RoomLayout layout in place.Layouts)
                if (layout != null) layouts.Add(layout);
            return layouts;
        }

        private static int SectionOf(MapContentSettings content, ShipSide side)
        {
            for (int i = 0; i < content.Sections.Count; i++)
                if (content.Sections[i] != null && content.Sections[i].Side == side) return i;
            return MapNode.NoSection;
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
