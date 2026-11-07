using UnityEngine;

namespace CGD.Weapons
{
    // Draws where a throw would go while it is being aimed: the arc and its bounces up to where
    // it comes to rest, and a ring there the size of the blast. Two line renderers, built
    // once; shown only while Show is called each frame.
    public class TrajectoryPreview : MonoBehaviour
    {
        private const int RingSegments = 48;
        private const float SurfaceOffset = 0.02f; // lifts a bounce off the surface so the next step doesn't re-hit it
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Unlit material for the arc and the ring; the colour comes from the grenade")]
        [SerializeField] private Material _material;
        [SerializeField, Min(0.005f)] private float _lineWidth = 0.04f;

        private LineRenderer _arc;
        private LineRenderer _ring;
        private MaterialPropertyBlock _block;
        private Vector3[] _points;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _arc   = BuildLine("Arc", false);
            _ring  = BuildLine("BlastRing", true);
            Hide();
        }

        private void OnDisable() => Hide();

        // Steps the throw forward under the grenade's gravity, bouncing it the way the grenade
        // will bounce, until it settles (or, for an impact fuse, hits anything). The ring goes
        // where it last touched down.
        public void Show(Vector3 origin, Vector3 velocity, GrenadeData data)
        {
            if (_points == null || _points.Length != data.TrajectorySteps + 1)
                _points = new Vector3[data.TrajectorySteps + 1];

            int count = 0;
            bool landed = false;
            RaycastHit hit = default;
            Vector3 gravity = data.Gravity;
            Vector3 position = origin;
            _points[count++] = position;

            for (int i = 0; i < data.TrajectorySteps; i++)
            {
                float dt = data.TrajectoryStepTime;
                Vector3 next = position + velocity * dt + 0.5f * gravity * (dt * dt);

                if (!Physics.Linecast(position, next, out RaycastHit stepHit, data.HitMask, QueryTriggerInteraction.Ignore))
                {
                    _points[count++] = next;
                    velocity += gravity * dt;
                    position = next;
                    continue;
                }

                landed = true;
                hit = stepHit;
                _points[count++] = hit.point;

                Vector3 atHit = velocity + gravity * (dt * hit.distance / Vector3.Distance(position, next));
                if (data.Fuse == FuseMode.OnImpact || data.Settles(atHit, hit.normal)) break;

                velocity = data.BounceVelocity(atHit, hit.normal);
                position = hit.point + hit.normal * SurfaceOffset;
            }

            DrawArc(count, data.PreviewColor);
            if (landed) DrawRing(hit, data.ExplosionRadius, data.PreviewColor);
            else _ring.enabled = false;
        }

        public void Hide()
        {
            if (_arc != null)  _arc.enabled  = false;
            if (_ring != null) _ring.enabled = false;
        }

        private void DrawArc(int count, Color color)
        {
            _arc.positionCount = count;
            _arc.SetPositions(_points);
            Colour(_arc, color);
            _arc.enabled = true;
        }

        // A circle lying on the surface that was hit.
        private void DrawRing(RaycastHit hit, float radius, Color color)
        {
            if (radius <= 0f)
            {
                _ring.enabled = false;
                return;
            }

            Quaternion onSurface = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Vector3 centre = hit.point + hit.normal * 0.05f;
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                _ring.SetPosition(i, centre + onSurface * new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
            Colour(_ring, color);
            _ring.enabled = true;
        }

        private LineRenderer BuildLine(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace     = true;
            line.loop              = loop;
            line.widthMultiplier   = _lineWidth;
            line.sharedMaterial    = _material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows    = false;
            if (loop) line.positionCount = RingSegments;
            return line;
        }

        private void Colour(LineRenderer line, Color color)
        {
            _block.SetColor(BaseColorId, color);
            line.SetPropertyBlock(_block);
        }
    }
}
