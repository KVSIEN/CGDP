using UnityEngine;
using CGD.Combat;

namespace CGD.FiringRange
{
    // Moves a target back and forth around where it was placed (sideways or along its facing,
    // per DummyMovement), and eases back to that spot before changing direction or stopping.
    // Strafing turns at random moments as well as at the edges, like a player dodging shots.
    // Slowed and stunned like any character, so stun and ice effects can be seen working.
    public class TargetMover : MonoBehaviour
    {
        [Tooltip("How far to either side of its spot it sways or strafes")]
        [SerializeField, Min(0f)] private float _halfWidth = 2.5f;
        [Tooltip("How far toward and away it advances; keep it inside the room it stands in")]
        [SerializeField, Min(0f)] private float _halfDepth = 3f;
        [SerializeField, Min(0f)] private float _speed = 2f;
        [Header("Strafe")]
        [Tooltip("Strafing speed: about a player's walk, quicker than the sway")]
        [SerializeField, Min(0f)] private float _strafeSpeed = 3.5f;
        [Tooltip("Shortest and longest run in one direction before a random turn (seconds)")]
        [SerializeField] private Vector2 _strafeTurnTime = new(0.25f, 1.1f);

        private Vector3 _home;
        private DummyMovement _driving;
        private float _offset;
        private float _direction = 1f;
        private float _turnTimer;
        private Stunnable _stunnable;

        public DummyMovement Movement { get; set; }

        private void Awake()
        {
            _home = transform.position;
            TryGetComponent(out _stunnable);
        }

        private void Update()
        {
            if (_driving == DummyMovement.Still && Movement == DummyMovement.Still) return;

            float step = Speed() * Time.deltaTime;
            if (step <= 0f) return;

            if (Movement != _driving)
            {
                _offset = Mathf.MoveTowards(_offset, 0f, step);
                if (_offset == 0f) _driving = Movement;
            }
            else
            {
                if (_driving == DummyMovement.Strafe) TurnAtRandom();
                PingPong(_driving == DummyMovement.Advance ? _halfDepth : _halfWidth, step);
            }

            transform.position = Position();
        }

        private void TurnAtRandom()
        {
            _turnTimer -= Time.deltaTime;
            if (_turnTimer > 0f) return;

            _direction = -_direction;
            _turnTimer = Random.Range(_strafeTurnTime.x, Mathf.Max(_strafeTurnTime.x, _strafeTurnTime.y));
        }

        // Moves the spot it moves around, taking the target with it.
        public void MoveHome(Vector3 home)
        {
            _home = home;
            transform.position = Position();
        }

        private float Speed()
        {
            float speed = _driving == DummyMovement.Strafe ? _strafeSpeed : _speed;
            if (_stunnable == null) return speed;
            return _stunnable.IsStunned ? 0f : speed * _stunnable.SpeedMultiplier;
        }

        private void PingPong(float limit, float step)
        {
            _offset += _direction * step;
            if (Mathf.Abs(_offset) < limit) return;

            _offset = Mathf.Clamp(_offset, -limit, limit);
            _direction = -_direction;
        }

        private Vector3 Position() =>
            _home + (_driving == DummyMovement.Advance ? transform.forward : transform.right) * _offset;
    }
}
