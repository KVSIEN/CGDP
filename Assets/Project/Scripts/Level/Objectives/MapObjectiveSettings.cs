using System;
using UnityEngine;
using CGD.Core;
using CGD.Interaction;
using CGD.Items;

namespace CGD.Level
{
    // How a generated level hands out objectives: how many, from which templates, what the
    // steps place in the world, and what side objectives pay (scaling with room tier).
    // Main objectives lock the Boss room until they're all done.
    [CreateAssetMenu(fileName = "MapObjectiveSettings", menuName = "CGD/Level/Map Objective Settings")]
    public class MapObjectiveSettings : ScriptableObject
    {
        [SerializeField] private MapObjectiveTemplate[] _templates = Array.Empty<MapObjectiveTemplate>();
        [Tooltip("Main objectives per map; all of them must be done to open the Boss room")]
        [SerializeField] private IntRange _mainCount = new(1, 2);
        [Tooltip("Optional side objectives per map, for rewards")]
        [SerializeField] private IntRange _sideCount = new(0, 3);

        [Header("World")]
        [Tooltip("Door on the Boss room's connections: a Door whose Condition is a ConditionLock. Empty = the build settings' terminal door")]
        [SerializeField] private GameObject _bossDoorPrefab;
        [Tooltip("Activate steps: a console to switch on (a LockTerminal)")]
        [SerializeField] private LockTerminal _consolePrefab;
        [Tooltip("Retrieve steps: the pickup placed in the room, holding Retrieve Item")]
        [SerializeField] private ItemPickup _retrievePickupPrefab;
        [SerializeField] private ItemDefinition _retrieveItem;
        [Tooltip("Marks the rooms of active steps on the world map and minimap")]
        [SerializeField] private Color _markerColor = new(1f, 0.85f, 0.2f, 1f);

        [Header("Side rewards (per highest room tier 1 / 2 / 3)")]
        [Tooltip("Spawned in the last step's room when a side objective is done (a loot container)")]
        [SerializeField] private GameObject _rewardCachePrefab;
        [SerializeField] private float[] _cacheLuckByTier = { 0f, 0.75f, 1.5f };
        [Tooltip("Paid into the inventory on completion, e.g. credits")]
        [SerializeField] private ItemDefinition _currency;
        [SerializeField] private int[] _currencyByTier = { 25, 50, 100 };

        public MapObjectiveTemplate[] Templates => _templates;
        public IntRange MainCount => _mainCount;
        public IntRange SideCount => _sideCount;
        public GameObject     BossDoorPrefab       => _bossDoorPrefab;
        public LockTerminal   ConsolePrefab        => _consolePrefab;
        public ItemPickup     RetrievePickupPrefab => _retrievePickupPrefab;
        public ItemDefinition RetrieveItem         => _retrieveItem;
        public Color          MarkerColor          => _markerColor;
        public GameObject     RewardCachePrefab    => _rewardCachePrefab;
        public ItemDefinition Currency             => _currency;

        public float CacheLuckFor(int tier) => ByTier(_cacheLuckByTier, tier);
        public int   CurrencyFor(int tier)  => _currencyByTier.Length == 0 ? 0 : _currencyByTier[Mathf.Clamp(tier - 1, 0, _currencyByTier.Length - 1)];

        private static float ByTier(float[] values, int tier) =>
            values.Length == 0 ? 0f : values[Mathf.Clamp(tier - 1, 0, values.Length - 1)];
    }
}
