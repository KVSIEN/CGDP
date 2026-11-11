using UnityEngine;
using CGD.Combat;
using CGD.Enemies;
using CGD.Expedition;
using CGD.Flow;
using CGD.Quests;
using CGD.Saving;

namespace CGD.Telemetry
{
    // Counts one run (RunStats) from the game's global broadcasts and, when the run settles,
    // adds it to the lifetime totals on disk. Sits next to the ExpeditionRunner; nothing else
    // needs to know it exists, except the end screen that shows its summary.
    public class RunStatsRecorder : MonoBehaviour
    {
        [Tooltip("The player's health: damage it takes counts as taken, hits by the player's team as dealt")]
        [SerializeField] private HealthManager _player;
        [SerializeField] private ExpeditionRunner _runner;
        [Tooltip("Off: the editor doesn't write lifetime totals. Builds always do")]
        [SerializeField] private bool _saveInEditor;
        [Tooltip("File in the persistent data folder. The editor uses its own copy (\"-editor\")")]
        [SerializeField] private string _fileName = "telemetry.json";

        private TelemetryStore _store;
        private bool _ended;

        public RunStats Current { get; } = new();
        // Totals including this run once it has ended; null while saving is off.
        public LifetimeStats Lifetime => _store?.Lifetime;
        public bool HasEnded => _ended;

        private bool SavingEnabled => !Application.isEditor || _saveInEditor;

        private void Awake()
        {
            if (!SavingEnabled) return;
            _store = new TelemetryStore(SaveFile.InPersistentData(_fileName));
            _store.Load();
        }

        private void OnEnable()
        {
            CombatEvents.DamageDealt += OnDamage;
            QuestEvents.Reported     += OnQuestEvent;
            if (_runner != null) _runner.RunEnded += OnRunEnded;
        }

        private void OnDisable()
        {
            CombatEvents.DamageDealt -= OnDamage;
            QuestEvents.Reported     -= OnQuestEvent;
            if (_runner != null) _runner.RunEnded -= OnRunEnded;
        }

        private void Update()
        {
            if (_ended) return;
            GameFlow flow = GameFlow.Instance;
            if (flow == null || flow.State == GameState.Playing) Current.AddTime(Time.deltaTime);
        }

        private void OnDamage(DamageReport report)
        {
            if (_ended) return;

            if (report.Target == _player)
            {
                Current.RecordTaken(report.Amount);
                return;
            }
            if (report.Source.Team != Team.Player) return;

            Current.RecordDealt(report.Amount, report.IsCritical, report.Killed, TierOf(report.Target));
        }

        private void OnQuestEvent(QuestEvent questEvent)
        {
            if (!_ended && questEvent.Kind == ObjectiveKind.Collect) Current.RecordCollected(questEvent.Amount);
        }

        private void OnRunEnded(RunReport report)
        {
            if (_ended) return;
            _ended = true;
            _store?.Record(Current, report.Outcome);
        }

        private static int TierOf(HealthManager target) =>
            target != null && target.TryGetComponent(out EnemyHealth enemy) && enemy.Data != null ? enemy.Data.Tier : 0;
    }
}
