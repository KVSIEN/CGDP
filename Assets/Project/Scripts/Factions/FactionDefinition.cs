using System;
using UnityEngine;

namespace CGD.Factions
{
    // One of the realities the anomaly warped the ship into. Every room of a generated map
    // belongs to one (MapFactionMix decides how they're split); it sets the room's look
    // and whose enemies spawn there. What the room is — a fight, loot, a resupply stop —
    // and how many enemies it has still come from its room type, so a faction room can be
    // entirely peaceful.
    [CreateAssetMenu(fileName = "Faction", menuName = "CGD/Factions/Faction")]
    public class FactionDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Faction";
        [Tooltip("Map legend and room tint")]
        [SerializeField] private Color _color = Color.white;
        [Tooltip("How strongly owned rooms' floors take on the colour (0 = untinted)")]
        [SerializeField, Range(0f, 1f)] private float _floorTint = 0.3f;
        [Tooltip("Enemy prefabs (each needs EnemyAI) spawned in rooms this faction owns, instead of the room type's own; the room type still sets how many (none in peaceful rooms). Empty = the room type's enemies")]
        [SerializeField] private GameObject[] _enemies = Array.Empty<GameObject>();

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Color  Color       => _color;
        public float  FloorTint   => _floorTint;
        public GameObject[] Enemies => _enemies;
    }
}
