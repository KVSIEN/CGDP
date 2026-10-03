using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Feedback;
using CGD.Loot;

namespace CGD.Level
{
    // Base for a room that plays out as an encounter. RoomEncounterBuilder adds one per
    // encounter room and hands it its context; the encounter notices the player stepping
    // inside (clear of the doorways) and runs its own rules from there. The shared steps —
    // sealing the room, running waves, paying out — live here.
    public abstract class RoomEncounter : MonoBehaviour
    {
        private const float EntryCheckInterval = 0.2f;

        private bool  _entered;
        private float _nextEntryCheck;

        protected EncounterContext Context { get; private set; }

        public void Begin(EncounterContext context)
        {
            Context = context;
            OnBegin();
        }

        // Set up what the room shows before the player arrives (terminals, caches).
        protected virtual void OnBegin() { }

        protected virtual void OnPlayerEntered() { }

        // Per-frame work once begun (timers, ticking hazards).
        protected virtual void Tick() { }

        private void Update()
        {
            if (Context == null) return;
            Tick();

            if (_entered || Context.Player == null || Time.time < _nextEntryCheck) return;
            _nextEntryCheck = Time.time + EntryCheckInterval;
            if (!Context.Area.Contains(Context.Player.position)) return;

            _entered = true;
            OnPlayerEntered();
        }

        protected bool PlayerInside => Context.Player != null && Context.Area.Contains(Context.Player.position);

        // Spawns `waves` waves one after another, each once the last is dead, then calls
        // `onDone`. `spawnWave(index)` brings in one wave.
        protected void RunWaves(int waves, Func<int, List<HealthManager>> spawnWave, Action onDone)
        {
            int wave = 0;
            void Next()
            {
                if (wave >= waves)
                {
                    onDone();
                    return;
                }

                var group = new EnemyGroup();
                group.AddRange(spawnWave(wave));
                wave++;
                if (waves > 1) Notify($"Wave {wave}/{waves}", NotificationStyle.Warning);

                if (group.IsCleared) Next();
                else group.Cleared += Next;
            }
            Next();
        }

        protected List<HealthManager> SpawnWave(bool fromSecondRoster = false) =>
            Context.Spawner.Spawn(Context.WaveSize, fromSecondRoster);

        protected GameObject SpawnReward(Vector3 position, float extraLuck = 0f)
        {
            if (Context.Settings.RewardPrefab == null) return null;

            GameObject reward = Instantiate(Context.Settings.RewardPrefab, position, Quaternion.Euler(0f, Context.Random.Range(0, 360), 0f), Context.Level);
            float luck = Context.LootLuck + extraLuck;
            if (luck > 0f && reward.TryGetComponent(out LootDropper dropper)) dropper.AddLuck(luck);
            return reward;
        }

        protected static void Notify(string message, NotificationStyle style = NotificationStyle.Info) =>
            FeedbackBus.Notify(message, style);
    }
}
