using UnityEngine;

namespace CGD.Interaction
{
    // A console that is switched on once (hold Interact): it counts toward its
    // ConditionLock, if it has one, and raises SwitchedOn for anything else listening (a
    // room encounter's uplink or vent controls). Hand-placed terminals reference their
    // lock; a generated level binds them with Bind.
    [RequireComponent(typeof(Collider))]
    public class LockTerminal : MonoBehaviour, IInteractable
    {
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private ConditionLock _lock;
        [SerializeField] private string _label = "Restore power";
        [SerializeField, Min(0f)] private float _holdDuration = 1.5f;
        [Tooltip("Optional light that turns from Off to On colour when switched on")]
        [SerializeField] private Renderer _indicator;
        [SerializeField] private Color _offColor = new(1f, 0.25f, 0.2f);
        [SerializeField] private Color _onColor  = new(0.3f, 1f, 0.4f);

        private bool _isOn;
        private MaterialPropertyBlock _block;

        public float HoldDuration => _holdDuration;
        public bool  IsOn         => _isOn;

        public event System.Action<LockTerminal> SwitchedOn;

        private void Awake() => ShowState();

        public void Bind(ConditionLock conditionLock) => _lock = conditionLock;

        public void SetLabel(string label) => _label = label;

        public string GetInteractLabel(GameObject interactor) => _label;

        public bool CanInteract(GameObject interactor) => !_isOn;

        public void Interact(GameObject interactor)
        {
            if (_isOn) return;

            _isOn = true;
            if (_lock != null) _lock.Satisfy(this);
            ShowState();
            SwitchedOn?.Invoke(this);
        }

        private void ShowState()
        {
            if (_indicator == null) return;
            _block ??= new MaterialPropertyBlock();
            _indicator.GetPropertyBlock(_block);
            _block.SetColor(ColorId, _isOn ? _onColor : _offColor);
            _indicator.SetPropertyBlock(_block);
        }
    }
}
