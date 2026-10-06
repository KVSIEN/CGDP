using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Feedback;
using CGD.Interaction;
using CGD.Loot;
using CGD.Quests;
using CGD.WorldMap;

namespace CGD.Level
{
    // Runs a generated level's objectives: each becomes a quest in the player's log, its
    // steps are set up in their rooms when they become active (a console, an item, a room
    // to clear) and marked on the maps, main objectives open the Boss room's doors once all
    // are done, and side objectives pay out (currency, and a loot cache in the last room).
    // Added to the level by LevelBuilder.
    public class MapObjectiveRunner : MonoBehaviour
    {
        private const float MarkerSize = 14f;

        private class Step
        {
            public LevelRoom       Room;
            public ObjectiveAction Action;
            public QuestSignal     Signal;
            public GameObject      Marker;
            public bool            Done;
        }

        private class Running
        {
            public PlannedObjective Plan;
            public QuestDefinition  Quest;
            public readonly List<Step> Steps = new();
        }

        private readonly List<Running> _running = new();
        private readonly Queue<Step> _toArm = new();

        private MapObjectiveSettings _settings;
        private RoomPopulator _populator;
        private QuestTracker _tracker;
        private IReadOnlyList<ConditionLock> _bossLocks;
        private float _runLuck;

        public void Begin(IReadOnlyList<PlannedObjective> plan, IReadOnlyDictionary<int, LevelRoom> rooms, MapObjectiveSettings settings,
                          RoomPopulator populator, QuestTracker tracker, IReadOnlyList<ConditionLock> bossLocks, float runLuck)
        {
            _settings  = settings;
            _populator = populator;
            _tracker   = tracker;
            _bossLocks = bossLocks;
            _runLuck   = runLuck;

            _tracker.Log.ObjectiveCompleted += OnObjectiveCompleted;
            _tracker.Log.QuestCompleted     += OnQuestCompleted;

            foreach (PlannedObjective planned in plan)
                Add(planned, rooms);
        }

        private void OnDestroy()
        {
            if (_tracker == null) return;
            _tracker.Log.ObjectiveCompleted -= OnObjectiveCompleted;
            _tracker.Log.QuestCompleted     -= OnQuestCompleted;
        }

        // Steps are set up a frame after they unlock, outside the quest log's own events.
        private void Update()
        {
            while (_toArm.Count > 0) Arm(_toArm.Dequeue());
        }

        private void Add(PlannedObjective planned, IReadOnlyDictionary<int, LevelRoom> rooms)
        {
            var running = new Running { Plan = planned };
            var objectives = new ObjectiveDefinition[planned.NodeIds.Count];
            for (int i = 0; i < planned.NodeIds.Count; i++)
            {
                ObjectiveStepTemplate template = planned.Template.Steps[i];
                var step = new Step
                {
                    Room   = rooms[planned.NodeIds[i]],
                    Action = template.Action,
                    Signal = ScriptableObject.CreateInstance<QuestSignal>(),
                };
                running.Steps.Add(step);
                string description = string.IsNullOrEmpty(template.Description) ? Describe(step) : template.Description;
                objectives[i] = new ObjectiveDefinition(description, ObjectiveKind.Signal, step.Signal);
            }

            string title = $"{(planned.IsMain ? "Main" : "Side")}: {planned.Template.Title}";
            running.Quest = QuestDefinition.CreateRuntime(title, planned.Template.Description, objectives,
                                                          planned.Template.Sequential, RewardsFor(planned));
            _running.Add(running);
            _tracker.Log.Add(running.Quest);

            if (planned.Template.Sequential) _toArm.Enqueue(running.Steps[0]);
            else foreach (Step step in running.Steps) _toArm.Enqueue(step);
        }

        private QuestReward[] RewardsFor(PlannedObjective planned)
        {
            if (planned.IsMain || _settings.Currency == null) return System.Array.Empty<QuestReward>();
            int amount = _settings.CurrencyFor(planned.TopTier);
            return amount > 0 ? new[] { new QuestReward { Item = _settings.Currency, Count = amount } } : System.Array.Empty<QuestReward>();
        }

        private string Describe(Step step)
        {
            string where = $"a tier {step.Room.Node.EffectiveTier}{(step.Room.Faction != null ? " " + step.Room.Faction.DisplayName : "")} room";
            return step.Action switch
            {
                ObjectiveAction.Clear    => $"Clear {where}",
                ObjectiveAction.Activate => $"Activate the console in {where}",
                _                        => $"Retrieve the {(_settings.RetrieveItem != null ? _settings.RetrieveItem.DisplayName : "item")} from {where}",
            };
        }

