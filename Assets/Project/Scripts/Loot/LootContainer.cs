using UnityEngine;
using UnityEngine.Events;
using CGD.Core;
using CGD.Interaction;

namespace CGD.Loot
{
    // A chest, locker or supply cache: interact to open it once and spill its LootDropper's
    // table. Can require an item (a key, a lockpick) and exposes an event for lids,
    // sounds and VFX.
    [RequireComponent(typeof(LootDropper))]
    [RequireComponent(typeof(Collider))]
    public class LootContainer : MonoBehaviour, IInteractable, IPoolable
    {
        [SerializeField] private string _label = "Open";
        [Tooltip("Seconds the Interact key must be held (0 = instant)")]
        [SerializeField, Min(0f)] private float _holdDuration = 0.5f;
        [SerializeField] private ItemRequirement _requirement;
        [SerializeField] private UnityEvent _onOpened = new();

        private LootDropper _dropper;
        private bool        _opened;

        public float HoldDuration => _holdDuration;
        public bool  IsOpened     => _opened;

        private void Awake() => _dropper = GetComponent<LootDropper>();

        // A pooled container comes back full.
        public void OnSpawned() => _opened = false;

        public bool CanInteract(GameObject interactor) => !_opened;

        public string GetInteractLabel(GameObject interactor) =>
            _requirement.IsMetBy(interactor) ? _label : $"Locked  ({_requirement.Describe()})";

        public void Interact(GameObject interactor)
        {
            if (_opened || !_requirement.TryUse(interactor)) return;

            _opened = true;
            _dropper.Drop();
            _onOpened.Invoke();
        }
    }
}
