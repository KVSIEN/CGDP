using UnityEngine;

namespace CGD.Level
{
    // A blast shutter in a doorway: sunk into the floor while open, raised to block the
    // doorway while closed. The slab's collider only blocks while it is closed.
    public class RoomShutter : MonoBehaviour
    {
        [Tooltip("Moves between its authored (closed) position and Travel metres below it")]
        [SerializeField] private Transform _slab;
        [SerializeField] private Collider  _blocker;
        [SerializeField, Min(0.1f)] private float _travel = 4.2f;
        [SerializeField, Min(0.1f)] private float _speed  = 9f;

        private Vector3 _closedPosition;
        private bool    _closed;

        private void Awake()
        {
            _closedPosition = _slab.localPosition;
            _slab.localPosition = OpenPosition;
            if (_blocker != null) _blocker.enabled = false;
            enabled = false;
        }

        private Vector3 OpenPosition => _closedPosition + Vector3.down * _travel;

        public void SetClosed(bool closed)
        {
            _closed = closed;
            if (_blocker != null) _blocker.enabled = closed;
            enabled = true;
        }

        private void Update()
        {
            Vector3 target = _closed ? _closedPosition : OpenPosition;
            _slab.localPosition = Vector3.MoveTowards(_slab.localPosition, target, _speed * Time.deltaTime);
            if (_slab.localPosition == target) enabled = false;
        }
    }
}
