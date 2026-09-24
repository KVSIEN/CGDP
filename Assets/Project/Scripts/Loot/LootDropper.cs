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
    public class LootDropper : MonoBehaviour
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

        private void Awake()
        {
            if (_dropOnDeath && TryGetComponent(out _health)) _health.OnDeath += Drop;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnDeath -= Drop;
        }

        public void Drop()
        {
            if (_table == null) return;

            _drops.Clear();
            _table.Roll(_drops, _luck);

            foreach (LootDrop drop in _drops)
                Spawn(drop, DropPosition());

            _drops.Clear();
        }

        private void Spawn(in LootDrop drop, Vector3 position)
        {
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            switch (drop.Kind)
            {
                case LootDropKind.Prefab:
                    PrefabPool.Spawn(drop.Prefab, position, rotation);
                    break;

                case LootDropKind.Instance when drop.Instance is WeaponInstance weapon:
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

        private Vector3 DropPosition()
        {
            Vector2 scatter = Random.insideUnitCircle * _scatterRadius;
            Vector3 point   = transform.TransformPoint(_dropOffset) + new Vector3(scatter.x, 0f, scatter.y);

            return Physics.Raycast(point, Vector3.down, out RaycastHit hit, GroundProbeDistance, _groundMask, QueryTriggerInteraction.Ignore)
                ? hit.point + Vector3.up * _groundClearance
                : point;
        }
    }
}
