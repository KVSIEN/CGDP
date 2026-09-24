using UnityEngine;

namespace CGD.CameraEffects
{
    // A damped spring the camera can be kicked with: recoil, landing thumps, a blow to
    // the head. Kicks add velocity, so repeated kicks stack naturally and the camera
    // always settles back to rest.
    public class SpringKick
    {
        private Vector3 _angle;
        private Vector3 _angularVelocity;
        private Vector3 _offset;
        private Vector3 _velocity;

        // Instant velocity, in degrees/second and metres/second.
        public void Kick(Vector3 angularVelocity, Vector3 velocity)
        {
            _angularVelocity += angularVelocity;
            _velocity        += velocity;
        }

        public void Clear()
        {
            _angle = _angularVelocity = _offset = _velocity = Vector3.zero;
        }

        public CameraOffset Evaluate(float deltaTime, CameraEffectSettings settings)
        {
            Step(ref _angle,  ref _angularVelocity, deltaTime, settings);
            Step(ref _offset, ref _velocity,        deltaTime, settings);
            return new CameraOffset(_offset, _angle);
        }

        // Semi-implicit Euler, split into small steps so a long frame can't blow the
        // spring up.
        private static void Step(ref Vector3 value, ref Vector3 velocity, float deltaTime, CameraEffectSettings settings)
        {
            const float MaxStep = 1f / 120f;

            while (deltaTime > 0f)
            {
                float dt = Mathf.Min(deltaTime, MaxStep);
                velocity += (-settings.SpringStiffness * value - settings.SpringDamping * velocity) * dt;
                value    += velocity * dt;
                deltaTime -= dt;
            }
        }
    }
}
