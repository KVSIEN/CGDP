using System;
using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Enemies;
using CGD.Stats;

namespace CGD.Level
{
    // Tuning and prefabs for the rooms that play out as encounters (Lockdown, Holdout,
    // Ambush, Stealth, Rift, Puzzle, Hazard, Spawner, Shrine, Siege, Gauntlet, Haunt, Boss). Shared by every generated level.
    [CreateAssetMenu(fileName = "EncounterSettings", menuName = "CGD/Level/Encounter Settings")]
    public class EncounterSettings : ScriptableObject
    {
        [Header("Shared")]
        [Tooltip("A RoomShutter, placed in every doorway of an encounter room (origin at the doorway's centre on the floor, +Z out of the room)")]
        [SerializeField] private RoomShutter _shutterPrefab;
        [Tooltip("Spawned when an encounter pays out (a LootContainer with a LootDropper)")]
        [SerializeField] private GameObject _rewardPrefab;
        [Tooltip("A LockTerminal, used as the Holdout uplink and the Hazard vent controls")]
        [SerializeField] private GameObject _terminalPrefab;
        [Tooltip("Enemies per wave, read at the room's intensity")]
        [SerializeField] private IntRange _waveSize = new(2, 5);
        [Tooltip("Free spots set aside in each encounter room for enemies to arrive on")]
        [SerializeField, Min(1)] private int _spawnSpots = 8;

        [Header("Holdout")]
        [SerializeField, Min(5f)] private float _holdoutDuration = 75f;
        [SerializeField, Min(5f)] private float _holdoutWaveInterval = 18f;
        [Tooltip("A finished uplink downloads the ship's map: the whole world map is revealed")]
        [SerializeField] private bool _holdoutRevealsMap = true;

        [Tooltip("While the uplink runs, how often it sends out a signal that draws hostiles in earshot to the terminal (seconds)")]
        [SerializeField, Min(1f)] private float _holdoutPingInterval = 6f;
        [Tooltip("How far the uplink's signal carries (metres)")]
        [SerializeField, Min(1f)] private float _holdoutPingRadius = 30f;

        [Header("Sealed rooms")]
        [Tooltip("Seconds an enemy a sealed room is waiting on can stay out of reach (outside the room, off the NavMesh, no path to the player) before the room stops waiting on it")]
        [SerializeField, Min(5f)] private float _strandedSeconds = 20f;
        [Tooltip("Seconds a sealed room's fight may go with no kill before the room stops waiting on anyone and the doors open")]
        [SerializeField, Min(30f)] private float _stallSeconds = 150f;

        [Header("Ambush & Rift")]
        [SerializeField, Min(1)] private int _ambushWaves = 2;
        [Tooltip("Waves alternate between the room's two realities; the last brings both")]
        [SerializeField, Min(1)] private int _riftWaves = 3;

        [Header("Spawner")]
        [Tooltip("The structure in the middle of a Spawner room: an EnemySpawner with a HealthManager")]
        [SerializeField] private EnemySpawner _spawnerPrefab;
        [Tooltip("Seconds between batches while the spawner is below its cap")]
        [SerializeField, Min(0.5f)] private float _spawnerInterval = 8f;
        [SerializeField, Min(1)] private int _spawnerBatch = 2;
        [Tooltip("The spawner stops while this many of its enemies are alive, so the player can always out-kill it")]
        [SerializeField, Min(1)] private int _spawnerCap = 5;
        [Tooltip("The spawner only runs while the player is this close")]
        [SerializeField, Min(1f)] private float _spawnerActivationRange = 25f;
        [Tooltip("How much of their usual loot the spawner's enemies drop (0 = none): the reward for the room is " +
                 "the spawner's own, so an endless stream can't be farmed")]
        [SerializeField, Range(0f, 1f)] private float _spawnerEnemyLoot = 0f;

        [Header("Haunt")]
        [Tooltip("The Void entity that stalks a Haunt room (an enemy prefab with EnemyAI). Killing it only banishes it")]
        [SerializeField] private GameObject _hauntEntityPrefab;
        [Tooltip("What binds the entity to the room: a prefab with a HealthManager (e.g. Destructible) and a collider. Break them all to unbind it")]
        [SerializeField] private HealthManager _hauntAnchorPrefab;
        [SerializeField, Range(1, 4)] private int _hauntAnchors = 3;
        [Tooltip("Seconds a banished entity stays away before it comes back")]
        [SerializeField, Min(1f)] private float _hauntBanishSeconds = 25f;

        [Header("Shrine")]
        [Tooltip("An altar the player takes a boon from (ShrineAltar + a collider)")]
        [SerializeField] private ShrineAltar _altarPrefab;
        [Tooltip("The boons a shrine can offer; a blessing and a curse can share one (negative values)")]
        [SerializeField] private StatBoon[] _shrineBoons = Array.Empty<StatBoon>();
        [SerializeField, Min(1)] private int _shrineOffers = 3;

        [Header("Siege")]
        [Tooltip("The objective to defend, in the middle of the room (SiegeObjective)")]
        [SerializeField] private SiegeObjective _siegePrefab;
        [SerializeField, Min(5f)] private float _siegeDuration = 60f;
        [SerializeField, Min(5f)] private float _siegeWaveInterval = 15f;

