using UnityEngine;

namespace CGD.Stealth
{
    // The ground a concealment zone covers, as a flat rectangle: where it is, which way it lies
    // and how far it reaches either way. What a character who has seen a bush knows of it — the
    // whole patch, not a point in the middle of it — so it can pick where to go in, where to lie
    // and which way to cross it. Plain geometry; nothing here touches physics.
    public readonly struct BushFootprint
    {
        private readonly Vector3 _centre;   // on the ground
        private readonly Vector2 _half;     // half the width along each axis
        private readonly Vector3 _axisX;    // flat unit vectors
        private readonly Vector3 _axisZ;

        public BushFootprint(Vector3 groundCentre, Vector2 halfSize, Vector3 axisX)
        {
            axisX.y = 0f;
            _axisX  = axisX.sqrMagnitude > 0.0001f ? axisX.normalized : Vector3.right;
            _axisZ  = new Vector3(-_axisX.z, 0f, _axisX.x);
            _centre = groundCentre;
            _half   = new Vector2(Mathf.Max(0.05f, halfSize.x), Mathf.Max(0.05f, halfSize.y));
        }

        public Vector3 Centre => _centre;
        public Vector2 HalfSize => _half;
        public Vector3 AxisX => _axisX;
        public Vector3 AxisZ => _axisZ;
        // The short way across: how deep a hiding spot can sit before it is out the other side.
        public float Narrowest => Mathf.Min(_half.x, _half.y) * 2f;

        public Vector3 PointAt(float x, float z) => _centre + _axisX * x + _axisZ * z;

        public bool Contains(Vector3 point)
        {
            Local(point, out float x, out float z);
            return Mathf.Abs(x) <= _half.x && Mathf.Abs(z) <= _half.y;
        }

        // The nearest spot inside the patch that is at least `margin` in from its edge (the middle
        // line of a patch narrower than twice the margin). Keeps y.
        public Vector3 Inset(Vector3 point, float margin)
        {
            Local(point, out float x, out float z);
            float reachX = Mathf.Max(0f, _half.x - margin);
            float reachZ = Mathf.Max(0f, _half.y - margin);
            Vector3 inside = PointAt(Mathf.Clamp(x, -reachX, reachX), Mathf.Clamp(z, -reachZ, reachZ));
            inside.y = point.y;
            return inside;
        }

        // How far `point` is outside the patch (0 inside).
        public float DistanceTo(Vector3 point)
        {
            Local(point, out float x, out float z);
            float outX = Mathf.Max(0f, Mathf.Abs(x) - _half.x);
            float outZ = Mathf.Max(0f, Mathf.Abs(z) - _half.y);
            return Mathf.Sqrt(outX * outX + outZ * outZ);
        }

        // Where the straight walk a → b runs through the patch, as how far along it (0–1) it goes
        // in and comes out. False when it misses the patch.
        public bool TryChord(Vector3 a, Vector3 b, out float enters, out float leaves)
        {
            Local(a, out float ax, out float az);
            Local(b, out float bx, out float bz);

            enters = 0f;
            leaves = 1f;
            return Clip(ax, bx - ax, _half.x, ref enters, ref leaves) &&
                   Clip(az, bz - az, _half.y, ref enters, ref leaves) &&
                   enters <= leaves;
        }

        // The side of the patch a point on or near its edge belongs to, as the way out of it
        // (a flat unit vector): the axis it is furthest along, relative to the patch's size.
        public Vector3 OutwardAt(Vector3 point)
        {
            Local(point, out float x, out float z);
            float alongX = x / _half.x;
            float alongZ = z / _half.y;
            return Mathf.Abs(alongX) >= Mathf.Abs(alongZ) ? _axisX * Mathf.Sign(alongX == 0f ? 1f : alongX)
                                                         : _axisZ * Mathf.Sign(alongZ == 0f ? 1f : alongZ);
        }

        // Slab test on one axis: shrinks [enters, leaves] to where start + t·delta stays within ±reach.
        private static bool Clip(float start, float delta, float reach, ref float enters, ref float leaves)
        {
            if (Mathf.Abs(delta) < 0.00001f) return Mathf.Abs(start) <= reach;

            float t0 = (-reach - start) / delta;
            float t1 = ( reach - start) / delta;
            if (t0 > t1) (t0, t1) = (t1, t0);
            enters = Mathf.Max(enters, t0);
            leaves = Mathf.Min(leaves, t1);
            return enters <= leaves;
        }

        private void Local(Vector3 point, out float x, out float z)
        {
            Vector3 offset = point - _centre;
            x = offset.x * _axisX.x + offset.z * _axisX.z;
            z = offset.x * _axisZ.x + offset.z * _axisZ.z;
        }
    }
}
