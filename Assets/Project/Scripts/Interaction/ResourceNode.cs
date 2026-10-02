using UnityEngine;
using CGD.Core;
using CGD.Feedback;
using CGD.Items;
using CGD.Player;
using CGD.Quests;

namespace CGD.Interaction
{
    // A spot to gather crafting material from by holding Interact, a few times before it's
    // spent. Placeholder for now: which resource a node gives isn't decided yet, so with
    // no Resource set it can be found and harvested but yields nothing.
    [RequireComponent(typeof(Collider))]
    public class ResourceNode : MonoBehaviour, IInteractable
    {
        [Tooltip("What harvesting gives. Leave empty until the node's resource is decided")]
        [SerializeField] private ItemDefinition _resource;
        [SerializeField] private IntRange _amount = new(2, 4);
        [SerializeField, Min(1)] private int _harvests = 3;
        [SerializeField, Min(0f)] private float _holdDuration = 1.5f;
        [Tooltip("Optional — hidden once the node is spent")]
        [SerializeField] private GameObject _visual;

        private int _left;

        public float HoldDuration => _holdDuration;

        private void Awake() => _left = _harvests;

        public string GetInteractLabel(GameObject interactor) =>
            _resource != null ? $"Harvest  {_resource.DisplayName}" : "Harvest  Resource Node";

        public bool CanInteract(GameObject interactor) => _left > 0 && interactor.TryGetComponent(out PlayerInventory _);

        public void Interact(GameObject interactor)
        {
            if (_left <= 0 || !interactor.TryGetComponent(out PlayerInventory inventory)) return;

            _left--;
            if (_left == 0 && _visual != null) _visual.SetActive(false);

            if (_resource == null)
            {
                FeedbackBus.Notify("Nothing to gather here yet", NotificationStyle.Info);
                return;
            }

            int rolled = Mathf.Max(1, _amount.Evaluate());
            int amount = rolled - inventory.Inventory.Add(_resource, rolled);
            if (amount <= 0) return;
            QuestEvents.Report(ObjectiveKind.Collect, _resource, amount);
            FeedbackBus.Notify($"+{amount} {_resource.DisplayName}", NotificationStyle.Reward);
        }
    }
}
