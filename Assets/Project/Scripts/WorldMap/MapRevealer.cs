using UnityEngine;

namespace CGD.WorldMap
{
    // Uncovers the fog of war around this object (usually the Player). Only reveals
    // after moving a little, not every frame.
    public class MapRevealer : MonoBehaviour
    {
        [SerializeField] private WorldMapArea _area;
        [SerializeField, Min(0.5f)] private float _radius = 18f;
        [Tooltip("Metres to move before revealing again")]
        [SerializeField, Min(0.1f)] private float _step = 1f;

        private Vector3 _lastReveal;
        private bool    _hasRevealed;

        private void Update()
        {
            if (_area == null) return;

            Vector3 position = transform.position;
            if (_hasRevealed && (position - _lastReveal).sqrMagnitude < _step * _step) return;

            _area.Reveal(position, _radius);
            _lastReveal  = position;
            _hasRevealed = true;
        }
    }
}
