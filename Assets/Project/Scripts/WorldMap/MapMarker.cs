using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;

namespace CGD.WorldMap
{
    // Puts this object on the minimap and world map: enemies, pickups, quest targets,
    // doors. Active markers register themselves, so maps just read MapMarker.Active.
    public class MapMarker : MonoBehaviour
    {
        private static readonly List<MapMarker> _active = new();

        [SerializeField] private MapMarkerShape _shape = MapMarkerShape.Circle;
        [SerializeField] private Color _color = Color.white;
        [SerializeField, Min(1f)] private float _size = 10f;

        [Header("Visibility")]
        [SerializeField] private bool _showOnMinimap  = true;
        [SerializeField] private bool _showOnWorldMap = true;
        [Tooltip("Hidden until the player has explored the spot it's in")]
        [SerializeField] private bool _requireExplored = true;
        [Tooltip("Kept on the minimap's edge when out of range — for objectives")]
        [SerializeField] private bool _clampToEdge;
        [Tooltip("Hidden once this object's HealthManager dies")]
        [SerializeField] private bool _hideWhenDead = true;

        private HealthManager _health;

        public static IReadOnlyList<MapMarker> Active => _active;

        public MapMarkerShape Shape          => _shape;
        public Color          Color          => _color;
        public float          Size           => _size;
        public bool           ShowOnMinimap  => _showOnMinimap;
        public bool           ShowOnWorldMap => _showOnWorldMap;
        public bool           ClampToEdge    => _clampToEdge;
        public Vector3        Position       => transform.position;
        public float          Heading        => MapProjection.Heading(transform);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _active.Clear();

        private void Awake() => TryGetComponent(out _health);

        private void OnEnable()  => _active.Add(this);
        private void OnDisable() => _active.Remove(this);

        public bool IsVisibleOn(WorldMapArea area)
        {
            if (_hideWhenDead && _health != null && _health.IsDead) return false;
            return !_requireExplored || area == null || area.IsExplored(Position);
        }
    }
}
