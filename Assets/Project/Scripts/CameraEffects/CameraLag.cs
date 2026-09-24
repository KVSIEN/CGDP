using UnityEngine;

namespace CGD.CameraEffects
{
    // Makes the camera trail its target position slightly, softening sudden movement.
    // Returns a world-space offset from the target position.
    public class CameraLag
    {
        private Vector3 _smoothed;
        private Vector3 _velocity;
        private bool    _hasPosition;

        public void Snap() => _hasPosition = false;

        public Vector3 Evaluate(Vector3 target, float deltaTime, CameraEffectSettings settings)
        {
            if (settings.PositionLag <= 0f || !_hasPosition || deltaTime <= 0f)
            {
                _smoothed    = target;
                _velocity    = Vector3.zero;
                _hasPosition = true;
                return Vector3.zero;
            }

            _smoothed = Vector3.SmoothDamp(_smoothed, target, ref _velocity, settings.PositionLag, Mathf.Infinity, deltaTime);
            _smoothed = target + Vector3.ClampMagnitude(_smoothed - target, settings.MaxLagDistance);
            return _smoothed - target;
        }
    }
}
