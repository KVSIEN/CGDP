using UnityEngine;
using CGD.Player;

namespace CGD.Quests
{
    // Raises a QuestSignal when the player enters this trigger — "reach the location"
    // objectives. Needs a trigger Collider on the same GameObject.
    [RequireComponent(typeof(Collider))]
    public class QuestSignalTrigger : MonoBehaviour
    {
        [SerializeField] private QuestSignal _signal;
        [Tooltip("Raise only the first time the player enters")]
        [SerializeField] private bool _once = true;

        private bool _raised;

        private void OnTriggerEnter(Collider other)
        {
            if (_signal == null || (_once && _raised)) return;
            if (other.GetComponentInParent<PlayerHealth>() == null) return;

            _raised = true;
            _signal.Raise();
        }
    }
}