        [Header("Gauntlet")]
        [SerializeField, Min(2)] private int _gauntletWaves = 4;
        [Tooltip("Enemies added to the wave size with every wave after the first")]
        [SerializeField, Min(0)] private int _gauntletWaveGrowth = 1;
        [Tooltip("Loot luck added to the reward for every wave survived")]
        [SerializeField, Min(0f)] private float _gauntletLuckPerWave = 0.5f;
        [Tooltip("One is laid on the player for each wave (empty = plain waves)")]
        [SerializeField] private StatBoon[] _gauntletTwists = Array.Empty<StatBoon>();

        [Header("Stealth")]
        [Tooltip("Extra loot luck on the vault while the alarm hasn't been raised")]
        [SerializeField, Min(0f)] private float _stealthBonusLuck = 2f;
        [SerializeField, Min(0)] private int _alarmWaves = 1;

        [Header("Puzzle")]
        [Tooltip("A PuzzleSwitch console")]
        [SerializeField] private GameObject _puzzleSwitchPrefab;
        [Tooltip("Blocks the doorways leading deeper until the puzzle is solved: a Door with a ConditionLock as its Condition")]
        [SerializeField] private GameObject _puzzleDoorPrefab;
        [SerializeField] private IntRange _puzzleNodes = new(4, 5);
        [SerializeField, Min(1)] private int _puzzleScramble = 2;

        [Header("Boss")]
        [Tooltip("The boss of a Boss room whose faction has none of its own. Empty = a built-in boss made from the room's strongest enemy")]
        [SerializeField] private BossDefinition _boss;

        [Header("Hazard")]
        [Tooltip("One is picked per Hazard room; it is applied to everyone inside on every tick")]
        [SerializeField] private StatusEffect[] _hazardEffects = Array.Empty<StatusEffect>();
        [SerializeField, Min(0.1f)] private float _hazardTick = 1f;
        [Tooltip("Strength the effect is applied with (its 'hit' damage)")]
        [SerializeField, Min(0f)] private float _hazardMagnitude = 8f;
        [Tooltip("Tinted with the effect's colour and laid over the room's floor while the leak runs")]
        [SerializeField] private Material _hazardMaterial;

        public RoomShutter ShutterPrefab => _shutterPrefab;
        public GameObject  RewardPrefab  => _rewardPrefab;
        public GameObject  TerminalPrefab => _terminalPrefab;
        public IntRange    WaveSize      => _waveSize;
        public int         SpawnSpots    => _spawnSpots;

        public float HoldoutDuration     => _holdoutDuration;
        public float HoldoutWaveInterval => _holdoutWaveInterval;
        public bool  HoldoutRevealsMap   => _holdoutRevealsMap;
        public float HoldoutPingInterval => _holdoutPingInterval;
        public float HoldoutPingRadius   => _holdoutPingRadius;

        public float StrandedSeconds => _strandedSeconds;
        public float StallSeconds    => _stallSeconds;

        public int AmbushWaves => _ambushWaves;
        public int RiftWaves   => _riftWaves;

        public EnemySpawner SpawnerPrefab          => _spawnerPrefab;
        public float        SpawnerInterval        => _spawnerInterval;
        public int          SpawnerBatch           => _spawnerBatch;
        public int          SpawnerCap             => _spawnerCap;
        public float        SpawnerActivationRange => _spawnerActivationRange;
        public float        SpawnerEnemyLoot       => _spawnerEnemyLoot;

        public GameObject    HauntEntityPrefab  => _hauntEntityPrefab;
        public HealthManager HauntAnchorPrefab  => _hauntAnchorPrefab;
        public int           HauntAnchors       => _hauntAnchors;
        public float         HauntBanishSeconds => _hauntBanishSeconds;

        public ShrineAltar ShrineAltarPrefab => _altarPrefab;
        public StatBoon[]  ShrineBoons       => _shrineBoons;
        public int         ShrineOffers      => _shrineOffers;

        public SiegeObjective SiegePrefab       => _siegePrefab;
        public float          SiegeDuration     => _siegeDuration;
        public float          SiegeWaveInterval => _siegeWaveInterval;

        public int        GauntletWaves        => _gauntletWaves;
        public int        GauntletWaveGrowth   => _gauntletWaveGrowth;
        public float      GauntletLuckPerWave  => _gauntletLuckPerWave;
        public StatBoon[] GauntletTwists       => _gauntletTwists;

        public float StealthBonusLuck => _stealthBonusLuck;
        public int   AlarmWaves       => _alarmWaves;

        public GameObject PuzzleSwitchPrefab => _puzzleSwitchPrefab;
        public GameObject PuzzleDoorPrefab   => _puzzleDoorPrefab;
        public IntRange   PuzzleNodes        => _puzzleNodes;
        public int        PuzzleScramble     => _puzzleScramble;

        public BossDefinition Boss => _boss;

        public StatusEffect[] HazardEffects   => _hazardEffects;
        public float          HazardTick      => _hazardTick;
        public float          HazardMagnitude => _hazardMagnitude;
        public Material       HazardMaterial  => _hazardMaterial;
    }
}
