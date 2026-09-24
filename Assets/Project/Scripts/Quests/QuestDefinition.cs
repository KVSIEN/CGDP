using UnityEngine;

namespace CGD.Quests
{
    // One quest: what to do (objectives), in what order, how long you have, what it
    // pays, and which quests must be finished first. Chains are built from
    // prerequisites: a quest becomes available once every prerequisite is completed.
    [CreateAssetMenu(fileName = "Quest", menuName = "CGD/Quests/Quest")]
    public class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string _title = "Quest";
        [SerializeField, TextArea] private string _description;

        [Header("Objectives")]
        [SerializeField] private ObjectiveDefinition[] _objectives = System.Array.Empty<ObjectiveDefinition>();
        [Tooltip("Objectives unlock one at a time, in order. Otherwise all count at once.")]
        [SerializeField] private bool _sequential;

        [Header("Availability")]
        [Tooltip("Must all be completed before this quest becomes available")]
        [SerializeField] private QuestDefinition[] _prerequisites = System.Array.Empty<QuestDefinition>();
        [Tooltip("Starts as soon as it becomes available; otherwise something must call QuestTracker.StartQuest")]
        [SerializeField] private bool _autoStart = true;

        [Header("Failure")]
        [Tooltip("Game seconds to finish once started; 0 = no limit")]
        [SerializeField, Min(0f)] private float _timeLimit;
        [Tooltip("A failed quest becomes available again")]
        [SerializeField] private bool _retryable = true;

        [Header("Rewards")]
        [SerializeField] private QuestReward[] _rewards = System.Array.Empty<QuestReward>();

        public string                Title         => _title;
        public string                Description   => _description;
        public ObjectiveDefinition[] Objectives    => _objectives;
        public bool                  Sequential    => _sequential;
        public QuestDefinition[]     Prerequisites => _prerequisites;
        public bool                  AutoStart     => _autoStart;
        public float                 TimeLimit     => _timeLimit;
        public bool                  HasTimeLimit  => _timeLimit > 0f;
        public bool                  Retryable     => _retryable;
        public QuestReward[]         Rewards       => _rewards;
    }
}
