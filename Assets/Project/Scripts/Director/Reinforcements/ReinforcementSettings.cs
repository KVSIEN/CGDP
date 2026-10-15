using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Director
{
    // Tuning for the ReinforcementSpawner: who comes, how many, how often, from how far and
    // how the player is warned. Group size and interval are at neutral pressure; the
    // director's Pressure scales both.
    [CreateAssetMenu(fileName = "ReinforcementSettings", menuName = "CGD/Director/Reinforcement Settings")]
    public class ReinforcementSettings : ScriptableObject
    {
        [Tooltip("Enemy prefabs a group is picked from, one pick per enemy")]
        [SerializeField] private GameObject[] _enemies = Array.Empty<GameObject>();
        [Tooltip("Seconds between groups, counted only while the director lets pressure build")]
        [SerializeField] private FloatRange _interval = new(20f, 35f);
        [SerializeField] private IntRange _groupSize = new(2, 4);
        [Tooltip("Reinforcements alive at once; a group that would go over is cut short")]
        [SerializeField, Min(1)] private int _maxAlive = 6;
        [Tooltip("How far from the player (straight line) a group arrives")]
        [SerializeField] private FloatRange _distance = new(18f, 30f);
        [Tooltip("Spots tried per group before it is skipped (out of sight, on the NavMesh, with a path to the player)")]
        [SerializeField, Min(1)] private int _spotAttempts = 10;

        [Header("Arrival cue")]
        [Tooltip("Played where a group arrives, so the player hears where it comes from. Optional")]
        [SerializeField] private AudioClip _arrivalSound;
        [SerializeField, Range(0f, 1f)] private float _arrivalVolume = 1f;
        [Tooltip("Shown in the HUD feed when a group arrives. Empty = no message")]
        [SerializeField] private string _arrivalMessage = "Movement nearby";

        public GameObject[] Enemies        => _enemies;
        public FloatRange   Interval       => _interval;
        public IntRange     GroupSize      => _groupSize;
        public int          MaxAlive       => _maxAlive;
        public FloatRange   Distance       => _distance;
        public int          SpotAttempts   => _spotAttempts;
        public AudioClip    ArrivalSound   => _arrivalSound;
        public float        ArrivalVolume  => _arrivalVolume;
        public string       ArrivalMessage => _arrivalMessage;
    }
}
