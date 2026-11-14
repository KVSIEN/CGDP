using System.Collections.Generic;
using System.Text;
using CGD.Level;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The Room Lab's plan: the room's blueprint, a layer painted over it (tile tags, zones or
    // sightlines), the doorways, a highlighted issue, and a readout for the tile under the
    // mouse — what it is tagged, how far you see from it and what stands on it — so every
    // choice the generator made can be read off the floor.
    public class RoomLabPlanView
    {
        private static readonly Color Doorway  = new(0.3f, 0.9f, 1f, 0.9f);
        private static readonly Color Hover    = new(1f, 1f, 1f, 0.35f);
        private static readonly Color Issue    = new(0.95f, 0.25f, 0.2f, 0.9f);
        private static readonly Color Walkway  = new(0.3f, 0.75f, 1f, 0.45f);
        private static readonly Color NearDoor = new(0.3f, 1f, 0.9f, 0.5f);
        private static readonly Color Access   = new(1f, 0.85f, 0.2f, 0.5f);
        private static readonly Color Centre   = new(1f, 0.4f, 0.9f, 0.55f);
        private static readonly Color Corner   = new(0.65f, 0.5f, 1f, 0.45f);
        private static readonly Color Edge     = new(1f, 1f, 1f, 0.12f);
        private static readonly Color Open     = new(0.2f, 0.8f, 0.3f, 0.35f);
        private static readonly Color Exposed  = new(0.95f, 0.2f, 0.15f, 0.6f);

        private readonly RoomPlanTexture _plan = new();
        private readonly StringBuilder _readout = new();
        private RoomSample _rendered;
        private LevelBlueprint.Fill _renderedFill;

        public RoomLabLayer Layer { get; set; } = RoomLabLayer.Tags;
        public LevelBlueprint.Fill Fill { get; set; }
        // Tiles to outline (the issue picked in the report), or null.
        public RectInt? Highlight { get; set; }

        public void Release() => _plan.Release();

        public void Draw(Rect area, RoomSample sample)
        {
            EditorGUI.DrawRect(area, LevelBlueprint.Background);
            if (sample == null || !sample.IsValid)
            {
                DrawMessage(area, sample?.Message ?? "Nothing to show yet.");
                return;
            }
            if (_rendered != sample || _renderedFill != Fill)
            {
                _plan.Render(sample, Fill);
                _rendered     = sample;
                _renderedFill = Fill;
            }

            Rect image = _plan.Fit(new Rect(area.x + 8f, area.y + 8f, area.width - 16f, area.height - 52f));
            _plan.Draw(image);
            if (Event.current.type == EventType.Repaint)
            {
                DrawLayer(image, sample);
                DrawDoorways(image, sample);
                if (Highlight.HasValue) Outline(image, Highlight.Value, Issue, 2f);
            }
            DrawHover(area, image, sample);
            DrawLegend(new Rect(area.x + 8f, area.yMax - 40f, area.width - 16f, 16f), sample);
        }

        // --- Layers ---------------------------------------------------------------------

        private void DrawLayer(Rect image, RoomSample sample)
        {
            RoomStructure structure = sample.Room.Structure;
            foreach (Vector2Int tile in sample.Room.Footprint.Tiles)
            {
                Color? color = Layer switch
                {
                    RoomLabLayer.Tags       => TagColor(structure.TagsAt(tile)),
                    RoomLabLayer.Zones      => structure.ZoneCount > 1 ? ZoneColor(structure.ZoneOf(tile)) : (Color?)null,
                    RoomLabLayer.Sightlines => SightColor(sample, tile),
                    _                       => null,
                };
                if (color.HasValue) EditorGUI.DrawRect(TileRect(image, tile), color.Value);
            }
        }

        // The tag that says most about a tile: what it is kept free for before where it is.
        private static Color? TagColor(RoomTileTags tags)
        {
            if ((tags & (RoomTileTags.Structure | RoomTileTags.Prop)) != 0) return null;
            if ((tags & RoomTileTags.Centre)   != 0) return Centre;
            if ((tags & RoomTileTags.NearDoor) != 0) return NearDoor;
            if ((tags & RoomTileTags.Access)   != 0) return Access;
            if ((tags & RoomTileTags.Walkway)  != 0) return Walkway;
            if ((tags & RoomTileTags.Corner)   != 0) return Corner;
            if ((tags & RoomTileTags.Edge)     != 0) return Edge;
            return null;
        }

        private static Color ZoneColor(int zone)
        {
            Color color = MapGraphStyle.PaletteColor(zone);
            color.a = 0.4f;
            return color;
        }

        private static Color? SightColor(RoomSample sample, Vector2Int tile)
        {
            int run = sample.Sight.LongestRun(tile);
            if (run == 0) return null;
            return Color.Lerp(Open, Exposed, Mathf.InverseLerp(2f, RoomSampleChecks.ExposedLaneTiles + 2f, run));
        }

        private void DrawDoorways(Rect image, RoomSample sample)
        {
            foreach (LevelDoorway doorway in sample.Layout.Doorways)
                if (doorway.Room == sample.Room) Outline(image, new RectInt(doorway.RoomTile, Vector2Int.one), Doorway, 2f);
        }

        // --- Hover ----------------------------------------------------------------------

        private void DrawHover(Rect area, Rect image, RoomSample sample)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseMove && area.Contains(e.mousePosition)) GUI.changed = true;   // repaint as the mouse moves
            if (!image.Contains(e.mousePosition)) return;

            Vector2Int tile = TileAt(image, e.mousePosition);
            if (!sample.Layout.IsWalkable(tile)) return;
            if (e.type == EventType.Repaint) EditorGUI.DrawRect(TileRect(image, tile), Hover);

            string text = Readout(sample, tile);
            var box = new Rect(area.x + 8f, area.y + 8f, 300f, 16f * (1 + CountLines(text)));
            GUI.Box(box, GUIContent.none, EditorStyles.helpBox);
            GUI.Label(new Rect(box.x + 4f, box.y + 2f, box.width - 8f, box.height - 4f), text, EditorStyles.wordWrappedMiniLabel);
        }

        private string Readout(RoomSample sample, Vector2Int tile)
        {
            _readout.Clear();
            LevelRoom room = sample.Layout.RoomAt(tile);
            if (room != sample.Room)
                return room != null ? $"Tile {tile.x}, {tile.y} — neighbouring room ({room.DisplayName})" : $"Tile {tile.x}, {tile.y} — corridor";

            RoomStructure structure = room.Structure;
            _readout.Append($"Tile {tile.x}, {tile.y}");
            if (structure.ZoneCount > 1) _readout.Append($" · zone {structure.ZoneOf(tile) + 1}");
            RoomTileTags tags = structure.TagsAt(tile);
            _readout.Append('\n').Append(tags == RoomTileTags.None ? "Open floor" : Describe(tags));

            (int alongX, int alongY) = sample.Sight.Runs(tile);
            if (alongX + alongY > 0)
                _readout.Append($"\nSees {alongX * sample.Settings.TileSize:0} m east–west, {alongY * sample.Settings.TileSize:0} m north–south");

            foreach (PlannedProp prop in sample.Furnishing.Props)
                if (prop.Tiles.Contains(tile) && prop.Prefab != null)
                    _readout.Append($"\n{prop.Prefab.name}{(prop.Decor ? " (decor)" : "")}, facing {Mathf.Repeat(prop.Yaw, 360f):0}°");
            return _readout.ToString();
        }

        private static string Describe(RoomTileTags tags)
        {
            var parts = new List<string>();
            if ((tags & RoomTileTags.Structure) != 0) parts.Add("pillar / structure");
            if ((tags & RoomTileTags.Prop)      != 0) parts.Add("furniture");
            if ((tags & RoomTileTags.Centre)    != 0) parts.Add("centre spot (centrepiece)");
            if ((tags & RoomTileTags.NearDoor)  != 0) parts.Add("in front of a doorway (kept clear)");
            if ((tags & RoomTileTags.Walkway)   != 0) parts.Add("walkway (kept clear of props)");
            if ((tags & RoomTileTags.Access)    != 0) parts.Add("access (floor kept free to use something)");
            if ((tags & RoomTileTags.Corner)    != 0) parts.Add("corner");
            else if ((tags & RoomTileTags.Edge) != 0) parts.Add("against a wall");
            return string.Join(", ", parts);
        }

        private static int CountLines(string text)
        {
            int lines = 1;
            foreach (char c in text) if (c == '\n') lines++;
            return lines;
        }

        private void DrawLegend(Rect rect, RoomSample sample)
        {
            string legend = Layer switch
            {
                RoomLabLayer.Tags       => "cyan doorway front · blue walkway · yellow access · pink centre · violet corner · sand furniture",
                RoomLabLayer.Zones      => sample.Room.Structure.ZoneCount > 1 ? $"{sample.Room.Structure.ZoneCount} zones split by inner walls" : "One zone: no inner walls split this room",
                RoomLabLayer.Sightlines => $"green short sightlines → red {RoomSampleChecks.ExposedLaneTiles * sample.Settings.TileSize:0} m+ open lanes",
                _                       => "sand furniture · dark pillars · cyan outline doorway",
            };
            GUI.Label(rect, legend + " · hover a tile for details", EditorStyles.whiteMiniLabel);
        }

        // --- Geometry -------------------------------------------------------------------

        private Rect TileRect(Rect image, Vector2Int tile)
        {
            Vector2 min = ToGui(image, tile), max = ToGui(image, tile + Vector2Int.one);
            return Rect.MinMaxRect(min.x, max.y, max.x, min.y);
        }

        private void Outline(Rect image, RectInt tiles, Color color, float thickness)
        {
            Vector2 min = ToGui(image, tiles.min), max = ToGui(image, tiles.max);
            MapGraphStyle.DrawOutline(Rect.MinMaxRect(min.x, max.y, max.x, min.y), color, thickness);
        }

        // Tile coordinates (y up) to GUI (y down) and back.
        private Vector2 ToGui(Rect image, Vector2 tile)
        {
            RectInt frame = _plan.Frame;
            return new Vector2(image.x + (tile.x - frame.xMin) / frame.width * image.width,
                               image.yMax - (tile.y - frame.yMin) / frame.height * image.height);
        }

        private Vector2Int TileAt(Rect image, Vector2 point)
        {
            RectInt frame = _plan.Frame;
            float x = frame.xMin + (point.x - image.x) / image.width * frame.width;
            float y = frame.yMin + (image.yMax - point.y) / image.height * frame.height;
            return new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
        }

        private static void DrawMessage(Rect rect, string message)
        {
            GUILayout.BeginArea(new Rect(rect.center.x - 180f, rect.center.y - 25f, 360f, 50f));
            EditorGUILayout.HelpBox(message, MessageType.Info);
            GUILayout.EndArea();
        }
    }
}
