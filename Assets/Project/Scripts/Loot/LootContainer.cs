using UnityEngine;
using UnityEngine.Events;
using CGD.Combat;
using CGD.Core;
using CGD.Feedback;
using CGD.Interaction;

namespace CGD.Loot
{
    // A chest, locker or supply cache: interact to open it once and spill its LootDropper's
    // table. Can require an item (a key, a lockpick) and exposes an event for lids,
    // sounds and VFX.
    //
    // Searchable furniture (Find Weight above 0) shares in its level's search finds instead
    // of rolling its own odds: once the level is built it is told whether it holds one
    // (PlanSearch), and searched it drops a find that is never empty, or nothing. Rummaging
    // through it makes a noise enemies can hear (Noise Radius).
    [RequireComponent(typeof(LootDropper))]
    [RequireComponent(typeof(Collider))]
    public class LootContainer : MonoBehaviour, IInteractable, IPoolable
    {
        [SerializeField] private string _label = "Open";
        [Tooltip("Seconds the Interact key must be held (0 = instant)")]
        [SerializeField, Min(0f)] private float _holdDuration = 0.5f;
        [SerializeField] private ItemRequirement _requirement;
        [SerializeField] private UnityEvent _onOpened = new();

        [Header("Searchable furniture")]
        [Tooltip("How likely the level's search finds land here, against the other searchable furniture (bigger pieces higher). 0 = not searchable furniture: it always rolls its own table")]
        [SerializeField, Min(0f)] private float _findWeight;
        [Tooltip("Adds its table's odds of holding something to the level's search finds. Off for a piece that only shares in the finds others add, so more searchable furniture spreads the loot instead of adding to it")]
        [SerializeField] private bool _addsFinds;
        [Tooltip("How far the noise of searching it carries, metres (0 = silent)")]
        [SerializeField, Min(0f)] private float _noiseRadius;

        private LootDropper _dropper;
        private bool        _opened;
        private bool        _searchPlanned;
        private bool        _holdsFind;

        public float HoldDuration => _holdDuration;
        public bool  IsOpened     => _opened;
        public float FindWeight   => _findWeight;
        public LootDropper Dropper => _dropper != null ? _dropper : _dropper = GetComponent<LootDropper>();

        // The finds this piece adds to its level's: its table's odds at its luck, or none.
        public float FindsAdded => _addsFinds && _findWeight > 0f ? Dropper.FindChance : 0f;

        // Raised after the loot has dropped (an Ambush springs on it).
        public event System.Action Opened;

        private void Awake() => _dropper = GetComponent<LootDropper>();

        // A pooled container comes back full, and rolls its own table until a level plans it.
        public void OnSpawned()
        {
            _opened        = false;
            _searchPlanned = false;
        }

        // Whether this piece holds one of the level's search finds (SearchLootDistributor).
        public void PlanSearch(bool holdsFind)
        {
            _searchPlanned = true;
            _holdsFind     = holdsFind;
        }

        public bool CanInteract(GameObject interactor) => !_opened;

        public string GetInteractLabel(GameObject interactor) =>
            _requirement.IsMetBy(interactor) ? Label : $"Locked  ({_requirement.Describe()})";

        // "Open weapon crate (Pistols)" when the dropper rolled a subtype.
        private string Label
        {
            get
            {
                LootFilter subtype = _dropper.Subtype;
                return subtype != null && !string.IsNullOrEmpty(subtype.Label) ? $"{_label} ({subtype.Label})" : _label;
            }
        }

        public void Interact(GameObject interactor)
        {
            if (_opened || !_requirement.TryUse(interactor)) return;

            _opened = true;
            Noise.Emit(transform.position, _noiseRadius, DamageSource.Of(interactor));
            if (!_searchPlanned) _dropper.Drop();
            else if (_holdsFind) _dropper.DropFound();
            else FeedbackBus.Notify("Nothing useful");
            _onOpened.Invoke();
            Opened?.Invoke();
        }
    }
}
