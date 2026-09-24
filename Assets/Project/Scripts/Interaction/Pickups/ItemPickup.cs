using UnityEngine;
using CGD.Core;
using CGD.Feedback;
using CGD.Items;
using CGD.Player;
using CGD.Quests;

namespace CGD.Interaction
{
    // Generic world pickup for anything that goes into the player's inventory: a stack of a
    // stackable item (resources, consumables, munitions, keycards) or one rolled gear piece.
    // Place by hand with Item/Count, or let LootDropper spawn and fill it from a pool.
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour, IInteractable, IPoolable
    {
        [SerializeField] private ItemDefinition _item;
        [SerializeField, Min(1)] private int _count = 1;

        private ItemInstance _instance;

        public void SetStack(ItemDefinition item, int count)
        {
            _item     = item;
            _count    = Mathf.Max(1, count);
            _instance = null;
        }

        public void SetInstance(ItemInstance instance)
        {
            _instance = instance;
            _item     = instance?.Definition;
            _count    = 1;
        }

        public void OnDespawned() => _instance = null;

        public string GetInteractLabel(GameObject interactor)
        {
            if (_instance != null) return $"Pick Up  {_instance.DisplayName}";
            if (_item == null)     return "Pick Up";
            return _count > 1 ? $"Pick Up  {_item.DisplayName} ×{_count}" : $"Pick Up  {_item.DisplayName}";
        }

        public bool CanInteract(GameObject interactor) =>
            (_instance != null || _item != null) && interactor.TryGetComponent(out PlayerInventory _);

        public void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out PlayerInventory inventory)) return;

            if (_instance != null)           inventory.Inventory.Add(_instance);
            else if (_item != null)          inventory.Inventory.Add(_item, _count);
            else                             return;

            QuestEvents.Report(ObjectiveKind.Collect, _item, _count);
            FeedbackBus.Notify(_instance != null ? $"Picked up {_instance.DisplayName}" : $"+{_count} {_item.DisplayName}",
                               NotificationStyle.Reward);

            PrefabPool.Release(gameObject);
        }
    }
}
