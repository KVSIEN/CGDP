using System;
using UnityEngine;
using CGD.Feedback;
using CGD.Interaction;
using CGD.Stats;

namespace CGD.Level
{
    // One altar of a shrine, offering a boon. Taking it applies the boon to the player for
    // the rest of the level; the shrine then closes the others.
    [RequireComponent(typeof(Collider))]
    public class ShrineAltar : MonoBehaviour, IInteractable
    {
        private StatBoon _boon;
        private bool     _closed;

        public event Action<ShrineAltar> Claimed;

        public void Offer(StatBoon boon) => _boon = boon;

        public void Close() => _closed = true;

        public bool CanInteract(GameObject interactor) => !_closed && _boon != null;

        public string GetInteractLabel(GameObject interactor) => $"Take  {_boon.Label}";

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;

            CharacterStats stats = interactor.GetComponentInParent<CharacterStats>();
            if (stats == null) return;

            stats.AddPreset(_boon);
            _closed = true;
            FeedbackBus.Notify(_boon.Label, NotificationStyle.Reward);
            Claimed?.Invoke(this);
        }
    }
}
