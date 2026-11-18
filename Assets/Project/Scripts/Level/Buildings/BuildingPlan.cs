using System;
using UnityEngine;

namespace CGD.Level
{
    // A floor plan for a whole building: one hall running in from the front door, with rooms on
    // either side and, optionally, one big room across the far end. It says what the rooms are
    // (a school's classrooms, a hospital's wards), not where: BuildingLayoutPlanner places them
    // inside whatever space it is given, so the same plan fills a small lot or a whole room.
    //
    // A plan is used by a BuildingStructureRule (the building is the room) or by a
    // TownStructureRule (the building is one lot among streets).
    [CreateAssetMenu(fileName = "BuildingPlan", menuName = "CGD/Level/Building Plan")]
    public class BuildingPlan : ScriptableObject
    {
        // The smallest a room is, hall to wall, and along the hall.
        public const int MinRoomTiles = 2;

        [SerializeField] private string _displayName = "Building";
        [Tooltip("Tiles wide the hall is (1 = 3 m with the default tiles)")]
        [SerializeField, Min(1)] private int _hallTiles = 1;
        [Tooltip("What furnishes the hall (lockers, benches, a front desk). Empty = a bare hall")]
        [SerializeField] private RoomFunction _hallFunction;
        [Tooltip("Metres of clear height under the lintel over every doorway")]
        [SerializeField, Min(1f)] private float _doorHeight = 2.6f;
        [Tooltip("The rooms of the building. Required ones first, then the rest, side by side along the hall")]
        [SerializeField] private BuildingSpaceSpec[] _spaces = Array.Empty<BuildingSpaceSpec>();

        public string DisplayName => _displayName;
        public int HallTiles => Mathf.Max(1, _hallTiles);
        public RoomFunction HallFunction => _hallFunction;
        public float DoorHeight => _doorHeight;
        public BuildingSpaceSpec[] Spaces => _spaces;

        // Whether a lot `depth` tiles from the front door inward and `across` tiles wide has room
        // for the hall and rooms on at least one side of it.
        public bool Fits(int depth, int across) =>
            depth >= MinRoomTiles * 2 && across >= HallTiles + MinRoomTiles;
    }
}
