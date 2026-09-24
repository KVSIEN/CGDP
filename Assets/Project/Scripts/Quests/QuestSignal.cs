using UnityEngine;

namespace CGD.Quests
{
    // A named moment in the world that objectives can wait for: "talked to the
    // mechanic", "reached the roof", "generator repaired". Raise it from a UnityEvent
    // (EventInteractable, Switch) or from a QuestSignalTrigger zone.
    [CreateAssetMenu(fileName = "QuestSignal", menuName = "CGD/Quests/Quest Signal")]
    public class QuestSignal : ScriptableObject
    {
        public void Raise() => QuestEvents.Report(ObjectiveKind.Signal, this);

        public void Raise(int amount) => QuestEvents.Report(ObjectiveKind.Signal, this, amount);
    }
}
