using UnityEngine;
using CGD.Core;
using CGD.Feedback;
using CGD.Interaction;
using CGD.Items;
using CGD.Player;
using CGD.Weapons;

namespace CGD.Director
{
    // The director's helping hand: a few seconds into every calm after a peak (not the
    // session's opening calm), it checks what the player is low on — health without healing
    // in the pack, or spare ammo for the weapon in hand — and puts down the most needed
    // supply nearby, in sight where possible. At most one supply per calm, so it rewards
    // getting through a peak without replacing scavenging.
    public class SupplySpawner : MonoBehaviour
    {
        private const float PickupLift = 0.5f;

        [SerializeField] private SupplySettings   _settings;
        [SerializeField] private GameDirector     _director;
        [SerializeField] private PlayerHealth     _health;
        [SerializeField] private PlayerInventory  _inventory;
        [Tooltip("Optional — without it only healing is handed out")]
        [SerializeField] private WeaponController _weapons;
        [Tooltip("Geometry that blocks the player's view of a spot")]
        [SerializeField] private LayerMask        _obstacleMask = 1;

        private SupplyPlanner   _planner;
        private SpawnSpotFinder _spots;
        private bool            _pending;

        private void Awake()
        {
            if (_settings == null || _director == null || _health == null || _inventory == null || _settings.PickupPrefab == null)
            {
                Debug.LogError($"{name}: SupplySpawner needs SupplySettings (with a pickup prefab), the GameDirector, and the player's PlayerHealth and PlayerInventory.", this);
                enabled = false;
                return;
            }

            _planner = new SupplyPlanner(_settings.Needs);
            _spots   = new SpawnSpotFinder(Seed.Random().Stream(), _obstacleMask);
        }

        private void OnEnable()
        {
            if (_planner != null) _director.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (_planner != null) _director.PhaseChanged -= OnPhaseChanged;
        }

        // The opening calm enters Relax from itself; every later one follows a peak or a respawn.
        private void OnPhaseChanged(PacingPhase previous, PacingPhase next) =>
            _pending = next == PacingPhase.Relax && previous != PacingPhase.Relax;

        private void Update()
        {
            if (!_pending || _director.Phase != PacingPhase.Relax || _director.InCombat) return;
            if (_director.TimeInPhase < _settings.DelayIntoCalm || _health.IsDead) return;

            _pending = false;
            SupplyKind kind = _planner.Plan(CurrentNeeds());
            if (kind != SupplyKind.None) PutDown(kind);
        }

        private SupplyNeeds CurrentNeeds()
        {
            float healthShare = _health.MaxHealth > 0f ? _health.Health / _health.MaxHealth : 1f;
            int healing = _settings.HealingItem != null ? _inventory.Inventory.CountOf(_settings.HealingItem) : int.MaxValue;
            return new SupplyNeeds(healthShare, healing, ReserveMagazines());
        }

        private float ReserveMagazines()
        {
            if (_weapons == null || _weapons.Current == null || AmmoMunition() == null) return float.PositiveInfinity;
            return _weapons.Reserve / (float)_weapons.Current.MagazineSize;
        }

        private MunitionDefinition AmmoMunition() =>
            _weapons != null && _weapons.Data != null ? _settings.MunitionFor(_weapons.Data.AmmoType) : null;

        private void PutDown(SupplyKind kind)
        {
            if (!TryPickSupply(kind, out ItemDefinition item, out int count)) return;

            Vector3 player = _health.transform.position;
            if (!_spots.TryFind(player, _settings.Distance, _settings.SpotAttempts, SpotVisibility.InSight, out Vector3 spot) &&
                !_spots.TryFind(player, _settings.Distance, _settings.SpotAttempts, SpotVisibility.Any, out spot)) return;

            ItemPickup pickup = PrefabPool.Spawn(_settings.PickupPrefab, spot + Vector3.up * PickupLift, Quaternion.identity);
            pickup.SetStack(item, count);
            if (!string.IsNullOrEmpty(_settings.Message)) FeedbackBus.Notify(_settings.Message, NotificationStyle.Info);
        }

        private bool TryPickSupply(SupplyKind kind, out ItemDefinition item, out int count)
        {
            if (kind == SupplyKind.Healing)
            {
                item  = _settings.HealingItem;
                count = _settings.HealingCount;
                return item != null;
            }

            item  = AmmoMunition();
            count = item != null ? Mathf.CeilToInt(_weapons.Current.MagazineSize * _settings.AmmoMagazines) : 0;
            return item != null;
        }
    }
}
