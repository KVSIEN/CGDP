using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // How nature overgrows a room's floor: a low drift of dirt, moss and roots lying on the floor
    // the room already has, never a floor level of its own. It spreads out from a few patches
    // where the growth broke through, piles up against the walls, rolls in soft lumps, and keeps
    // to slopes anyone can walk, so players and enemies cross it as they would bare floor.
    // Doorways, stairs, ledges, solid furniture and the centre spot stay bare (OvergrowthGround).
    // A faction design names the profile its rooms grow (FactionDesign › Overgrowth).
    [CreateAssetMenu(fileName = "OvergrowthProfile", menuName = "CGD/Level/Overgrowth Profile")]
    public class OvergrowthProfile : ScriptableObject
    {
        // The most any profile may pile up: under a third of a floor level and well under the
        // lowest cover, so overgrowth never reads as a step, never hides anyone and never
        // counts as cover.
        public const float HighestAllowed = 0.4f;
        // The steepest any profile may slope: a gentle bank for the player and the NavMesh alike.
        public const float SteepestAllowed = 30f;

        [Header("Shape")]
        [Tooltip("Metres the overgrowth rises at its thickest")]
        [SerializeField, Range(0.05f, HighestAllowed)] private float _height = 0.3f;
        [Tooltip("The steepest it slopes anywhere, in degrees. Every bank, and the edge where it meets bare floor, is kept to this")]
        [SerializeField, Range(5f, SteepestAllowed)] private float _maxSlope = 20f;
        [Tooltip("Metres across its broad lumps: small for a bumpy, busy floor, large for slow rolling drifts")]
        [SerializeField, Range(1f, 12f)] private float _lumpSize = 5f;
        [Tooltip("Layers of finer lumps on the broad ones: 1 is smooth, 4 is rough and tangled")]
        [SerializeField, Range(1, 4)] private int _detail = 3;
        [Tooltip("How much the lumps twist and creep: 0 is plain noise, 1 runs it into winding, organic shapes")]
        [SerializeField, Range(0f, 1f)] private float _warp = 0.5f;

        [Header("Spread")]
        [Tooltip("Share of the floor it reaches: 0 keeps it to the patches' hearts, 1 covers nearly all the floor it may")]
        [SerializeField, Range(0f, 1f)] private float _coverage = 0.6f;
        [Tooltip("Places the growth broke through, each thickest at its heart")]
        [SerializeField] private IntRange _patches = new(2, 4);
        [Tooltip("Tiles from a patch's heart to where it thins out")]
        [SerializeField] private FloatRange _patchRadius = new(3f, 6f);
        [Tooltip("How much it gathers against the walls and in the corners, wherever the patches are")]
        [SerializeField, Range(0f, 1f)] private float _wallPile = 0.5f;
        [Tooltip("Keep the walkways from the doorways to the room's centre bare as well: a path trodden through the growth")]
        [SerializeField] private bool _clearWalkways;

        [Header("Look")]
        [Tooltip("The overgrowth's surface. Empty = the level's floor material")]
        [SerializeField] private Material _material;

        public float    Height       => _height;
        public float    MaxSlope     => _maxSlope;
        public float    LumpSize     => _lumpSize;
        public int      Detail       => _detail;
        public float    Warp         => _warp;
        public float    Coverage     => _coverage;
        public IntRange Patches      => _patches;
        public FloatRange PatchRadius => _patchRadius;
        public float    WallPile     => _wallPile;
        public bool     ClearWalkways => _clearWalkways;
        public Material Material     => _material;
    }
}
