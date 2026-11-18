using UnityEngine;
using CGD.Combat;
using CGD.Player;
using CGD.Stealth;

namespace CGD.Interaction
{
    // Opens its Door when a creature stands in the sensor box and closes it a moment after
    // the last one leaves, like an automatic door. A locked or barred door stays shut: the
    // sensor never bypasses a key, a condition or a one-way bar. Polled a few times a second,
    // so it needs no rigidbody and survives pooled enemies vanishing inside it.
    //
    // A crouched player who comes in from the side (see DoorSlip) doesn't trip it: they force
    // the door a little open — silently — while they stand at the doorway, and squeeze through.
    public class DoorSensor : MonoBehaviour
    {
        [SerializeField] private Door _door;
        [SerializeField] private DoorSensorTarget _opensFor = DoorSensorTarget.Anyone;
        [Tooltip("Centre of the sensed box, in the sensor's space")]
        [SerializeField] private Vector3 _center = new(0f, 1.1f, 0f);
        [Tooltip("Full size of the sensed box; make it deep enough to open before you reach the door")]
        [SerializeField] private Vector3 _size = new(3.4f, 2.2f, 5f);
        [SerializeField] private LayerMask _mask = ~0;
        [Tooltip("Seconds the door stays open after the box empties")]
        [SerializeField, Min(0f)] private float _closeDelay = 0.6f;
        [SerializeField, Min(0.02f)] private float _pollInterval = 0.1f;

        [Header("Sneaking past")]
        [Tooltip("A crouched player coming in from the side can force the door partly open instead of triggering it. Needs the door's Squeeze Opening above 0")]
        [SerializeField] private bool _allowSqueeze = true;
        [Tooltip("How close to the door's plane (metres, each side) and how far along it the player must stand to force it")]
        [SerializeField] private Vector2 _squeezeReach = new(0.9f, 1.6f);

        private const int BufferSize = 16;
        private readonly Collider[] _buffer = new Collider[BufferSize];
        private DoorSlip _slip;
        private float _nextPoll;
        private float _lastOccupied;
        // Who held it open last: closing after the player makes noise, closing after a guard doesn't.
        private HealthManager _lastOccupant;
        private bool  _holding;

        private void Awake() => _slip = new DoorSlip(_size);

        private void Update()
        {
            if (Time.time < _nextPoll) return;
            _nextPoll = Time.time + _pollInterval;

            Sense(out HealthManager opener, out PlayerHealth slipper);

            if (opener != null)
            {
                _lastOccupied = Time.time;
                _lastOccupant = opener;
                if (!_holding && _door.CanOpenAutomatically)
                {
                    _holding = true;
                    _door.SetOpen(true, opener.gameObject);
                }
                _door.SetSqueezed(false);
                return;
            }

            _door.SetSqueezed(_allowSqueeze && slipper != null && IsAtDoor(slipper.transform.position));

            if (!_holding) return;
            if (Time.time - _lastOccupied < _closeDelay) return;

            _holding = false;
            _door.SetOpen(false, _lastOccupant != null ? _lastOccupant.gameObject : null);
        }

        // `opener`: a creature that trips the door (the player, unless they're slipping past).
        // `slipper`: the player, when they're slipping past instead.
        private void Sense(out HealthManager opener, out PlayerHealth slipper)
        {
            opener  = null;
            slipper = null;

            Vector3 center = transform.TransformPoint(_center);
            Vector3 half   = Vector3.Scale(_size, transform.lossyScale) * 0.5f;
            int count = Physics.OverlapBoxNonAlloc(center, half, _buffer, transform.rotation, _mask, QueryTriggerInteraction.Ignore);

            PlayerHealth player = null;
            for (int i = 0; i < count; i++)
            {
                HealthManager creature = _buffer[i].GetComponentInParent<HealthManager>();
                if (creature == null || creature.IsDead) continue;
                if (creature is PlayerHealth found) player = found;
                else if (_opensFor == DoorSensorTarget.Anyone) opener ??= creature;
            }

            ObservePlayer(player);
            if (player == null) return;
            if (_allowSqueeze && _slip.IsSlipping) slipper = player;
            else opener = player;
        }

        private void ObservePlayer(PlayerHealth player)
        {
            if (player == null)
            {
                _slip.Left();
                return;
            }
            bool crouched = Stealthable.TryGet(player, out Stealthable stealth) && stealth.IsSneaking;
            _slip.Observe(transform.InverseTransformPoint(player.transform.position) - _center, crouched);
        }

        private bool IsAtDoor(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            return Mathf.Abs(local.z) <= _squeezeReach.x && Mathf.Abs(local.x) <= _squeezeReach.y;
        }
    }
}
