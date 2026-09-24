using UnityEngine;
using UnityEngine.Events;

namespace CGD.Interaction
{
    // Designer-wired interactable: terminals, levers, buttons, NPC conversation starters,
    // vehicle seats, story triggers. Hook up what happens in the Inspector through
    // On Interact instead of writing a component per object. Supports an item
    // requirement, single use, and a cooldown between uses.
    [RequireComponent(typeof(Collider))]
    public class EventInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _label = "Use";
        [Tooltip("Seconds the Interact key must be held (0 = instant)")]
        [SerializeField, Min(0f)] private float _holdDuration;
        [Tooltip("Higher wins when several interactables overlap")]
        [SerializeField] private int _priority;

        [Header("Rules")]
        [SerializeField] private ItemRequirement _requirement;
        [Tooltip("Can only be used once; the prompt disappears afterwards")]
        [SerializeField] private bool _singleUse;
        [Tooltip("Seconds before it can be used again (prompt hidden meanwhile)")]
        [SerializeField, Min(0f)] private float _cooldown;

        [Header("Events")]
        [Tooltip("Receives the interacting GameObject")]
        [SerializeField] private UnityEvent<GameObject> _onInteract = new();
        [Tooltip("Fired when used without meeting the requirement (play a 'locked' sound, show a hint...)")]
        [SerializeField] private UnityEvent<GameObject> _onRequirementMissing = new();

        private bool  _used;
        private float _readyTime;

        public float HoldDuration     => _holdDuration;
        public int   InteractPriority => _priority;

        public bool CanInteract(GameObject interactor) =>
            !(_singleUse && _used) && Time.time >= _readyTime;

        public string GetInteractLabel(GameObject interactor) =>
            _requirement.IsMetBy(interactor) ? _label : $"{_label}  (needs {_requirement.Describe()})";

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;

            if (!_requirement.TryUse(interactor))
            {
                _onRequirementMissing.Invoke(interactor);
                return;
            }

            _used      = true;
            _readyTime = Time.time + _cooldown;
            _onInteract.Invoke(interactor);
        }

        // For scripted resets (e.g. a puzzle that re-arms its lever).
        public void ResetUse()
        {
            _used      = false;
            _readyTime = 0f;
        }
    }
}
