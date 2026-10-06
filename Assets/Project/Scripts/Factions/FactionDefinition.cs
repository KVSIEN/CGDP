using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        [Tooltip("Tier 1 (rank and file) enemy prefabs (each needs EnemyAI) spawned in rooms this faction owns, instead of the room type's own; the room type still sets how many (none in peaceful rooms) and how often higher tiers turn up. Empty = the room type's enemies")]
        [FormerlySerializedAs("_enemies")]
        [SerializeField] private GameObject[] _tier1 = Array.Empty<GameObject>();
        [Tooltip("Tier 2: tougher enemies that roll random affixes")]
        [SerializeField] private GameObject[] _tier2 = Array.Empty<GameObject>();
        [Tooltip("Tier 3: the faction's strongest regular enemies, with more affixes")]
        [SerializeField] private GameObject[] _tier3 = Array.Empty<GameObject>();

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Color  Color       => _color;
        public float  FloorTint   => _floorTint;
        // Tier 1, the faction's everyday roster.
        public GameObject[] Enemies => _tier1;

        // The roster of `tier` (1–3), falling back to the next tier down when it's empty.
        public GameObject[] EnemiesOfTier(int tier)
        {
            if (tier >= 3 && _tier3.Length > 0) return _tier3;
            if (tier >= 2 && _tier2.Length > 0) return _tier2;
            return _tier1;
        }
    }
}
