using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Interaction;
using CGD.Weapons;

namespace CGD.Loot
{
    // Turns a LootTable roll into world pickups around this object. Drops automatically
    // when a HealthManager on the same object dies (enemies, destructible crates), or on
    // request via Drop() (chests, quest rewards, scripted events).
    //
    // Stackables and non-weapon gear spawn as ItemPickups, weapons as WeaponPickups, and
    // prefab entries as themselves — all through PrefabPool.
    //
    // Given a seed (a generated level hands one to everything it places or spawns), each
    // drop rolls from it — the same seed drops the same loot in the same spots. Without
    // one every drop is random. A pooled enemy's seed is cleared when it goes back.
    public class LootDropper : MonoBehaviour, ISeedable, IPoolable
    {
        [SerializeField] private LootTable _table;
        [Tooltip("Drop when the HealthManager on this object dies")]
        [SerializeField] private bool _dropOnDeath = true;
        [Tooltip("Skews the table toward fuller, rarer drops (0 = authored odds)")]
        [SerializeField, Min(0f)] private float _luck;

        [Header("Pickups")]
        [SerializeField] private ItemPickup   _itemPickupPrefab;
        [SerializeField] private WeaponPickup _weaponPickupPrefab;

        [Header("Placement")]
        [Tooltip("Where drops appear, relative to this object")]
        [SerializeField] private Vector3 _dropOffset = new(0f, 1f, 0f);
        [Tooltip("Drops are scattered within this radius so they don't stack on one spot")]
        [SerializeField, Min(0f)] private float _scatterRadius = 0.8f;
        [Tooltip("Drops are settled onto this geometry below the drop point")]
        [SerializeField] private LayerMask _groundMask = 1; // Default layer
        [Tooltip("Height above the ground a settled pickup rests at")]
        [SerializeField, Min(0f)] private float _groundClearance = 0.3f;

        private const float GroundProbeDistance = 5f;

        private readonly List<LootDrop> _drops = new();
        private HealthManager _health;
        private Seed? _seed;
        private int   _dropCount;

        private void Awake()
        {
            if (_dropOnDeath && TryGetComponent(out _health)) _health.OnDeath += Drop;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDeath -= Drop;
        }

        // Run-wide bonuses (a warning's loot luck) stack on top of the authored luck.
        public void AddLuck(float amount) => _luck = Mathf.Max(0f, _luck + amount);

        public void ApplySeed(Seed seed)
        {
            _seed      = seed;
            _dropCount = 0;
        }

        public void OnDespawned() => _seed = null;

        public void Drop()
        {
            if (_table == null) return;

            // Each drop its own child, so a dropper that drops twice doesn't repeat itself.
            Seed seed = _seed?.Derive(_dropCount++) ?? Seed.Random();
            _drops.Clear();
            _table.Roll(_drops, _luck, seed);

            RandomStream placement = seed.Derive("placement").Stream();
            foreach (LootDrop drop in _drops)
                Spawn(drop, DropPosition(placement), placement);

            _drops.Clear();
        }

        private void Spawn(in LootDrop drop, Vector3 position, RandomStream placement)
        {
            Quaternion rotation = Quaternion.Euler(0f, placement.Range(0f, 360f), 0f);

            switch (drop.Kind)
            {
                case LootDropKind.Prefab:
                    PrefabPool.Spawn(drop.Prefab, position, rotation);
                    break;

                case LootDropKind.Instance when drop.Instance is WeaponItem weapon:
                    if (_weaponPickupPrefab == null) return;
                    PrefabPool.Spawn(_weaponPickupPrefab, position, rotation).SetWeapon(weapon);
                    break;

                case LootDropKind.Instance:
                    if (_itemPickupPrefab == null) return;
                    PrefabPool.Spawn(_itemPickupPrefab, position, rotation).SetInstance(drop.Instance);
                    break;

                case LootDropKind.Stack:
                    if (_itemPickupPrefab == null) return;
                    PrefabPool.Spawn(_itemPickupPrefab, position, rotation).SetStack(drop.Definition, drop.Count);
                    break;
            }
        }

        private Vector3 DropPosition(RandomStream placement)
        {
            Vector2 scatter = placement.InsideUnitCircle() * _scatterRadius;
            Vector3 point   = transform.TransformPoint(_dropOffset) + new Vector3(scatter.x, 0f, scatter.y);

            return Physics.Raycast(point, Vector3.down, out RaycastHit hit, GroundProbeDistance, _groundMask, QueryTriggerInteraction.Ignore)
                ? hit.point + Vector3.up * _groundClearance
                : point;
        }
    }
}
