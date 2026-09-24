using UnityEngine;

namespace CGD.CameraEffects
{
    // Trauma-based shake: events add trauma (0..1), it decays over time, and the camera
    // shakes by trauma^exponent along smooth noise. Many small hits blend into one
    // continuous shake instead of fighting each other.
    public class TraumaShake
    {
        // Separate noise rows per axis so they don't move in lockstep.
        private static readonly float[] Rows = { 0.1f, 10.3f, 20.7f, 30.2f, 40.9f, 50.4f };

        private float _time;

        public float Trauma { get; private set; }

        public void AddTrauma(float amount) => Trauma = Mathf.Clamp01(Trauma + amount);

        public void Clear() => Trauma = 0f;

        public CameraOffset Evaluate(float deltaTime, CameraEffectSettings settings)
        {
            if (Trauma <= 0f) return CameraOffset.None;

            _time += deltaTime * settings.ShakeFrequency;
            float shake = Mathf.Pow(Trauma, settings.TraumaExponent);
            Trauma = Mathf.Max(0f, Trauma - settings.TraumaDecay * deltaTime);

            Vector3 angles = Scale(settings.MaxShakeAngles, 0) * shake;
            Vector3 offset = Scale(settings.MaxShakeOffset, 3) * shake;
            return new CameraOffset(offset, angles);
        }

        private Vector3 Scale(Vector3 max, int firstRow) => new(
            max.x * Noise(firstRow),
            max.y * Noise(firstRow + 1),
            max.z * Noise(firstRow + 2));

        // -1..1
        private float Noise(int row) => Mathf.PerlinNoise(Rows[row], _time) * 2f - 1f;
    }
}
