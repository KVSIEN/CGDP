using System;
using UnityEngine;

namespace CGD.Enemies
{
    // A boss: which enemy it is, how much tougher than a regular one, how the fight escalates
    // and what beating it pays. A generated level's Boss room uses its faction's boss, else
    // the encounter settings' one, else a built-in boss made from the room's strongest enemy.
    [CreateAssetMenu(fileName = "Boss", menuName = "CGD/Enemies/Boss")]
    public class BossDefinition : ScriptableObject
    {
        [Tooltip("The boss's name on the HUD. Empty = the enemy's own name")]
        [SerializeField] private string _displayName;
        [Tooltip("An enemy prefab (EnemyAI + EnemyHealth). Empty = one of the boss room's tier 3 enemies")]
        [SerializeField] private GameObject _prefab;

        [Header("Toughness")]
        [Tooltip("× the enemy's health")]
        [SerializeField, Min(1f)] private float _healthMultiplier = 8f;
        [Tooltip("Flat armour added")]
        [SerializeField, Min(0f)] private float _armorBonus = 20f;
        [Tooltip("Flat shield added")]
        [SerializeField, Min(0f)] private float _shieldBonus;
        [Tooltip("× the enemy's size, so the boss reads as one at a glance")]
        [SerializeField, Range(1f, 2f)] private float _scale = 1.3f;

        [Header("Fight")]
        [Tooltip("How the fight escalates as the boss's health drops; any order")]
        [SerializeField] private BossPhase[] _phases =
        {
            new() { HealthBelow = 0.66f, Announcement = "It calls for help", DamageMultiplier = 1.15f, SpeedMultiplier = 1.05f, AttackRateMultiplier = 1.1f, Reinforcements = 2 },
            new() { HealthBelow = 0.33f, Announcement = "It's enraged",      DamageMultiplier = 1.3f,  SpeedMultiplier = 1.15f, AttackRateMultiplier = 1.3f, Reinforcements = 3 },
        };

        [Header("Reward")]
        [Tooltip("Reward caches dropped when the boss falls")]
        [SerializeField, Min(0)] private int _rewardCaches = 2;
        [Tooltip("Loot luck added to every one of them")]
        [SerializeField, Min(0f)] private float _rewardLuck = 2f;

        public GameObject Prefab       => _prefab;
        public float      Scale        => _scale;
        public int        RewardCaches => _rewardCaches;
        public float      RewardLuck   => _rewardLuck;

        // The built-in boss, with every value at its default.
        public static BossDefinition CreateDefault()
        {
            var boss = CreateInstance<BossDefinition>();
            boss.name = "DefaultBoss";
            return boss;
        }

        public string DisplayNameFor(EnemyData data) =>
            !string.IsNullOrEmpty(_displayName) ? _displayName : data.DisplayName;

        // A copy of the enemy's data made into the boss's. The caller owns (and destroys) it.
        public EnemyData CreateData(EnemyData enemy)
        {
            EnemyData boss = Instantiate(enemy);
            boss.MaxHealth *= _healthMultiplier;
            boss.Armor     += _armorBonus;
            boss.MaxShield += _shieldBonus;
            return boss;
        }

        // The phases from the first to start (highest threshold) to the last.
        public BossPhase[] PhasesInOrder()
        {
            var ordered = (BossPhase[])_phases.Clone();
            Array.Sort(ordered, (a, b) => b.HealthBelow.CompareTo(a.HealthBelow));
            return ordered;
        }
    }
}
