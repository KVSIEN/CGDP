using System.Collections.Generic;
using System.Text;
using CGD.Level;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The Room Lab's plan: the room's blueprint, a layer painted over it (tile tags, zones,
    // sightlines or enemy posts), the doorways, a highlighted issue, and a readout for the tile under the
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
        private static readonly Color Concealed = new(0.2f, 0.85f, 0.3f, 0.55f);
        private static readonly Color Overwatch = new(0.95f, 0.3f, 0.2f, 0.5f);
        private static readonly Color Choke     = new(1f, 0.6f, 0.15f, 0.45f);
        private static readonly Color DeadEnd   = new(0.6f, 0.35f, 0.95f, 0.45f);
        private static readonly Color Route     = new(0.3f, 0.7f, 1f, 0.25f);
        private static readonly Color HighGround = new(1f, 0.75f, 0.3f, 0.5f);
        private static readonly Color Threshold = new(0.3f, 0.95f, 0.75f, 0.45f);
        private static readonly Color Goal      = new(1f, 0.8f, 0.25f, 0.5f);
        private static readonly Color Arrival   = new(0.3f, 1f, 0.6f, 1f);
        private static readonly Color HolderSide = new(1f, 1f, 1f, 0.95f);
        private static readonly Color BreachSide = new(1f, 0.35f, 0.85f, 0.95f);
        private static readonly Color Raised    = new(1f, 0.55f, 0.2f, 0.45f);
        private static readonly Color Sunken    = new(0.25f, 0.45f, 1f, 0.45f);
        private static readonly Color StairTile = new(1f, 0.9f, 0.25f, 0.7f);
        private static readonly Color SlopeTile = new(0.55f, 0.85f, 0.35f, 0.7f);
        private static readonly Color DeckTile  = new(0.7f, 0.4f, 1f, 0.75f);
        private static readonly Color OpenLedge  = new(1f, 1f, 1f, 0.95f);
        private static readonly Color RailedLedge = new(0.45f, 0.45f, 0.5f, 1f);
        private static readonly Color HalfWallLedge = new(0.2f, 0.2f, 0.25f, 1f);

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
                    RoomLabLayer.Posts      => PostColor(sample.Posts.KindsAt(tile)),
                    RoomLabLayer.Flow       => FlowColor(structure.TagsAt(tile)),
                    RoomLabLayer.Elevation  => ElevationColor(structure, tile),
                    _                       => null,
                };
                if (color.HasValue) EditorGUI.DrawRect(TileRect(image, tile), color.Value);
            }
            if (Layer == RoomLabLayer.Posts) DrawEnemies(image, sample);
            if (Layer == RoomLabLayer.Elevation) DrawLevels(image, structure);
            if (Layer == RoomLabLayer.Flow && structure.Arrival.HasValue)
                Outline(image, new RectInt(structure.Arrival.Value, Vector2Int.one), Arrival, 3f);
        }

        private static Color? ElevationColor(RoomStructure structure, Vector2Int tile)
        {
            if (structure.TryGetStairs(tile, out StairFlight flight)) return flight.Slope ? SlopeTile : StairTile;
            int level = structure.LevelOf(tile);
            if (level == 0) return null;

            // Deeper tiers in a stronger shade, so floor several levels up or down reads apart.
            Color color = level > 0 ? Raised : Sunken;
            color.a = Mathf.Min(0.9f, color.a * (0.6f + 0.4f * Mathf.Abs(level)));
            return color;
        }

        // Each flight as an arrow climbing toward its head, and each ledge as a line along its edge:
        // white where someone can drop off it, grey where a railing stops them. An overpass is a
        // violet band along its deck, over the floor it crosses, with an arrow up each flight.
        private void DrawLevels(Rect image, RoomStructure structure)
        {
            foreach (Overpass overpass in structure.Overpasses)
            {
                foreach (Vector2Int deck in overpass.Deck) EditorGUI.DrawRect(Shrink(TileRect(image, deck), 0.25f), DeckTile);
                foreach (StairFlight flight in overpass.Stairs)
                {
                    EditorGUI.DrawRect(TileRect(image, flight.Tile), StairTile);
                    DrawArrow(image, flight.Tile, flight.Up);
                }
            }
            if (!structure.HasElevation) return;

            Color before = Handles.color;
            var cut = new HashSet<Vector2Int>();
            foreach (ElevatedArea area in structure.Elevations)
                foreach (GroundCorner corner in area.Corners)
                {
                    if (!corner.Sheer)
                    {
                        EditorGUI.DrawRect(Shrink(TileRect(image, corner.Tile), 0.2f), SlopeTile);
                        continue;
                    }
                    // A cut corner: its raised half, and its ledge across the diagonal.
                    cut.Add(corner.Tile);
                    var (inner, a, b) = corner.RaisedHalf;
                    Handles.color = Raised;
                    Handles.DrawAAConvexPolygon(ToGui(image, inner), ToGui(image, a), ToGui(image, b));
                    LedgeGuard guard = area.GuardAcross(corner);
                    Handles.color = guard == LedgeGuard.None ? OpenLedge : guard == LedgeGuard.HalfWall ? HalfWallLedge : RailedLedge;
                    Handles.DrawAAPolyLine(guard == LedgeGuard.None ? 2f : guard == LedgeGuard.HalfWall ? 6f : 4f, ToGui(image, a), ToGui(image, b));
                }

            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                if (structure.TryGetStairs(tile, out StairFlight stairs)) DrawArrow(image, tile, stairs.Up);

                foreach (Vector2Int side in new[] { Vector2Int.right, Vector2Int.up })
                {
                    Vector2Int next = tile + side;
                    if (!structure.IsLedge(tile, next) || cut.Contains(tile) || cut.Contains(next)) continue;

                    bool open = structure.CanDropDown(tile, next) || structure.CanDropDown(next, tile);
                    Vector2 a = tile + Vector2.one - (Vector2)Perpendicular(side);   // the far corner of the shared edge…
                    Vector2 b = tile + Vector2.one;                                   // …and the near one
                    bool halfWall = structure.GuardOn(tile, next) == LedgeGuard.HalfWall;
                    Handles.color = open ? OpenLedge : halfWall ? HalfWallLedge : RailedLedge;
                    Handles.DrawAAPolyLine(open ? 2f : halfWall ? 6f : 4f, ToGui(image, a), ToGui(image, b));
                }
            }
            Handles.color = before;
        }

        // A black arrow across a tile, pointing `up`.
        private void DrawArrow(Rect image, Vector2Int tile, Vector2Int up)
        {
            Color before = Handles.color;
            Vector2 middle = tile + Vector2.one * 0.5f;
            Vector2 to = ToGui(image, middle + (Vector2)up * 0.35f);
            Handles.color = Color.black;
            Handles.DrawAAPolyLine(3f, ToGui(image, middle - (Vector2)up * 0.35f), to);
            Handles.DrawAAPolyLine(3f, to, ToGui(image, middle + (Vector2)up * 0.1f + (Vector2)Perpendicular(up) * 0.2f));
            Handles.DrawAAPolyLine(3f, to, ToGui(image, middle + (Vector2)up * 0.1f - (Vector2)Perpendicular(up) * 0.2f));
            Handles.color = before;
        }

        private static Vector2Int Perpendicular(Vector2Int side) => new(side.y, side.x);

        // The threshold and the goal; the contest between them is left plain.
        private static Color? FlowColor(RoomTileTags tags)
        {
            if ((tags & (RoomTileTags.Structure | RoomTileTags.Prop)) != 0) return null;
            if ((tags & RoomTileTags.Threshold) != 0) return Threshold;
            if ((tags & RoomTileTags.Goal)      != 0) return Goal;
            return null;
        }

        // The kind that matters most to the factions: hiding, then firing positions, then the rest.
        private static Color? PostColor(RoomPostKinds kinds)
        {
            if ((kinds & RoomPostKinds.Concealed) != 0) return Concealed;
            if ((kinds & RoomPostKinds.Overwatch) != 0) return Overwatch;
            if ((kinds & RoomPostKinds.HighGround) != 0) return HighGround;
            if ((kinds & RoomPostKinds.DeadEnd)   != 0) return DeadEnd;
            if ((kinds & RoomPostKinds.Choke)     != 0) return Choke;
            if ((kinds & RoomPostKinds.Route)     != 0) return Route;
            return null;
        }

        // Each side's patrol loop, then every spawn: a ring for a guard holding its post, a dot
        // for a patroller, white for the holder and pink for the breaching faction, framed in
        // violet when it starts up on an overpass's deck.
        private void DrawEnemies(Rect image, RoomSample sample)
        {
            RoomEnemyPlan plan = sample.Enemies;
            DrawLoop(image, plan.PatrolOf(false), HolderSide);
            if (plan.Split != null) DrawLoop(image, plan.PatrolOf(true), BreachSide);

            foreach (PlannedSpawn spawn in plan.Spawns)
            {
                Color color = spawn.Breach ? BreachSide : HolderSide;
                Rect rect = TileRect(image, spawn.Tile);
                if (spawn.OnDeck) MapGraphStyle.DrawOutline(rect, DeckTile, 2f);
                if (spawn.Holds) MapGraphStyle.DrawOutline(Shrink(rect, 0.15f), color, 2f);
                else EditorGUI.DrawRect(Shrink(rect, 0.32f), color);
            }
        }

        private void DrawLoop(Rect image, IReadOnlyList<StandSpot> loop, Color color)
        {
            if (loop.Count < 2) return;
            var points = new Vector3[loop.Count + 1];
            for (int i = 0; i <= loop.Count; i++) points[i] = TileRect(image, loop[i % loop.Count].Tile).center;
            Color before = Handles.color;
            Handles.color = new Color(color.r, color.g, color.b, 0.6f);
            Handles.DrawAAPolyLine(2f, points);
            Handles.color = before;
        }

        private static Rect Shrink(Rect rect, float share)
        {
            float dx = rect.width * share, dy = rect.height * share;
            return new Rect(rect.x + dx, rect.y + dy, rect.width - 2f * dx, rect.height - 2f * dy);
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
            RoomVisibility visibility = sample.Visibility;
            float run = visibility == null ? sample.Sight.LongestRun(tile)
                      : visibility.Area(tile) > 0 ? visibility.Reach(tile) + 1f : 0f;
            if (run == 0f) return null;
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
            if (structure.HasElevation) _readout.Append($" · {LevelName(structure.LevelOf(tile))}");
            if (structure.TryGetStairs(tile, out StairFlight flight)) _readout.Append($" · {(flight.Slope ? "slope" : "stairs")} climbing {SideName(flight.Up)}");
            Overpass above = structure.OverpassAbove(tile);
            if (above != null)
                _readout.Append($" · under an overpass ({RoomElevation.HeightOf(above.Level) - RoomElevation.DeckThickness - RoomElevation.HeightOf(structure.LevelOf(tile)):0.0} m clear)");
            foreach (Overpass overpass in structure.Overpasses)
                foreach (StairFlight onto in overpass.Stairs)
                    if (onto.Tile == tile) _readout.Append($" · stairs up onto an overpass, climbing {SideName(onto.Up)}");
            if (sample.Split != null)
                _readout.Append($" ({(sample.Split.IsBreachZone(structure.ZoneOf(tile)) ? room.BreachFaction.DisplayName : room.Faction?.DisplayName ?? "holder")}'s side)");
            RoomTileTags tags = structure.TagsAt(tile);
            _readout.Append('\n').Append(tags == RoomTileTags.None ? "Open floor" : Describe(tags));

            (int alongX, int alongY) = sample.Sight.Runs(tile);
            if (alongX + alongY > 0)
                _readout.Append($"\nSees {alongX * sample.Settings.TileSize:0} m east–west, {alongY * sample.Settings.TileSize:0} m north–south");
            if (sample.Visibility != null && sample.Visibility.Area(tile) > 0)
                _readout.Append($"\nSees {sample.Visibility.Area(tile)} tiles, the farthest {sample.Visibility.Reach(tile) * sample.Settings.TileSize:0} m away");

            RoomPostKinds kinds = sample.Posts.KindsAt(tile);
            if (kinds != RoomPostKinds.None) _readout.Append($"\nFor an enemy: {kinds.ToString().ToLowerInvariant()}");
            foreach (PlannedSpawn spawn in sample.Enemies.Spawns)
                if (spawn.Tile == tile)
                    _readout.Append($"\nAn enemy starts {(spawn.OnDeck ? "on the overpass above" : "here")} ({(spawn.Breach ? room.BreachFaction?.DisplayName : room.Faction?.DisplayName) ?? "no faction"}) and {(spawn.Holds ? "holds the post" : "patrols")}");

            foreach (PlannedProp prop in sample.Furnishing.Props)
                if (prop.Tiles.Contains(tile) && prop.Prefab != null)
                    _readout.Append($"\n{prop.Prefab.name}{(prop.Decor ? " (decor)" : "")}, facing {Mathf.Repeat(prop.Yaw, 360f):0}°{(prop.Decor ? "" : SeenOver(PropHeights.Of(prop.Prefab)))}");
            return _readout.ToString();
        }

        // What a prop's height means for sight (SightBlocking's eye heights).
        private static string SeenOver(float height)
        {
            if (float.IsPositiveInfinity(height)) return ", no prefab: taken to block sight";
            if (height >= SightBlocking.EyeHeight) return $", {height:0.0#} m: blocks sight";
            if (height >= SightBlocking.CrouchingEyeHeight) return $", {height:0.0#} m: seen over standing, hides someone crouching";
            return height > 0f ? $", {height:0.0#} m: seen over" : ", no collider: seen through";
        }

        private static string LevelName(int level) => level == 0 ? "doorways' level" : $"level {level:+0;-0} ({RoomElevation.HeightOf(level):+0.#;-0.#} m)";

        private static string SideName(Vector2Int side) =>
            side == Vector2Int.up ? "north" : side == Vector2Int.right ? "east" : side == Vector2Int.down ? "south" : "west";

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
            if ((tags & RoomTileTags.Threshold) != 0) parts.Add("threshold (just inside the way in)");
            if ((tags & RoomTileTags.Goal)      != 0) parts.Add("goal (the far end)");
            if ((tags & RoomTileTags.Stairs)    != 0) parts.Add("stairs");
            if ((tags & RoomTileTags.Landing)   != 0) parts.Add("landing (kept clear at the end of a flight)");
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
                RoomLabLayer.Sightlines => sample.Visibility == null
                    ? $"green short sightlines → red {RoomSampleChecks.ExposedLaneTiles * sample.Settings.TileSize:0} m+ open lanes"
                    : $"green short sightlines → red {RoomSampleChecks.ExposedLaneTiles * sample.Settings.TileSize:0} m+ lines of sight, in any direction",
                RoomLabLayer.Posts      => "green concealed · red overwatch · amber high ground · violet dead end · orange choke · blue route · ring guard, dot patrol (pink: breach side)",
                RoomLabLayer.Flow       => FlowLegend(sample.Room.Structure),
                RoomLabLayer.Elevation  => sample.Room.Structure.HasElevation || sample.Room.Structure.Overpasses.Count > 0
                    ? "orange raised · blue sunken · yellow stairs, green slopes (arrow climbing; small square: a corner) · violet overpass deck · white ledge you can drop off · grey railed · dark thick half wall"
                    : "Flat: every tile on the doorways' level (Elevation rules need Level Build Settings › Lowest/Highest Level)",
                _                       => "sand furniture · dark pillars · cyan outline doorway",
            };
            GUI.Label(rect, legend + " · hover a tile for details", EditorStyles.whiteMiniLabel);
        }

        private static string FlowLegend(RoomStructure structure)
        {
            if (!structure.Arrival.HasValue) return "No doorway leads back toward Start: this room has no flow";
            string flank = RoomFlank.Has(structure) ? "a flank goes round the main lane" : "no flank: one lane through the middle";
            return $"green outline the way in · teal threshold · gold goal · {flank}";
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
