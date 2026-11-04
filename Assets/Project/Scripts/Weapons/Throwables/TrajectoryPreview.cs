using UnityEngine;

namespace CGD.Weapons
{
    // Draws where a throw would go while it is being aimed: the arc up to the first surface it
    // would hit, and a ring on that surface the size of the blast. Two line renderers, built
    // once; shown only while Show is called each frame.
    public class TrajectoryPreview : MonoBehaviour
    {
        private const int RingSegments = 48;
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

        // Steps the throw forward under gravity, stopping at the first thing it would hit.
        public void Show(Vector3 origin, Vector3 velocity, GrenadeData data)
        {
            if (_points == null || _points.Length != data.TrajectorySteps + 1)
                _points = new Vector3[data.TrajectorySteps + 1];

            int count = 0;
            bool landed = false;
            RaycastHit hit = default;
            Vector3 position = origin;
            _points[count++] = position;

            for (int i = 0; i < data.TrajectorySteps; i++)
            {
                float dt = data.TrajectoryStepTime;
                Vector3 next = position + velocity * dt + 0.5f * Physics.gravity * (dt * dt);

                landed = Physics.Linecast(position, next, out hit, data.HitMask, QueryTriggerInteraction.Ignore);
                if (landed)
                {
                    _points[count++] = hit.point;
                    break;
                }

                _points[count++] = next;
                velocity += Physics.gravity * dt;
                position = next;
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
