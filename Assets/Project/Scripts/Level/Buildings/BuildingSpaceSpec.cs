using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // One kind of room a BuildingPlan asks for: how many, how wide, where, and which place
    // furnishes it. Anything left empty is still a walled room, just a bare one.
    [Serializable]
    public class BuildingSpaceSpec
    {
        [Tooltip("What the room is called in the Room Lab (Classroom, Principal's Office)")]
        [SerializeField] private string _name = "Room";
        [Tooltip("The place that furnishes this room: its props go in this room alone. Empty = a bare room")]
        [SerializeField] private RoomFunction _function;
        [Tooltip("The arrangement of the place this room takes (a classroom's desks, a surgery's tables): its set pieces and sets go in this room. Empty = the place's own props. The layout's floor plans, size and structure rules aren't used: the room is whatever the building made it")]
        [SerializeField] private RoomLayout _layout;
        [Tooltip("With no layout set: pick one of the place's layouts by weight for each room, as a level does, so the rooms of one kind differ")]
        [SerializeField] private bool _pickLayout;
        [Tooltip("How many of these the building has. A minimum of 1 or more makes it required: it is placed before anything optional, and reported when it didn't fit")]
        [SerializeField] private IntRange _count = new(1, 1);
        [Tooltip("Tiles along the hall (for a room at the Back: its depth). Rooms are at least 2 tiles")]
        [SerializeField] private IntRange _width = new(3, 4);
        [SerializeField] private BuildingSpacePlacement _placement;
        [Tooltip("When frontage is left over after everything asked for is placed, more of these fill it. If no spec fills, the last room on that side grows instead")]
        [SerializeField] private bool _fills;

        public string Name => _name;
        public RoomFunction Function => _function;
        public RoomLayout Layout => _layout;
        public IntRange Count => _count;
        public IntRange Width => _width;
        public BuildingSpacePlacement Placement => _placement;
        public bool Fills => _fills;
        public bool Required => _count.Min > 0;

        // The arrangement for one room of this kind: the pinned layout, else (with Pick Layout)
        // one of the place's by weight, else none. Only a pick draws a random number.
        public RoomLayout RollLayout(RandomStream random)
        {
            if (_layout != null) return _layout;
            return _pickLayout && _function != null ? _function.PickLayout(random) : null;
        }
    }
}
