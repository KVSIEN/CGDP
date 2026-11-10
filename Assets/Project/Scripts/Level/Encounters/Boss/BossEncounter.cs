using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Core;
using CGD.Enemies;
using CGD.Feedback;

namespace CGD.Level
{
    // Boss: stepping in brings out the boss and seals the room — the only way on to the Exit
    // runs through it, so extraction opens only once the boss falls. The top of the HUD shows
    // its name, health and phase. As its health drops past each phase's threshold it hits
    // harder, speeds up and calls in help. Beating it opens the room and drops reward caches.
    public class BossEncounter : RoomEncounter
    {
        private const float SpawnSearch  = 4f;
        private const float RewardSpread = 1.5f;

        private BossDefinition  _definition;
        private bool            _ownsDefinition;
        private EnemyData       _data;
        private EnemyHealth     _boss;
        private BossPhase[]     _phases;
        private BossPhases      _progress;
        private ObjectiveStatus _status;

        protected override void OnBegin()
        {
            BossDefinition factionBoss = Context.Room.Faction != null ? Context.Room.Faction.Boss : null;
            _definition = factionBoss != null ? factionBoss : Context.Settings.Boss;
            if (_definition != null) return;

            _definition     = BossDefinition.CreateDefault();
            _ownsDefinition = true;
        }

        protected override void OnPlayerEntered()
        {
            _boss = SpawnBoss();
            if (_boss == null) return;   // nothing to fight: the room stays an open passage

            _phases   = _definition.PhasesInOrder();
            var thresholds = new float[_phases.Length];
            for (int i = 0; i < _phases.Length; i++) thresholds[i] = _phases[i].HealthBelow;
            _progress = new BossPhases(thresholds);

            _status = new ObjectiveStatus(_boss.DisplayName) { Health = _boss };
            ShowPhase();
            ObjectiveFeed.Show(_status);

            Context.Seal.Seal();
            Notify($"{_boss.DisplayName} — defeat it to reach extraction", NotificationStyle.Danger);

            _boss.OnChanged += CheckPhase;
            _boss.OnDeath   += Defeated;
            Hunt(_boss);
        }

        private EnemyHealth SpawnBoss()
        {
            GameObject prefab = _definition.Prefab != null ? _definition.Prefab : Context.Spawner.PickOfTier(3);
            if (prefab == null) return null;

            Vector3 spot = Context.Center;
            if (NavMesh.SamplePosition(spot, out NavMeshHit hit, SpawnSearch, NavMesh.AllAreas)) spot = hit.position;
            Vector3 toPlayer = Context.Player.position - spot;
            toPlayer.y = 0f;
            Quaternion facing = toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity;

            GameObject body = Instantiate(prefab, spot, facing, Context.Level);
            if (!body.TryGetComponent(out EnemyHealth health) || !body.TryGetComponent(out EnemyAI ai))
            {
                Debug.LogWarning($"{name}: boss prefab {prefab.name} needs EnemyHealth and EnemyAI.", prefab);
                Destroy(body);
                return null;
            }

            // Seeded first, so the boss builds on the affixes (and drops) its seed rolls.
            ISeedable.SeedAll(body, Context.RewardSeed.Derive("boss"));
            body.transform.localScale *= _definition.Scale;

            _data = _definition.CreateData(health.Data);
            ai.UseData(_data);
            health.UseData(_data);
            health.SetDisplayName(_definition.DisplayNameFor(_data));
            return health;
        }

        private void CheckPhase()
        {
            if (_boss.IsDead || _boss.MaxHealth <= 0f) return;

            int before = _progress.Current;
            if (_progress.Advance(_boss.Health / _boss.MaxHealth) == 0) return;

            for (int i = before; i < _progress.Current; i++) BeginPhase(_phases[i]);
            ShowPhase();
        }

        private void BeginPhase(BossPhase phase)
        {
            // _data is the boss's own copy, read live by its AI, so scaling it is the buff.
            phase.ApplyTo(_data);
            if (!string.IsNullOrEmpty(phase.Announcement)) Notify(phase.Announcement, NotificationStyle.Danger);

            if (phase.Reinforcements <= 0) return;
            List<HealthManager> help = Context.Spawner.Spawn(phase.Reinforcements);
            foreach (HealthManager enemy in help) Hunt(enemy);
        }

        private void ShowPhase() => _status.Detail = $"Phase {_progress.Current + 1}/{_progress.Count}";

        private void Hunt(HealthManager enemy)
        {
            if (Context.Player == null || !enemy.TryGetComponent(out EnemyAI ai)) return;
            ai.AlertTo(Context.Player.position, DamageSource.Of(Context.Player.gameObject));
        }

        private void Defeated()
        {
            string bossName = _boss.DisplayName;
            Unsubscribe();
            ObjectiveFeed.Clear(_status);
            Context.Seal.Release();
            Notify($"{bossName} defeated — the way to extraction is open", NotificationStyle.Success);

            int caches = _definition.RewardCaches;
            for (int i = 0; i < caches; i++)
            {
                float angle = i * Mathf.PI * 2f / Mathf.Max(1, caches);
                Vector3 offset = caches > 1 ? new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * RewardSpread : Vector3.zero;
                SpawnReward(Context.Center + offset, _definition.RewardLuck);
            }
        }

        private void Unsubscribe()
        {
            if (_boss == null) return;
            _boss.OnChanged -= CheckPhase;
            _boss.OnDeath   -= Defeated;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            ObjectiveFeed.Clear(_status);
            if (_data != null) Destroy(_data);
            if (_ownsDefinition && _definition != null) Destroy(_definition);
        }
    }
}
