using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Core;
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
        private const float GuardInterval = 1f;
        private const float NavMeshSearch = 2f;

        private bool  _entered;
        private float _nextEntryCheck;
        private int   _rewards;
        private float _nextGuardCheck;
        private NavMeshPath _path;
        private readonly List<GuardedGroup> _guarded = new();

        // A group the sealed room is waiting on, and the failsafe watching it.
        private sealed class GuardedGroup
        {
            public GuardedGroup(EnemyGroup group, SealFailsafe<HealthManager> failsafe)
            {
                Group    = group;
                Failsafe = failsafe;
            }

            public EnemyGroup Group { get; }
            public SealFailsafe<HealthManager> Failsafe { get; }
        }

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
            if (_guarded.Count > 0) TickGuards();

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
                Guard(group);
                wave++;
                if (waves > 1) Notify($"Wave {wave}/{waves}", NotificationStyle.Warning);

                if (group.IsCleared) Next();
                else group.Cleared += Next;
            }
            Next();
        }

        // --- Sealed-room failsafe ----------------------------------------------------------

        // Watches a group the sealed room waits on, so an enemy that can't be reached (or a
        // fight that goes nowhere) can't keep the doors shut for good: see SealFailsafe and
        // EncounterSettings › Sealed rooms. Call it once the group is filled.
        protected void Guard(EnemyGroup group)
        {
            if (group.IsCleared) return;

            var guarded = new GuardedGroup(group, new SealFailsafe<HealthManager>(Context.Settings.StrandedSeconds, Context.Settings.StallSeconds));
            _guarded.Add(guarded);
            group.Cleared += () => _guarded.Remove(guarded);
        }

        private void TickGuards()
        {
            if (Time.time < _nextGuardCheck) return;
            _nextGuardCheck = Time.time + GuardInterval;
            if (!Context.Seal.IsSealed) return;   // only a sealed room can trap anyone

            for (int i = _guarded.Count - 1; i >= 0; i--)
            {
                if (i >= _guarded.Count) continue;   // a release below ended a group and started the next

                GuardedGroup guarded = _guarded[i];
                foreach (HealthManager enemy in guarded.Failsafe.Advance(GuardInterval, guarded.Group.Members, IsStranded))
                    guarded.Group.Release(enemy);
                if (guarded.Failsafe.Overridden) Notify("Override — the room lets go of what's left", NotificationStyle.Warning);
            }
        }

        // Out of the room, off the NavMesh, or with no way to walk there from the player.
        private bool IsStranded(HealthManager enemy)
        {
            Vector3 at = enemy.transform.position;
            if (!Context.Floor.Contains(at)) return true;
            if (Context.Player == null || !NavMesh.SamplePosition(Context.Player.position, out NavMeshHit from, NavMeshSearch, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(at, out NavMeshHit to, NavMeshSearch, NavMesh.AllAreas)) return true;

            _path ??= new NavMeshPath();
            return !NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, _path) || _path.status != NavMeshPathStatus.PathComplete;
        }

        protected List<HealthManager> SpawnWave(bool fromSecondRoster = false) =>
            Context.Spawner.Spawn(Context.WaveSize, fromSecondRoster);

        protected GameObject SpawnReward(Vector3 position, float extraLuck = 0f)
        {
            if (Context.Settings.RewardPrefab == null) return null;

            GameObject reward = Instantiate(Context.Settings.RewardPrefab, position, Quaternion.Euler(0f, Context.Random.Range(0, 360), 0f), Context.Level);
            ISeedable.SeedAll(reward, Context.RewardSeed.Derive(_rewards++));
            float luck = Context.LootLuck + extraLuck;
            if (luck > 0f && reward.TryGetComponent(out LootDropper dropper)) dropper.AddLuck(luck);
            return reward;
        }

        protected static void Notify(string message, NotificationStyle style = NotificationStyle.Info) =>
            FeedbackBus.Notify(message, style);
    }
}
