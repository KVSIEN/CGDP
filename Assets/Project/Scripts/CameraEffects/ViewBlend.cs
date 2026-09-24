using UnityEngine;

namespace CGD.CameraEffects
{
    // Blends the camera from wherever gameplay puts it toward another viewpoint (a death
    // cam, a door opening, a boss intro) and back. Weight 0 = gameplay view, 1 = viewpoint.
    public class ViewBlend
    {
        private Transform _viewpoint;
        private float     _weight;
        private float     _target;
        private float     _speed;

        public bool IsActive => _weight > 0f || _target > 0f;

        public void BlendTo(Transform viewpoint, float duration)
        {
            _viewpoint = viewpoint;
            _target    = 1f;
            _speed     = SpeedFor(duration);
        }

        public void Release(float duration)
        {
            _target = 0f;
            _speed  = SpeedFor(duration);
        }

        public void Clear()
        {
            _weight = _target = 0f;
            _viewpoint = null;
        }

        public void Advance(float deltaTime)
        {
            if (_viewpoint == null) _weight = _target = 0f;
            else                    _weight = Mathf.MoveTowards(_weight, _target, _speed * deltaTime);
        }

        // Moves a pose toward the viewpoint by the current (eased) weight.
        public void Apply(ref Vector3 position, ref Quaternion rotation)
        {
            if (_viewpoint == null || _weight <= 0f) return;

            float t = Mathf.SmoothStep(0f, 1f, _weight);
            position = Vector3.Lerp(position, _viewpoint.position, t);
            rotation = Quaternion.Slerp(rotation, _viewpoint.rotation, t);
        }

        private static float SpeedFor(float duration) => duration > 0f ? 1f / duration : float.MaxValue;
    }
}
