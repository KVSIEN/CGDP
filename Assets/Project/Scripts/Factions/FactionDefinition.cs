using System;
using UnityEngine;
using UnityEngine.Serialization;
using CGD.Core;
using CGD.Enemies;
using CGD.Map;

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
        [Header("Fauna")]
        [Tooltip("Residents (each needs EnemyAI) of this faction's calm rooms: animals that graze and run, cleaning bots that roam. They are no part of a room's fight or its enemy count. Empty = none")]
        [SerializeField] private GameObject[] _fauna = Array.Empty<GameObject>();
        [Tooltip("Chance that a room of a fitting type holds a group of them")]
        [SerializeField, Range(0f, 1f)] private float _faunaChance = 0.4f;
        [Tooltip("How many in a group")]
        [SerializeField] private IntRange _faunaGroup = new(1, 3);
        [Tooltip("Room types that can hold fauna: the ones that are not a fight, a sealed encounter or a set piece")]
        [SerializeField] private MapNodeType[] _faunaRoomTypes =
        {
            MapNodeType.Combat, MapNodeType.Quiet, MapNodeType.Junction, MapNodeType.Resupply,
            MapNodeType.Workshop, MapNodeType.Treasure, MapNodeType.Event,
        };

        [Tooltip("Waits in the Boss room of a generated level when this faction holds it. Empty = the encounter settings' boss")]
        [SerializeField] private BossDefinition _boss;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Color  Color       => _color;
        public float  FloorTint   => _floorTint;
        public BossDefinition Boss => _boss;
        public GameObject[] Fauna => _fauna;
        public IntRange FaunaGroup => _faunaGroup;

        // Whether a room of this type may hold fauna, and how likely it does.
        public bool HoldsFauna(MapNodeType type) => _fauna.Length > 0 && System.Array.IndexOf(_faunaRoomTypes, type) >= 0;
        public float FaunaChance => _faunaChance;
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
