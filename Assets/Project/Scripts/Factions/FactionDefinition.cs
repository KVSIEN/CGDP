using System;
using UnityEngine;

namespace CGD.Factions
{
    // A group that holds territory on the ship. The map generator spreads each faction out
    // from an origin room; in a generated level the rooms it owns are fought over by its
    // own enemies and wear its colour.
    [CreateAssetMenu(fileName = "Faction", menuName = "CGD/Factions/Faction")]
    public class FactionDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Faction";
        [Tooltip("Map legend and room tint")]
        [SerializeField] private Color _color = Color.white;
        [Tooltip("How strongly owned rooms' floors take on the colour (0 = untinted)")]
        [SerializeField, Range(0f, 1f)] private float _floorTint = 0.3f;
        [Tooltip("Enemy prefabs (each needs EnemyAI) spawned in rooms this faction owns, instead of the room type's own. Empty = the room type's enemies")]
        [SerializeField] private GameObject[] _enemies = Array.Empty<GameObject>();

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Color  Color       => _color;
        public float  FloorTint   => _floorTint;
        public GameObject[] Enemies => _enemies;
    }
}
