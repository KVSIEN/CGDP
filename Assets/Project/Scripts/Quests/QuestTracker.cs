using UnityEngine;
using CGD.Items;
using CGD.Player;
using CGD.Timing;

namespace CGD.Quests
{
    // The player's quest log in the scene: registers the quests this scene knows about,
    // feeds them world events (QuestEvents) and game time, and pays out rewards into the
    // player's inventory. Time limits run on GameClock, so they pause with the game.
    public class QuestTracker : MonoBehaviour, ITickable
    {
        [Tooltip("Every quest that can happen here, chained ones included (via prerequisites)")]
        [SerializeField] private QuestDefinition[] _quests = System.Array.Empty<QuestDefinition>();
        [Tooltip("Where rewards go. Found on this GameObject if left empty.")]
        [SerializeField] private PlayerInventory _inventory;

        public QuestLog Log { get; } = new();

        private void Awake()
        {
            if (_inventory == null) TryGetComponent(out _inventory);
            Log.QuestCompleted += GrantRewards;

            foreach (QuestDefinition quest in _quests)
                Log.Add(quest);
        }

        private void OnEnable()
        {
            QuestEvents.Reported += Log.Report;
            GameTime.Instance.Clock.Register(this);
        }

        private void OnDisable()
        {
            QuestEvents.Reported -= Log.Report;
            if (GameTime.Exists) GameTime.Instance.Clock.Unregister(this);
        }

        void ITickable.Tick(float deltaTime) => Log.Tick(deltaTime);

        // For UnityEvents: an NPC's EventInteractable can start a quest that isn't auto-start.
        public void StartQuest(QuestDefinition quest) => Log.Start(quest);

        private void GrantRewards(QuestProgress quest)
        {
            if (_inventory == null) return;

            foreach (QuestReward reward in quest.Definition.Rewards)
            {
                if (reward.Item == null) continue;
                int count = Mathf.Max(1, reward.Count);

                if (reward.Item is not GearDefinition gear)
                {
                    _inventory.Inventory.Add(reward.Item, count);
                    continue;
                }

                for (int i = 0; i < count; i++)
                    _inventory.Inventory.Add(gear.CreateInstance());
            }
        }
    }
}
