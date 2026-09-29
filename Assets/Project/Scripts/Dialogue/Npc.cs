using System;
using UnityEngine;
using CGD.Interaction;

namespace CGD.Dialogue
{
    // A character the player can talk to. Interacting starts its dialogue; the dialogue
    // panel on the HUD listens for ConversationStarted, so NPCs can be prefabs. What the
    // conversation can lead to (trading, quests) lives in the dialogue's choice actions,
    // which find their targets on this GameObject (a Vendor next to it, for example).
    [RequireComponent(typeof(Collider))]
    public class Npc : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _displayName = "Stranger";
        [SerializeField] private DialogueDefinition _dialogue;
        [Tooltip("Turn to face whoever starts talking")]
        [SerializeField] private bool _faceListener = true;

        public static event Action<Conversation> ConversationStarted;

        public string DisplayName => _displayName;

        public string GetInteractLabel(GameObject interactor) => $"Talk  {_displayName}";

        public bool CanInteract(GameObject interactor) => _dialogue != null;

        public void Interact(GameObject interactor)
        {
            if (_dialogue == null) return;
            if (_faceListener) Face(interactor.transform.position);

            var conversation = new Conversation(_dialogue, new DialogueContext(gameObject, interactor, _displayName));
            ConversationStarted?.Invoke(conversation);
            conversation.Start();
        }

        private void Face(Vector3 target)
        {
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(flat);
        }
    }
}
