using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // What the map director changes in a room of one beat (RoomBeat). Every value at 1 and no
    // supplies leaves the room as the rest of the generator makes it.
    [Serializable]
    public sealed class BeatTuning
    {
        // A beat with nothing changed, for rooms without a director or a tuning of their own.
        public static readonly BeatTuning Neutral = new();

        [SerializeField] private RoomBeat _beat;
        [Tooltip("Scales the room's size (single-cell rooms; never past the cell). Above 1 for an arena, below for a tighter room")]
        [SerializeField, Range(0.5f, 1.5f)] private float _sizeScale = 1f;
        [Tooltip("Scales how far the room's floor may see before the structure budget calls it open: below 1 asks for more walls and cover, above leaves the room easier to read")]
        [SerializeField, Range(0.5f, 2f)] private float _openRunScale = 1f;
        [Tooltip("Scales the structure the budget asks of the room for its size")]
        [SerializeField, Range(0.25f, 2f)] private float _structureScale = 1f;
        [Tooltip("Additions the structure budget may make here beyond its own limit, so a room asked for more cover can get it in full")]
        [SerializeField, Range(0, 6)] private int _extraAdditions;
        [Tooltip("Scales the enemies placed in the room (read at its intensity); a room that had enemies keeps at least one")]
        [SerializeField, Range(0f, 2f)] private float _enemyScale = 1f;
        [Tooltip("The director's reinforcements never arrive here, so the room stays calm while you are in it")]
        [SerializeField] private bool _noReinforcements;
        [Tooltip("One of these is put down in the room, in a nook against a wall (ammo before the boss, a reward behind the hardest fight). Empty = none")]
        [SerializeField] private GameObject[] _supplies = Array.Empty<GameObject>();

        public BeatTuning() { }

        public BeatTuning(RoomBeat beat, float sizeScale = 1f, float openRunScale = 1f, float structureScale = 1f, float enemyScale = 1f,
                          bool noReinforcements = false, GameObject[] supplies = null, int extraAdditions = 0)
        {
            _extraAdditions = extraAdditions;
            _beat = beat;
            _sizeScale = sizeScale;
            _openRunScale = openRunScale;
            _structureScale = structureScale;
            _enemyScale = enemyScale;
            _noReinforcements = noReinforcements;
            _supplies = supplies ?? Array.Empty<GameObject>();
        }

        public RoomBeat Beat => _beat;
        public float SizeScale => _sizeScale;
        public float EnemyScale => _enemyScale;
        public bool NoReinforcements => _noReinforcements;
        public IReadOnlyList<GameObject> Supplies => _supplies ?? Array.Empty<GameObject>();
        public bool HasSupplies => _supplies != null && Array.Exists(_supplies, s => s != null);

        public StructureTarget Structure => new(_openRunScale, _structureScale, _extraAdditions);

        // `count` enemies scaled, rounded; a room that had any keeps at least one, so a fight stays a fight.
        public int ScaleEnemies(int count)
        {
            if (count <= 0 || Mathf.Approximately(_enemyScale, 1f)) return count;
            return Mathf.Max(1, Mathf.RoundToInt(count * _enemyScale));
        }
    }
}
