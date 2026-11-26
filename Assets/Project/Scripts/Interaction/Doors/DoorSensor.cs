using UnityEngine;
using CGD.Combat;
using CGD.Player;

namespace CGD.Interaction
{
    // Opens its Door for enemies as they arrive, as if they pressed its button, and closes it
    // a moment after the last one leaves — silently, so patrols don't alert each other. The
    // player uses the door's buttons (or forces it) instead, so the sensor ignores them. A
    // locked or barred door stays shut: the sensor never bypasses a key, a condition or a
    // one-way bar. Polled a few times a second, so it needs no rigidbody and survives pooled
    // enemies vanishing inside it.
    public class DoorSensor : MonoBehaviour
    {
        [SerializeField] private Door _door;
        [Tooltip("Centre of the sensed box, in the sensor's space")]
        [SerializeField] private Vector3 _center = new(0f, 1.1f, 0f);
        [Tooltip("Full size of the sensed box; make it deep enough to open before they reach the door")]
        [SerializeField] private Vector3 _size = new(3.4f, 2.2f, 5f);
        [SerializeField] private LayerMask _mask = ~0;
        [Tooltip("Seconds the door stays open after the box empties")]
        [SerializeField, Min(0f)] private float _closeDelay = 0.6f;
        [SerializeField, Min(0.02f)] private float _pollInterval = 0.1f;

        private const int BufferSize = 16;
        private readonly Collider[] _buffer = new Collider[BufferSize];
        private float _nextPoll;
        private float _lastOccupied;
        private bool  _holding;

        private void Update()
        {
            if (Time.time < _nextPoll) return;
            _nextPoll = Time.time + _pollInterval;

            if (SensesEnemy())
            {
                _lastOccupied = Time.time;
                if (!_holding && _door.CanOpenAutomatically)
                {
                    _holding = true;
                    _door.SetOpen(true);
                }
                return;
            }

            if (!_holding || Time.time - _lastOccupied < _closeDelay) return;
            _holding = false;
            _door.SetOpen(false);
        }

        private bool SensesEnemy()
        {
            Vector3 center = transform.TransformPoint(_center);
            Vector3 half   = Vector3.Scale(_size, transform.lossyScale) * 0.5f;
            int count = Physics.OverlapBoxNonAlloc(center, half, _buffer, transform.rotation, _mask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                HealthManager creature = _buffer[i].GetComponentInParent<HealthManager>();
                if (creature != null && !creature.IsDead && creature is not PlayerHealth) return true;
            }
            return false;
        }
    }
}
