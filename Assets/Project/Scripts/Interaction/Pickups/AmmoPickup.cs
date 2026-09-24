using UnityEngine;
using CGD.Core;
using CGD.Feedback;
using CGD.Items;
using CGD.Player;
using CGD.Quests;

namespace CGD.Interaction
{
    // Attach to a world GameObject with a Collider (set Is Trigger = true).
    // Adds a stack of the referenced munition to the player's shared inventory.
    [RequireComponent(typeof(Collider))]
    public class AmmoPickup : MonoBehaviour, IInteractable
    {
        [Tooltip("Which munition (and therefore which shared AmmoType pool) this pickup grants.")]
        [SerializeField] private MunitionDefinition _munition;
        [SerializeField] private int _amount = 30;

        public string GetInteractLabel(GameObject interactor) =>
            _munition != null ? $"Pick Up  {_munition.DisplayName} ×{_amount}" : "Pick Up  Ammo";

        public bool CanInteract(GameObject player) =>
            _munition != null && _amount > 0 && player.TryGetComponent(out PlayerInventory _);

        public void Interact(GameObject player)
        {
            if (_munition == null || _amount <= 0) return;
            if (!player.TryGetComponent(out PlayerInventory inventory)) return;

            inventory.Inventory.Add(_munition, _amount);
            QuestEvents.Report(ObjectiveKind.Collect, _munition, _amount);
            FeedbackBus.Notify($"+{_amount} {_munition.DisplayName}", NotificationStyle.Reward);
            PrefabPool.Release(gameObject);
        }
    }
}
