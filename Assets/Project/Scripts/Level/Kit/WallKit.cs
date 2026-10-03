using UnityEngine;

namespace CGD.Level
{
    // The art for a level's walls, placed along LevelWallPlan's lines by WallKitPlanner so
    // generated levels can wear real art instead of grey boxes. The boxes stay as
    // invisible collision; kit pieces are visual only (no colliders needed).
    //
    // Piece conventions (straight and angled): origin on the floor at the start of the
    // wall line, +X along the wall, +Z toward the room; the wall stands behind its origin.
    // Pieces are authored PieceLength long and PieceHeight tall and stretched to fit.
    [CreateAssetMenu(fileName = "WallKit", menuName = "CGD/Level/Wall Kit")]
    public class WallKit : ScriptableObject
    {
        [Tooltip("For straight runs along the tile grid; repeated along each run")]
        [SerializeField] private GameObject _straight;
        [Tooltip("For bevels and curves (walls off the grid), one per wall piece. Empty = the straight piece")]
        [SerializeField] private GameObject _angled;
        [Tooltip("Upright at corners and wall ends (door jambs) that hides the seams. Origin on the floor; scaled to the wall's height")]
        [SerializeField] private GameObject _post;
        [Tooltip("Around every doorway, on the room side. Origin at the doorway's centre on the floor, +Z out of the room (like the door prefabs). Not scaled")]
        [SerializeField] private GameObject _doorFrame;

        [Header("Authored Size")]
        [Tooltip("Metres along the wall one piece covers")]
        [SerializeField, Min(0.1f)] private float _pieceLength = 3f;
        [Tooltip("Metres tall the pieces are modelled; they're scaled to each wall's height")]
        [SerializeField, Min(0.1f)] private float _pieceHeight = 4f;
        [Tooltip("Corners gentler than this (the bends of a rounded wall) get no post")]
        [SerializeField, Range(0f, 90f)] private float _postMinAngle = 30f;

        public GameObject Straight    => _straight;
        public GameObject Angled      => _angled != null ? _angled : _straight;
        public GameObject Post        => _post;
        public GameObject DoorFrame   => _doorFrame;
        public float      PieceLength => _pieceLength;
        public float      PieceHeight => _pieceHeight;
        public float      PostMinAngle => _postMinAngle;
    }
}
