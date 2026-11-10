using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Interaction;
using CGD.Stealth;
using CGD.Targeting;
using CGD.Weapons;

namespace CGD.Enemies
{
    // A Tech survey beacon (SurveyBeaconAbility). Each pulse it follows one rule:
    //   hostiles in range  strip their stealth for a moment and report the nearest one to
    //                      every kin within ReportRange, through walls (a Union's shared picture)
    //   none               top up the shields of allies standing near it
    // It belongs to the deployer's side: anyone else can shoot it down. A player who holds
    // Interact on it spoofs it: from then on it reports hostiles somewhere they are not, and
    // reveals no one.
    [RequireComponent(typeof(DeployableHealth))]
    public class SurveyBeacon : MonoBehaviour, IInteractable, IPoolable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<HealthManager> _found = new();
        private DeployableHealth _health;
        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private SurveyBeaconAbility _settings;
        private Team _team;
        private float _age;
        private float _nextPulse;
        private bool _spoofed;
        private Vector3 _falseReport;

        private void Awake()
        {
            _health   = GetComponent<DeployableHealth>();
            _renderer = GetComponentInChildren<Renderer>();
            _block    = new MaterialPropertyBlock();
            _health.OnDeath += OnBroken;
        }

        private void OnDestroy() => _health.OnDeath -= OnBroken;

        public void Deploy(SurveyBeaconAbility settings, Team team)
        {
            _settings  = settings;
            _team      = team;
            _age       = 0f;
            _nextPulse = 0.5f;
            _spoofed   = false;
            _health.Deploy(team);
            Tint(settings.Color);
            Flash(1f);
        }

        void IPoolable.OnDespawned() => _settings = null;

        private void Update()
        {
            if (_settings == null || _health.IsDead) return;

            _age += Time.deltaTime;
            if (_age >= _settings.Lifetime)
            {
                ShutDown();
                return;
            }
            if (_age < _nextPulse) return;

            _nextPulse = _age + _settings.PulseInterval;
            Pulse();
        }

        private void Pulse()
        {
            var hostiles = new TargetFilter(TargetAffiliation.Enemies, _team, _health, _settings.TargetMask);
            TargetQuery.Sphere(transform.position, _settings.ScanRadius, hostiles, _found);
            Flash(_settings.ScanRadius);

            if (_found.Count > 0) Scan();
            else                  Repair();
        }

        private void Scan()
        {
            TargetQuery.SortByDistance(_found, transform.position);
            Vector3 reported = _spoofed ? _falseReport : _found[0].transform.position;

            if (!_spoofed)
                foreach (HealthManager hostile in _found)
                    if (Stealthable.TryGet(hostile, out Stealthable stealth)) stealth.Reveal(_settings.RevealSeconds);

            EnemyCallouts.Raise(new EnemyCallout(null, transform.position, reported, _team, _settings.ReportRange, throughWalls: true));
        }

        private void Repair()
        {
            float range = _settings.RepairRadius;
            foreach (EnemyAI ally in EnemyRoster.All)
            {
                if (ally.Health.Team != _team || ally.Health.IsDead) continue;
                if ((ally.transform.position - transform.position).sqrMagnitude > range * range) continue;
                ally.Health.RestoreShield(_settings.RepairPerPulse);
            }
        }

        private void OnBroken()
        {
            if (_settings == null) return;
            Flash(1.5f);
            ShutDown();
        }

        private void ShutDown()
        {
            _settings = null;
            PrefabPool.Release(gameObject);
        }

        private void Flash(float radius)
        {
            if (_settings.PulseFlash == null) return;

            GameObject flash = PrefabPool.Spawn(_settings.PulseFlash, transform.position + Vector3.up * 0.6f, Quaternion.identity);
            if (flash.TryGetComponent(out ExplosionFlash ring)) ring.Play(radius, _spoofed ? Color.gray : _settings.Color);
        }

        private void Tint(Color color)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _renderer.SetPropertyBlock(_block);
        }

        // --- Spoofing (the player's exploit) -------------------------------------------------

        public float HoldDuration => _settings != null ? _settings.SpoofHoldSeconds : 0f;

        public string GetInteractLabel(GameObject interactor) => "Spoof Beacon";

        public bool CanInteract(GameObject interactor) =>
            _settings != null && !_spoofed && !_health.IsDead && _settings.SpoofHoldSeconds > 0f;

        // From now on it points its side away from whoever tampered with it.
        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;

            Vector3 away = transform.position - interactor.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = transform.forward;
            _falseReport = transform.position + away.normalized * _settings.SpoofOffset;
            _spoofed = true;
            Tint(Color.gray);
        }
    }
}
