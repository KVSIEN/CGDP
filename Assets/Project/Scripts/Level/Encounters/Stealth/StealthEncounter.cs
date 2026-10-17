using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Enemies;
using CGD.Feedback;
using CGD.Loot;

namespace CGD.Level
{
    // Stealth: a guarded vault. While no guard has spotted the player, the vault holds extra
    // loot. The first guard to give chase raises the alarm: the room seals, the bonus is
    // gone, and the guards plus reinforcements must be beaten before the shutters open.
    public class StealthEncounter : RoomEncounter
    {
        private readonly List<(EnemyAI ai, Action<EnemyAI.AiState> handler)> _watched = new();
        private LootDropper   _vault;
        private LootContainer _vaultContainer;
        private bool _alarm;

        protected override void OnBegin()
        {
            GameObject vault = SpawnReward(Context.TakeSpot(), Context.Settings.StealthBonusLuck);
            if (vault != null)
            {
                vault.TryGetComponent(out _vault);
                vault.TryGetComponent(out _vaultContainer);
            }

            foreach (HealthManager guard in Context.RoomEnemies)
            {
                if (guard == null || !guard.TryGetComponent(out EnemyAI ai)) continue;
                void Handler(EnemyAI.AiState state)
                {
                    if (state == EnemyAI.AiState.Chase) RaiseAlarm();
                }
                ai.StateChanged += Handler;
                _watched.Add((ai, Handler));
            }
        }

        protected override void OnPlayerEntered() =>
            Notify("Restricted area — stay out of sight and the vault pays extra");

        private void OnDestroy() => StopWatching();

        private void StopWatching()
        {
            foreach (var (ai, handler) in _watched)
                if (ai != null) ai.StateChanged -= handler;
            _watched.Clear();
        }

        private void RaiseAlarm()
        {
            if (_alarm || !PlayerInside) return;
            _alarm = true;
            StopWatching();

            if (_vault != null && _vaultContainer != null && !_vaultContainer.IsOpened)
                _vault.AddLuck(-Context.Settings.StealthBonusLuck);

            Context.Seal.Seal();
            Notify("Alarm! The room is sealed", NotificationStyle.Danger);

            int waves = 1 + Context.Settings.AlarmWaves;
            RunWaves(waves, wave => wave == 0 ? LockdownEncounter.EnemiesInside(Context) : SpawnWave(), () =>
            {
                Context.Seal.Release();
                Notify("Alarm cleared", NotificationStyle.Success);
            });
        }
    }
}
