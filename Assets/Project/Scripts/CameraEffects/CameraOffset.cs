using UnityEngine;

namespace CGD.CameraEffects
{
    // What one effect adds to the camera this frame, in the camera's own space.
    public readonly struct CameraOffset
    {
        public static readonly CameraOffset None = default;

        public CameraOffset(Vector3 position, Vector3 rotation, float fov = 0f)
        {
            Position = position;
            Rotation = rotation;
            Fov      = fov;
        }

        public Vector3 Position { get; }
        // Euler degrees: x = pitch (positive looks down), y = yaw, z = roll.
        public Vector3 Rotation { get; }
        public float   Fov      { get; }

        public static CameraOffset operator +(CameraOffset a, CameraOffset b) =>
            new(a.Position + b.Position, a.Rotation + b.Rotation, a.Fov + b.Fov);
    }
}
