using System;
using UnityEngine;

namespace CGD.Map
{
    // How often and where the generator may place one node type.
    [Serializable]
    public class MapNodeTypeRule
    {
        [SerializeField] private MapNodeType _type = MapNodeType.Combat;

        [Header("Count")]
        [SerializeField, Min(0)] private int _min;
        [SerializeField, Min(0)] private int _max = 99;
        [Tooltip("Relative chance when filling nodes beyond the minimums")]
        [SerializeField, Min(0f)] private float _weight = 1f;

        [Header("Placement")]
        [SerializeField] private MapPlacement _placement = MapPlacement.Anywhere;
        [Tooltip("Allowed depth, 0 = Start, 1 = Boss")]
        [SerializeField, Range(0f, 1f)] private float _minDepth;
        [SerializeField, Range(0f, 1f)] private float _maxDepth = 1f;
        [Tooltip("Place at dead ends first and weight them heavier — rewards for exploring")]
        [SerializeField] private bool _preferDeadEnds;
        [SerializeField] private bool _allowAdjacentSameType = true;
        [Tooltip("Fewest connections between two rooms of this type (0 = no limit). 3 keeps rewards spread out")]
        [SerializeField, Min(0)] private int _minSpacing;
        [Tooltip("Fewest connections a room needs to take this type (0 = any). 3 or more keeps it to forks, where routes split")]
        [SerializeField, Min(0)] private int _minConnections;
        [Tooltip("Prefer rooms with an empty grid cell beside them, so a room function spanning two cells (Cargo Bay, Park) has space to spread into")]
        [SerializeField] private bool _wantsSpace;

        [Header("Intensity")]
        [Tooltip("Added to the depth curve's intensity for this type")]
        [SerializeField, Range(-1f, 1f)] private float _intensityBonus;

        // Used by the Inspector when a rule is added to the list.
        public MapNodeTypeRule() { }

        public MapNodeTypeRule(MapNodeType type, int min, int max, float weight,
                               MapPlacement placement = MapPlacement.Anywhere,
                               float minDepth = 0f, float maxDepth = 1f,
                               bool preferDeadEnds = false, bool allowAdjacentSameType = true,
                               float intensityBonus = 0f, int minSpacing = 0, bool wantsSpace = false, int minConnections = 0)
        {
            _type                  = type;
            _min                   = min;
            _max                   = max;
            _weight                = weight;
            _placement             = placement;
            _minDepth              = minDepth;
            _maxDepth              = maxDepth;
            _preferDeadEnds        = preferDeadEnds;
            _allowAdjacentSameType = allowAdjacentSameType;
            _intensityBonus        = intensityBonus;
            _minSpacing            = minSpacing;
            _wantsSpace            = wantsSpace;
            _minConnections        = minConnections;
        }

        public MapNodeType Type                  => _type;
        public int         Min                   => _min;
        public int         Max                   => Mathf.Max(_min, _max);
        public float       Weight                => _weight;
        public MapPlacement Placement            => _placement;
        public bool        PreferDeadEnds        => _preferDeadEnds;
        public bool        AllowAdjacentSameType => _allowAdjacentSameType;
        public float       IntensityBonus        => _intensityBonus;
        public int         MinSpacing            => _minSpacing;
        public bool        WantsSpace            => _wantsSpace;
        public int         MinConnections        => _minConnections;

        public bool AllowsDepth(float progress) => progress >= _minDepth && progress <= _maxDepth;

        public bool AllowsPlacement(bool onMainPath) => _placement switch
        {
            MapPlacement.MainPathOnly => onMainPath,
            MapPlacement.BranchOnly   => !onMainPath,
            _                         => true
        };

        public bool Allows(float progress, bool onMainPath) => AllowsDepth(progress) && AllowsPlacement(onMainPath);
    }
}