        private void Arm(Step step)
        {
            if (step.Done) return;
            Vector3 spot = _populator.TryTakeSpot(step.Room, out Vector3 free) ? free : _populator.RoomCenter(step.Room);
            step.Marker = CreateMarker(step.Action == ObjectiveAction.Clear ? _populator.RoomCenter(step.Room) : spot);

            switch (step.Action)
            {
                case ObjectiveAction.Clear:    ArmClear(step);          break;
                case ObjectiveAction.Activate: ArmActivate(step, spot); break;
                case ObjectiveAction.Retrieve: ArmRetrieve(step, spot); break;
            }
        }

        private void ArmClear(Step step)
        {
            var alive = new List<HealthManager>();
            foreach (HealthManager enemy in _populator.EnemiesIn(step.Room))
                if (enemy != null && !enemy.IsDead) alive.Add(enemy);
            if (alive.Count == 0)
            {
                Complete(step);
                return;
            }

            int left = alive.Count;
            foreach (HealthManager enemy in alive)
            {
                HealthManager e = enemy;
                System.Action onDeath = null;
                onDeath = () =>
                {
                    e.OnDeath -= onDeath;
                    if (--left == 0) Complete(step);
                };
                e.OnDeath += onDeath;
            }
        }

        private void ArmActivate(Step step, Vector3 spot)
        {
            if (_settings.ConsolePrefab == null)
            {
                Complete(step);
                return;
            }
            LockTerminal console = Instantiate(_settings.ConsolePrefab, spot, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            console.SetLabel("Activate  Console");
            console.SwitchedOn += _ => Complete(step);
        }

        private void ArmRetrieve(Step step, Vector3 spot)
        {
            if (_settings.RetrievePickupPrefab == null || _settings.RetrieveItem == null)
            {
                Complete(step);
                return;
            }
            ItemPickup pickup = PrefabPool.Spawn(_settings.RetrievePickupPrefab, spot + Vector3.up * 0.5f, Quaternion.identity);
            pickup.SetStack(_settings.RetrieveItem, 1);
            pickup.PickedUp += _ => Complete(step);
        }

        private void Complete(Step step)
        {
            if (step.Done) return;
            step.Done = true;
            if (step.Marker != null) Destroy(step.Marker);
            step.Signal.Raise();
        }

        private void OnObjectiveCompleted(QuestProgress quest, ObjectiveProgress objective)
        {
            Running running = Find(quest.Definition);
            if (running == null || !running.Plan.Template.Sequential) return;

            int index = System.Array.IndexOf(running.Quest.Objectives, objective.Definition);
            if (index >= 0 && index + 1 < running.Steps.Count) _toArm.Enqueue(running.Steps[index + 1]);
        }

        private void OnQuestCompleted(QuestProgress quest)
        {
            Running running = Find(quest.Definition);
            if (running == null) return;

            if (running.Plan.IsMain)
            {
                foreach (ConditionLock bossLock in _bossLocks)
                    if (bossLock != null) bossLock.Satisfy(running.Quest);
                return;
            }
            SpawnRewardCache(running);
        }

        private void SpawnRewardCache(Running running)
        {
            if (_settings.RewardCachePrefab == null) return;

            LevelRoom room = running.Steps[running.Steps.Count - 1].Room;
            Vector3 spot = _populator.TryTakeSpot(room, out Vector3 free) ? free : _populator.RoomCenter(room);
            GameObject cache = Instantiate(_settings.RewardCachePrefab, spot, Quaternion.identity, transform);

            float luck = _runLuck + _settings.CacheLuckFor(running.Plan.TopTier);
            foreach (LootDropper dropper in cache.GetComponentsInChildren<LootDropper>(true))
                dropper.AddLuck(luck);
            FeedbackBus.Notify("A reward cache has appeared", NotificationStyle.Reward);
        }

        private GameObject CreateMarker(Vector3 position)
        {
            var marker = new GameObject("ObjectiveMarker");
            marker.transform.SetParent(transform, false);
            marker.transform.position = position;
            marker.AddComponent<MapMarker>().Configure(MapMarkerShape.Diamond, _settings.MarkerColor, MarkerSize);
            return marker;
        }

        private Running Find(QuestDefinition quest) => _running.Find(r => r.Quest == quest);
    }
}
