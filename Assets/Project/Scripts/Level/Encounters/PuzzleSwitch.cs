using System;
using UnityEngine;
using CGD.Interaction;

namespace CGD.Level
{
    // One node of a Puzzle room's calibration ring (LightsOutPuzzle): using it flips it and
    // its two neighbours. Its light shows whether it is calibrated (on).
    [RequireComponent(typeof(Collider))]
    public class PuzzleSwitch : MonoBehaviour, IInteractable
    {
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer _indicator;
        [SerializeField] private Color _offColor = new(1f, 0.25f, 0.2f);
        [SerializeField] private Color _onColor  = new(0.3f, 1f, 0.4f);
        [SerializeField, Min(0f)] private float _holdDuration = 0.4f;

        private Action<int> _pressed;
        private string _label = "Calibrate";
        private int    _index;
        private bool   _locked;
        private MaterialPropertyBlock _block;

        public float HoldDuration => _holdDuration;

        public void Setup(int index, string label, Action<int> pressed)
        {
            _index   = index;
            _label   = label;
            _pressed = pressed;
        }

        public void ShowState(bool on)
        {
            if (_indicator == null) return;
            _block ??= new MaterialPropertyBlock();
            _indicator.GetPropertyBlock(_block);
            _block.SetColor(ColorId, on ? _onColor : _offColor);
            _indicator.SetPropertyBlock(_block);
        }

        // A solved puzzle stays solved.
        public void Lock() => _locked = true;

        public string GetInteractLabel(GameObject interactor) => _label;

        public bool CanInteract(GameObject interactor) => !_locked && _pressed != null;

        public void Interact(GameObject interactor)
        {
            if (!_locked) _pressed?.Invoke(_index);
        }
    }
}
