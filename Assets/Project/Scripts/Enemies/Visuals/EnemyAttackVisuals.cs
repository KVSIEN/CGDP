using UnityEngine;
using UnityEngine.Rendering;
using CGD.Combat;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Enemies
{
    // Placeholder effects for an enemy's own attacks, each part optional:
    //   muzzle   a flash where each ranged shot leaves it
    //   strike   a claw trail on the striking arm through the swing, and a slash flash in
    //            front of it as the blow lands (a stagger cancels both)
    //   hits     a burst on whatever its attacks damage
    // Uses the pooled ExplosionFlash prefab and one transparent unlit material.
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyAttackVisuals : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private const float MuzzleHeight  = 1.5f;   // where EnemyRangedCombat fires from…
        private const float MuzzleForward = 0.6f;   // …and how far in front
        private const float StrikeHeight  = 1.15f;
        private const float TrailLead     = 0.08f;  // seconds before the blow the claw trail starts
        private const float TrailLinger   = 0.2f;   // and after it, it keeps drawing

        [Tooltip("Pooled prefab with an ExplosionFlash (Prefabs/VFX/ExplosionFlash)")]
        [SerializeField] private GameObject _flash;
        [Tooltip("Transparent unlit material for the claw trail (e.g. Materials/ExplosionFlash)")]
        [SerializeField] private Material _material;

        [Header("Muzzle")]
        [SerializeField] private Color _muzzleColor = new(1f, 0.85f, 0.3f, 0.8f);
        [Tooltip("Metres. 0 = no muzzle flash")]
        [SerializeField, Min(0f)] private float _muzzleFlash;

        [Header("Strike (melee)")]
        [Tooltip("The arm that swings (EnemyPlaceholderAnimation's Arm); the claw trail follows it. Empty = no trail")]
        [SerializeField] private Transform _arm;
        [SerializeField] private Color _strikeColor = new(0.4f, 1f, 0.35f, 0.75f);
        [Tooltip("Metres across the slash in front of it as the blow lands. 0 = none")]
        [SerializeField, Min(0f)] private float _strikeFlash;

        [Header("Hits")]
        [SerializeField] private Color _hitColor = new(0.4f, 1f, 0.35f, 0.8f);
        [Tooltip("Metres. 0 = no burst on what it hits")]
        [SerializeField, Min(0f)] private float _hitFlash;

        private EnemyAI       _ai;
        private TrailRenderer _claw;
        private float _strikeAt = -1f;   // when the pending blow lands; < 0 = none
        private float _trailUntil;

        private void Awake()
        {
            _ai = GetComponent<EnemyAI>();
            if (_arm != null && _material != null) _claw = CreateClaw();
        }

        private void OnEnable()
        {
            _ai.Fired         += OnFired;
            _ai.AttackStarted += OnAttackStarted;
            _ai.Staggered     += CancelStrike;
            if (_hitFlash > 0f) CombatEvents.DamageDealt += OnDamageDealt;
        }

        private void OnDisable()
        {
            _ai.Fired         -= OnFired;
            _ai.AttackStarted -= OnAttackStarted;
            _ai.Staggered     -= CancelStrike;
            CombatEvents.DamageDealt -= OnDamageDealt;
            CancelStrike();
        }

        private void Update()
        {
            float now = Time.time;
            if (_strikeAt >= 0f)
            {
                if (_claw != null && now >= _strikeAt - TrailLead) _claw.emitting = true;
                if (now >= _strikeAt)
                {
                    _strikeAt   = -1f;
                    _trailUntil = now + TrailLinger;
                    if (_strikeFlash > 0f)
                        Flash(transform.position + Vector3.up * StrikeHeight + transform.forward * _ai.Data.AttackRange * 0.7f,
                              _strikeFlash * 0.5f, _strikeColor);
                }
            }

            if (_claw != null && _claw.emitting && _strikeAt < 0f && now >= _trailUntil) _claw.emitting = false;
        }

        private void OnFired()
        {
            if (_muzzleFlash <= 0f) return;
            Flash(transform.position + Vector3.up * MuzzleHeight + transform.forward * MuzzleForward, _muzzleFlash * 0.5f, _muzzleColor);
        }

        private void OnAttackStarted()
        {
            _strikeAt = Time.time + _ai.Data.AttackWindup;
            if (_claw != null) _claw.Clear();
        }

        // A stagger spoils the wind-up: no blow, no slash.
        private void CancelStrike()
        {
            _strikeAt = -1f;
            if (_claw != null) _claw.emitting = false;
        }

        private void OnDamageDealt(DamageReport report)
        {
            if (report.Source.Owner != gameObject || report.Amount <= 0f) return;
            Flash(report.Point, _hitFlash * 0.5f, _hitColor);
        }

        private void Flash(Vector3 position, float radius, Color color)
        {
            if (_flash == null) return;
            GameObject flash = PrefabPool.Spawn(_flash, position, Quaternion.identity);
            if (flash.TryGetComponent(out ExplosionFlash burst)) burst.Play(radius, color);
        }

        // At the arm's tip, so the trail traces the thrust.
        private TrailRenderer CreateClaw()
        {
            var tip = new GameObject("Claw Trail");
            tip.transform.SetParent(_arm, false);
            tip.transform.localPosition = Vector3.forward * 0.5f;

            var trail = tip.AddComponent<TrailRenderer>();
            trail.sharedMaterial    = _material;
            trail.time              = 0.15f;
            trail.widthMultiplier   = 0.3f;
            trail.widthCurve        = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.minVertexDistance = 0.03f;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows    = false;
            trail.emitting          = false;

            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, _strikeColor);
            trail.SetPropertyBlock(block);
            return trail;
        }
    }
}
