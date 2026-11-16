using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // What one side of the ship does to the rooms on it, on top of the holding faction's
    // design: first-class rooms larger, emptier and decorated; crew rooms smaller, cluttered
    // and with a staff door. LevelBuildSettings lists one per side; a side without one changes
    // nothing.
    [CreateAssetMenu(fileName = "ShipSideDesign", menuName = "CGD/Level/Ship Side Design")]
    public class ShipSideDesign : ScriptableObject
    {
        [SerializeField] private ShipSide _side;
        [Tooltip("Multiplies a single-cell room's width and depth (after the faction's scale); multi-cell halls and landmarks keep theirs")]
        [SerializeField, Range(0.5f, 1.5f)] private float _roomSizeScale = 1f;
        [Tooltip("Multiplies the share of the floor props may take")]
        [SerializeField, Range(0.5f, 2f)] private float _fillShareScale = 1f;
        [Tooltip("Multiplies how many of every prop a room gets (required ones excepted)")]
        [SerializeField, Min(0.1f)] private float _propScale = 1f;
        [Tooltip("Placed after the function's furniture, before the room type's cover: rugs and planters in first class, a staff door in crew rooms")]
        [SerializeField] private RoomDressing _dressing;

        public ShipSide     Side           => _side;
        public float        RoomSizeScale  => _roomSizeScale;
        public float        FillShareScale => _fillShareScale;
        public float        PropScale      => _propScale;
        public RoomDressing Dressing       => _dressing;
    }
}
