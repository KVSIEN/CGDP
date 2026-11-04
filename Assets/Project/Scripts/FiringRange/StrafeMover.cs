using UnityEngine;

namespace CGD.FiringRange
{
    // Sways a target side to side along its right axis around where it was placed, and eases
    // back to that spot when switched off.
    public class StrafeMover : MonoBehaviour
    {
        [Tooltip("How far to either side of its spot it sways")]
        [SerializeField, Min(0f)] private float _halfWidth = 2.5f;
        [SerializeField, Min(0f)] private float _speed = 2f;

        private Vector3 _home;
        private float   _offset;
        private float   _direction = 1f;

        public bool Strafing { get; set; }

        // Moves the spot it sways around, taking the target with it.
        public void MoveHome(Vector3 home)
        {
            _home = home;
            transform.position = home + transform.right * _offset;
        }

        private void Awake() => _home = transform.position;

        private void Update()
        {
            if (!Strafing && _offset == 0f) return;

            if (Strafing)
            {
                _offset += _direction * _speed * Time.deltaTime;
                if (Mathf.Abs(_offset) >= _halfWidth)
                {
                    _offset = Mathf.Clamp(_offset, -_halfWidth, _halfWidth);
                    _direction = -_direction;
                }
            }
            else
            {
                _offset = Mathf.MoveTowards(_offset, 0f, _speed * Time.deltaTime);
            }

            transform.position = _home + transform.right * _offset;
        }
    }
}
