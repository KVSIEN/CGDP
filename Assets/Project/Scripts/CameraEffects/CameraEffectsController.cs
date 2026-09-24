using UnityEngine;
using UnityEngine.Rendering;
using CGD.Combat;

namespace CGD.CameraEffects
{
    // Reusable camera responses — shake, kicks, recoil, FOV punches, lag and blends to
    // other viewpoints — layered on top of whatever pose gameplay gave the camera.
    //
    // Effects are applied only while this camera renders and undone straight after, so
    // gameplay never sees them: shots, aim raycasts and PlayerCamera's own smoothing
    // all keep reading the clean pose, and a shaking camera never throws off your aim.
    // Lives on the Main Camera next to PlayerCamera and runs after it.
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Camera))]
    public class CameraEffectsController : MonoBehaviour
    {
        [SerializeField] private CameraEffectSettings _settings;
        [Tooltip("Optional — shake when this character takes damage (usually the Player)")]
        [SerializeField] private HealthManager _shakeOnDamageOf;

        private readonly TraumaShake _shake = new();
        private readonly SpringKick  _kick  = new();
        private readonly FovKick     _fov   = new();
        private readonly CameraLag   _lag   = new();
        private readonly ViewBlend   _blend = new();

        private Camera       _camera;
        private CameraOffset _offset;
        private Vector3      _lagOffset;

        private bool       _applied;
        private Vector3    _restorePosition;
        private Quaternion _restoreRotation;
        private float      _restoreFov;

        public float Trauma => _shake.Trauma;

        private void Awake()
        {
            TryGetComponent(out _camera);
            if (_settings == null) _settings = ScriptableObject.CreateInstance<CameraEffectSettings>();
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering   += OnEndCameraRendering;
            CameraImpulses.Emitted += OnImpulse;
            if (_shakeOnDamageOf != null) _shakeOnDamageOf.OnDamaged += OnDamaged;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering   -= OnEndCameraRendering;
            CameraImpulses.Emitted -= OnImpulse;
            if (_shakeOnDamageOf != null) _shakeOnDamageOf.OnDamaged -= OnDamaged;
            Restore();
        }

        // --- API -------------------------------------------------------------------

        // 0..1; shake strength follows trauma² by default.
        public void AddTrauma(float amount) => _shake.AddTrauma(amount);

        // An instant jolt, in degrees/second (x = pitch down, y = yaw right, z = roll)
        // and metres/second in camera space.
        public void Kick(Vector3 angularVelocity, Vector3 velocity = default) => _kick.Kick(angularVelocity, velocity);

        // Visual kick to go with a weapon's aim recoil (degrees up / right).
        public void AddRecoil(float pitch, float yaw) =>
            _kick.Kick(new Vector3(-pitch, yaw, 0f) * _settings.RecoilViewKick, Vector3.zero);

        // Positive widens the view momentarily.
        public void KickFov(float degrees) => _fov.Kick(degrees);

        public void BlendTo(Transform viewpoint, float duration) => _blend.BlendTo(viewpoint, duration);

        public void ReleaseBlend(float duration) => _blend.Release(duration);

        // Drops every running effect (respawn, cutscene cut).
        public void ClearAll()
        {
            _shake.Clear();
            _kick.Clear();
            _fov.Clear();
            _lag.Snap();
            _blend.Clear();
        }

        // --- Evaluation ------------------------------------------------------------

        // After PlayerCamera has placed the camera for this frame.
        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            _offset    = _shake.Evaluate(dt, _settings) + _kick.Evaluate(dt, _settings) + _fov.Evaluate(dt, _settings);
            _lagOffset = _lag.Evaluate(transform.position, dt, _settings);
            _blend.Advance(dt);
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera || _applied) return;

            _restorePosition = transform.position;
            _restoreRotation = transform.rotation;
            _restoreFov      = _camera.fieldOfView;
            _applied         = true;

            Vector3    position = _restorePosition + _lagOffset;
            Quaternion rotation = _restoreRotation;
            _blend.Apply(ref position, ref rotation);

            rotation *= Quaternion.Euler(_offset.Rotation);
            transform.SetPositionAndRotation(position + rotation * _offset.Position, rotation);
            _camera.fieldOfView = Mathf.Clamp(_restoreFov + _offset.Fov, 1f, 179f);
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera) Restore();
        }

        private void Restore()
        {
            if (!_applied) return;

            _applied = false;
            transform.SetPositionAndRotation(_restorePosition, _restoreRotation);
            _camera.fieldOfView = _restoreFov;
        }

        private void OnImpulse(Vector3 position, float radius, float trauma)
        {
            float distance = Vector3.Distance(position, transform.position);
            float falloff  = 1f - Mathf.Clamp01(distance / radius);
            if (falloff > 0f) AddTrauma(trauma * falloff * falloff);
        }

        private void OnDamaged(float amount) =>
            AddTrauma(Mathf.Min(amount * _settings.TraumaPerDamage, _settings.MaxDamageTrauma));
    }
}
