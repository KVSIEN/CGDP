using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Lays a BuildingPlan out in a rectangle of floor: the hall runs in from the front door,
    // rooms line both sides of it side by side, and one room may close off the far end.
    //
    //    front door
    //  ┌─────┬───┬───┬─────┐
    //  │room │   │   │room │      rooms are rolled from the plan, entrance ones first, then
    //  ├─────┤hall   ├─────┤      the required, then the optional; each goes on the side with
    //  │room │   │   │room │      the most frontage left. Leftover frontage is filled by the
    //  ├─────┴───┴───┴─────┤      plan's "fills" rooms, or the last room on that side grows.
    //  │    back room      │
    //  └───────────────────┘
    //
    // Pure geometry on tiles (no structure, no walls): the same blueprint can be written into a
    // room or a lot. The result tiles the whole rectangle, so every tile belongs to one space.
    public sealed class BuildingLayoutPlanner
    {
        private const int Min = BuildingPlan.MinRoomTiles;

        // The sides a front door can be on: it faces this way.

        private readonly BuildingPlan _plan;

        public BuildingLayoutPlanner(BuildingPlan plan) => _plan = plan;

        // `front`: the side of `lot` the front door is on (one of GridSides.All). Null when the lot is
        // too small for the plan or the plan has no rooms to line the hall with.
        public BuildingBlueprint Plan(RectInt lot, Vector2Int front, RandomStream random)
        {
            if (_plan == null) return null;
            var frame = new LotFrame(lot, front);
            if (!_plan.Fits(frame.Depth, frame.Across)) return null;

            var blueprint = new BuildingBlueprint(_plan);
            List<BuildingSpaceSpec> queue = Queue(random, blueprint, out BuildingSpaceSpec back, out List<BuildingSpaceSpec> fillers, out List<BuildingSpaceSpec> anyRoom);
            if (anyRoom.Count == 0) return null;

            int backDepth = BackDepth(back, frame.Depth, random, blueprint);
            int flank = frame.Depth - backDepth;

            int hall = Mathf.Min(_plan.HallTiles, frame.Across - Min);
            int total = frame.Across - hall;
            // Rooms on both sides when each can be a room; otherwise all on one.
            int a = total >= Min * 2 ? total / 2 : total;
            int b = total - a;
            if (random.Chance(0.5f)) (a, b) = (b, a);
            int hallFrom = a, hallTo = a + hall;

            var sides = new[]
            {
                new Side(0, a, roomRow: a - 1, hallRow: hallFrom),
                new Side(hallTo, frame.Across, roomRow: hallTo, hallRow: hallTo - 1),
            };

            foreach (BuildingSpaceSpec spec in queue)
            {
                Side side = Emptiest(sides, flank);
                if (side == null) blueprint.Missing.Add(spec.Name);
                else side.Add(spec, RollWidth(spec, flank - side.Cursor, random));
            }

            foreach (Side side in sides)
                Finish(side, flank, fillers, anyRoom, random);

            blueprint.Spaces.Add(new PlannedSpace(frame.Rect(0, flank, hallFrom, hallTo), _plan.HallFunction, "Hall", isHall: true));
            foreach (Side side in sides)
                foreach (Slot slot in side.Slots)
                {
                    blueprint.Spaces.Add(new PlannedSpace(frame.Rect(slot.From, slot.To, side.From, side.To), slot.Spec.Function, slot.Spec.Name, isHall: false,
                                                          layout: slot.Spec.RollLayout(random)));
                    int along = random.Range(slot.From, slot.To);
                    blueprint.Doors.Add(new BuildingDoor(frame.At(along, side.RoomRow), frame.At(along, side.HallRow)));
                }

            int middle = hallFrom + hall / 2;
            if (backDepth > 0)
            {
                blueprint.Spaces.Add(new PlannedSpace(frame.Rect(flank, frame.Depth, 0, frame.Across), back.Function, back.Name, isHall: false,
                                                      layout: back.RollLayout(random)));
                blueprint.Doors.Add(new BuildingDoor(frame.At(flank - 1, middle), frame.At(flank, middle)));
            }
            Vector2Int inside = frame.At(0, middle);
            blueprint.FrontDoor = new BuildingDoor(inside, inside + front);
            return blueprint;
        }

        // The rooms to place, in order: entrance rooms, then required, then optional (each group
        // shuffled). `back`: the one room for the far end, if any. `fillers`: rooms that may be
        // repeated to fill leftover frontage. `anyRoom`: every kind of room that can line a side.
        private List<BuildingSpaceSpec> Queue(RandomStream random, BuildingBlueprint blueprint, out BuildingSpaceSpec back,
                                              out List<BuildingSpaceSpec> fillers, out List<BuildingSpaceSpec> anyRoom)
        {
            back = null;
            fillers = new List<BuildingSpaceSpec>();
            anyRoom = new List<BuildingSpaceSpec>();
            var entrance = new List<BuildingSpaceSpec>();
            var required = new List<BuildingSpaceSpec>();
            var optional = new List<BuildingSpaceSpec>();

            foreach (BuildingSpaceSpec spec in _plan.Spaces)
            {
                if (spec == null) continue;
                bool isBack = spec.Placement == BuildingSpacePlacement.Back;
                if (!isBack)
                {
                    anyRoom.Add(spec);
                    if (spec.Fills) fillers.Add(spec);
                }

                int count = Mathf.Max(spec.Required ? 1 : 0, spec.Count.Evaluate(random));
                for (int i = 0; i < count; i++)
                {
                    if (isBack)
                    {
                        if (back == null) back = spec;
                        else blueprint.Missing.Add(spec.Name);
                    }
                    else if (spec.Placement == BuildingSpacePlacement.Entrance) entrance.Add(spec);
                    else (spec.Required ? required : optional).Add(spec);
                }
            }

            random.Shuffle(required);
            random.Shuffle(optional);
            entrance.AddRange(required);
            entrance.AddRange(optional);
            return entrance;
        }

        // How deep the room across the far end is (0 = none): at least a room, and leaving the
        // hall and the rooms beside it a little length.
        private static int BackDepth(BuildingSpaceSpec back, int depth, RandomStream random, BuildingBlueprint blueprint)
        {
            if (back == null) return 0;

            int most = depth - (Min + 1);
            if (most < Min)
            {
                blueprint.Missing.Add(back.Name);
                return 0;
            }
            return Mathf.Clamp(back.Width.Evaluate(random), Min, most);
        }

        // Tiles a room takes along the hall: its roll, or everything left when what would remain
        // is too little for a room of its own.
        private static int RollWidth(BuildingSpaceSpec spec, int remaining, RandomStream random)
        {
            int width = Mathf.Clamp(spec.Width.Evaluate(random), Min, remaining);
            return remaining - width < Min ? remaining : width;
        }

        // The side with the least frontage used that still has room for a room.
        private static Side Emptiest(Side[] sides, int flank)
        {
            Side best = null;
            foreach (Side side in sides)
                if (side.Exists && flank - side.Cursor >= Min && (best == null || side.Cursor < best.Cursor)) best = side;
            return best;
        }

        // Leftover frontage: more fillers, else the last room grows; a side nothing was placed on
        // gets a room of any kind, so no floor is left unassigned.
        private static void Finish(Side side, int flank, List<BuildingSpaceSpec> fillers, List<BuildingSpaceSpec> anyRoom, RandomStream random)
        {
            if (!side.Exists) return;

            while (fillers.Count > 0 && flank - side.Cursor >= Min)
            {
                BuildingSpaceSpec spec = random.Pick(fillers);
                side.Add(spec, RollWidth(spec, flank - side.Cursor, random));
            }
            if (side.Slots.Count == 0) side.Add(random.Pick(anyRoom), flank);
            else side.GrowLast(flank);
        }

        private sealed class Slot
        {
            public Slot(BuildingSpaceSpec spec, int from, int to)
            {
                Spec = spec;
                From = from;
                To   = to;
            }

            public BuildingSpaceSpec Spec { get; }
            public int From { get; }
            public int To   { get; set; }
        }

        // One side of the hall: the rows (across the building) its rooms cover, which of them
        // touches the hall, and the rooms so far, laid end to end from the front.
        private sealed class Side
        {
            public Side(int from, int to, int roomRow, int hallRow)
            {
                From    = from;
                To      = to;
                RoomRow = roomRow;
                HallRow = hallRow;
            }

            public int From { get; }
            public int To   { get; }
            // The row of the side's rooms next to the hall, and the hall's row beside it.
            public int RoomRow { get; }
            public int HallRow { get; }
            public int Cursor  { get; private set; }
            public List<Slot> Slots { get; } = new();
            public bool Exists => To > From;

            public void Add(BuildingSpaceSpec spec, int width)
            {
                Slots.Add(new Slot(spec, Cursor, Cursor + width));
                Cursor += width;
            }

            public void GrowLast(int to)
            {
                if (Cursor >= to) return;
                Slots[Slots.Count - 1].To = to;
                Cursor = to;
            }
        }

        // The lot seen from its front door: u runs from the door inward, v across it.
        private readonly struct LotFrame
        {
            private readonly Vector2Int _origin;
            private readonly Vector2Int _inward;
            private readonly Vector2Int _across;

            public LotFrame(RectInt lot, Vector2Int front)
            {
                _inward = -front;
                _across = front.x == 0 ? Vector2Int.right : Vector2Int.up;
                _origin = new Vector2Int(front.x > 0 ? lot.xMax - 1 : lot.xMin, front.y > 0 ? lot.yMax - 1 : lot.yMin);
                bool alongY = front.x == 0;
                Depth  = alongY ? lot.height : lot.width;
                Across = alongY ? lot.width : lot.height;
            }

            public int Depth  { get; }
            public int Across { get; }

            public Vector2Int At(int u, int v) => _origin + _inward * u + _across * v;

            // The tiles from u0 (inclusive) to u1 and v0 to v1 (exclusive).
            public RectInt Rect(int u0, int u1, int v0, int v1)
            {
                Vector2Int p = At(u0, v0), q = At(u1 - 1, v1 - 1);
                Vector2Int min = Vector2Int.Min(p, q), max = Vector2Int.Max(p, q);
                return new RectInt(min, max - min + Vector2Int.one);
            }
        }
    }
}
