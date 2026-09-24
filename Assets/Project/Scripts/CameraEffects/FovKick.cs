using UnityEngine;

namespace CGD.CameraEffects
{
    // A momentary field-of-view punch (dash, explosion, speed boost) that eases back.
    // Separate from PlayerCamera's sprint and aim FOV, which are held states.
    public class FovKick
    {
        private float _fov;

        public void Kick(float degrees) => _fov += degrees;

        public void Clear() => _fov = 0f;

        public CameraOffset Evaluate(float deltaTime, CameraEffectSettings settings)
        {
            if (_fov == 0f) return CameraOffset.None;

            _fov *= Mathf.Exp(-settings.FovRecovery * deltaTime);
            if (Mathf.Abs(_fov) < 0.01f) _fov = 0f;

            return new CameraOffset(Vector3.zero, Vector3.zero, _fov);
        }
    }
}
