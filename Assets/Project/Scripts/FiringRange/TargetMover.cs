using UnityEngine;

namespace CGD.FiringRange
{
    // Moves a target back and forth around where it was placed (sideways or along its facing,
    // per DummyMovement), and eases back to that spot before changing direction or stopping.
    public class TargetMover : MonoBehaviour
    {
        [Tooltip("How far to either side of its spot it sways")]
        [SerializeField, Min(0f)] private float _halfWidth = 2.5f;
        [Tooltip("How far toward and away it advances; keep it inside the room it stands in")]
        [SerializeField, Min(0f)] private float _halfDepth = 3f;
        [SerializeField, Min(0f)] private float _speed = 2f;

        private Vector3 _home;
        private DummyMovement _driving;
        private float _offset;
        private float _direction = 1f;

        public DummyMovement Movement { get; set; }

        private void Awake() => _home = transform.position;

        private void Update()
        {
            if (_driving == DummyMovement.Still && Movement == DummyMovement.Still) return;

            if (Movement != _driving)
            {
                _offset = Mathf.MoveTowards(_offset, 0f, _speed * Time.deltaTime);
                if (_offset == 0f) _driving = Movement;
            }
            else
            {
                PingPong(_driving == DummyMovement.Advance ? _halfDepth : _halfWidth);
            }

            transform.position = Position();
        }

        // Moves the spot it moves around, taking the target with it.
        public void MoveHome(Vector3 home)
        {
            _home = home;
            transform.position = Position();
        }

        private void PingPong(float limit)
        {
            _offset += _direction * _speed * Time.deltaTime;
            if (Mathf.Abs(_offset) < limit) return;

            _offset = Mathf.Clamp(_offset, -limit, limit);
            _direction = -_direction;
        }

        private Vector3 Position() =>
            _home + (_driving == DummyMovement.Advance ? transform.forward : transform.right) * _offset;
    }
}
