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

        [Header("Room tier")]
        [Tooltip("Rarer resources given instead in tier 2 and tier 3 rooms (element 0 = tier 2). Empty = always the Resource")]
        [SerializeField] private ItemDefinition[] _higherTierResources = System.Array.Empty<ItemDefinition>();
        [Tooltip("Extra amount per harvest for each room tier above 1")]
        [SerializeField, Min(0)] private int _bonusPerTier = 1;

        private int _left;
        private int _tier = 1;

        // The room this node stands in sets its tier when the level is built.
        public void SetTier(int tier) => _tier = Mathf.Clamp(tier, 1, 3);

        // Rarest resource set up to this tier.
        private ItemDefinition Resource
        {
            get
            {
                for (int t = Mathf.Min(_tier - 2, _higherTierResources.Length - 1); t >= 0; t--)
                    if (_higherTierResources[t] != null) return _higherTierResources[t];
                return _resource;
            }
        }

        public float HoldDuration => _holdDuration;

        private void Awake() => _left = _harvests;

        public string GetInteractLabel(GameObject interactor) =>
            Resource != null ? $"Harvest  {Resource.DisplayName}" : "Harvest  Resource Node";

        public bool CanInteract(GameObject interactor) => _left > 0 && interactor.TryGetComponent(out PlayerInventory _);

        public void Interact(GameObject interactor)
        {
            if (_left <= 0 || !interactor.TryGetComponent(out PlayerInventory inventory)) return;

            _left--;
            if (_left == 0 && _visual != null) _visual.SetActive(false);

            ItemDefinition resource = Resource;
            if (resource == null)
            {
                FeedbackBus.Notify("Nothing to gather here yet", NotificationStyle.Info);
                return;
            }

            int rolled = Mathf.Max(1, _amount.Evaluate()) + _bonusPerTier * (_tier - 1);
            int amount = rolled - inventory.Inventory.Add(resource, rolled);
            if (amount <= 0) return;
            QuestEvents.Report(ObjectiveKind.Collect, resource, amount);
            FeedbackBus.Notify($"+{amount} {resource.DisplayName}", NotificationStyle.Reward);
        }
    }
}
