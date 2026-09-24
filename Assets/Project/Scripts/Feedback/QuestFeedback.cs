using UnityEngine;
using CGD.Quests;

namespace CGD.Feedback
{
    // Announces quest progress: started, objective done, completed, failed.
    // {0} in each preset's message is the quest title (objective description for
    // objective presets).
    public class QuestFeedback : MonoBehaviour
    {
        [SerializeField] private QuestTracker   _tracker;
        [SerializeField] private FeedbackPreset _started;
        [SerializeField] private FeedbackPreset _objectiveComplete;
        [SerializeField] private FeedbackPreset _completed;
        [SerializeField] private FeedbackPreset _failed;

        private void OnEnable()
        {
            if (_tracker == null) return;

            _tracker.Log.QuestStarted       += OnStarted;
            _tracker.Log.ObjectiveCompleted += OnObjectiveCompleted;
            _tracker.Log.QuestCompleted     += OnCompleted;
            _tracker.Log.QuestFailed        += OnFailed;
        }

        private void OnDisable()
        {
            if (_tracker == null) return;

            _tracker.Log.QuestStarted       -= OnStarted;
            _tracker.Log.ObjectiveCompleted -= OnObjectiveCompleted;
            _tracker.Log.QuestCompleted     -= OnCompleted;
            _tracker.Log.QuestFailed        -= OnFailed;
        }

        private void OnStarted(QuestProgress quest)   => FeedbackBus.Play(_started, quest.Definition.Title);
        private void OnCompleted(QuestProgress quest) => FeedbackBus.Play(_completed, quest.Definition.Title);
        private void OnFailed(QuestProgress quest)    => FeedbackBus.Play(_failed, quest.Definition.Title);

        private void OnObjectiveCompleted(QuestProgress quest, ObjectiveProgress objective)
        {
            // The completion announcement covers the last one.
            if (quest.RequiredComplete && !objective.Definition.IsOptional) return;
            FeedbackBus.Play(_objectiveComplete, objective.Definition.Description);
        }
    }
}
