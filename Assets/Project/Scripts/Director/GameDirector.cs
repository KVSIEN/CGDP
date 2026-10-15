using System;
using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Flow;
using CGD.Player;

namespace CGD.Director
{
    // Paces one play session, after Left 4 Dead's AI Director. It watches how stressed the
    // player is — damage taken, enemies dying around them, dying — as an intensity from 0
    // to 1, and runs the pacing cycle (PacingPhase) from it: build pressure, hold the peak,
    // let it fade, then a calm stretch before the next build-up. How each peak went (the
    // lowest the player's health got, deaths) feeds a Pressure multiplier that rubber-bands
    // the session's difficulty, and shortens or lengthens the calms with it.
    //
    // The director decides *when* the world may push and *how hard*; the systems that act on
    // it (ReinforcementSpawner, SupplySpawner, DirectorMusic) ask AllowsPopulation, Phase,
    // Intensity and Pressure, or listen to PhaseChanged, and decide *how*. One per gameplay scene.
    public class GameDirector : MonoBehaviour
    {
        [SerializeField] private DirectorSettings _settings;
        [SerializeField] private PlayerHealth     _player;

        private IntensityMeter     _intensity;
        private PacingCycle        _pacing;
        private SessionPerformance _performance;
        private float              _lastCombatTime = float.NegativeInfinity;
        // The smallest share of health the player has been down to since the build-up began.
        private float              _lowestHealth = 1f;

        public float       Intensity   => _intensity?.Value ?? 0f;
        public PacingPhase Phase       => _pacing?.Phase ?? PacingPhase.Relax;
        public float       TimeInPhase => _pacing?.TimeInPhase ?? 0f;
        public bool        InCombat    => _settings != null && Time.time - _lastCombatTime < _settings.Stress.CombatMemory;
        // 1 at neutral; above 1 for a player who is cruising, below for one who is struggling.
        public float       Pressure    => _performance?.Pressure ?? 1f;

        // Whether the world may add threats right now: building up or at the peak, while playing.
        public bool AllowsPopulation => _pacing != null && _pacing.AllowsPopulation && IsPlaying;

        // (previous, next)
        public event Action<PacingPhase, PacingPhase> PhaseChanged;

        private static bool IsPlaying => GameFlow.Instance == null || GameFlow.Instance.State == GameState.Playing;

        private void Awake()
        {
            if (_settings == null || _player == null)
            {
                Debug.LogError($"{name}: GameDirector needs DirectorSettings and the player's PlayerHealth.", this);
                enabled = false;
                return;
            }

            _intensity   = new IntensityMeter(_settings.Intensity);
            _pacing      = new PacingCycle(_settings.Pacing, Seed.Random().Stream());
            _performance = new SessionPerformance(_settings.Performance);
            _pacing.PhaseChanged += OnPhaseChanged;
        }

        private void Start() => _pacing.Start();

        private void OnEnable()
        {
            if (_pacing == null) return;

            CombatEvents.DamageDealt += OnDamageDealt;
            _player.OnDeath   += OnPlayerDied;
            _player.OnRevived += OnPlayerRevived;
        }

        private void OnDisable()
        {
            if (_pacing == null) return;

            CombatEvents.DamageDealt -= OnDamageDealt;
            _player.OnDeath   -= OnPlayerDied;
            _player.OnRevived -= OnPlayerRevived;
        }

        private void Update()
        {
            if (!IsPlaying) return;

            float deltaTime = Time.deltaTime;
            _intensity.Tick(deltaTime, InCombat);
            _pacing.Tick(deltaTime, _intensity.Value);
        }

        private void OnDamageDealt(DamageReport report)
        {
            if (report.Target == _player)
            {
                _lastCombatTime = Time.time;
                if (_player.MaxHealth <= 0f) return;
                _intensity.Add(report.Amount / _player.MaxHealth * _settings.Stress.DamageTaken);
                _lowestHealth = Mathf.Min(_lowestHealth, HealthShare);
                return;
            }

            if (report.Source.Team == Team.Player) _lastCombatTime = Time.time;
            if (report.Killed && report.Target.Team != Team.Player) AddNearbyKillStress(report.Target.transform.position);
        }

        private void AddNearbyKillStress(Vector3 position)
        {
            StressTuning stress = _settings.Stress;
            float distance = Vector3.Distance(position, _player.transform.position);
            if (distance >= stress.NearbyKillRadius) return;

            _intensity.Add(stress.NearbyKill * (1f - distance / stress.NearbyKillRadius));
        }

        private float HealthShare => _player.MaxHealth > 0f ? _player.Health / _player.MaxHealth : 1f;

        private void OnPhaseChanged(PacingPhase previous, PacingPhase next)
        {
            if (next == PacingPhase.BuildUp) _lowestHealth = HealthShare;
            if (next == PacingPhase.PeakFade)
            {
                _performance.RecordPeak(_lowestHealth);
                UpdateRelaxScale();
            }
            PhaseChanged?.Invoke(previous, next);
        }

        private void UpdateRelaxScale() => _pacing.RelaxScale = 1f / _performance.Pressure;

        private void OnPlayerDied()
        {
            _intensity.Fill();
            _performance.RecordDeath();
            UpdateRelaxScale();
        }

        // A respawn is a fresh start: calm, with a full relax before anything comes again.
        private void OnPlayerRevived()
        {
            _intensity.Clear();
            _pacing.Restart();
        }
    }
}
