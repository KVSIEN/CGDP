using UnityEngine;
using CGD.Combat;
using CGD.Enemies;

namespace CGD.FiringRange
{
    // One firing-range dummy: stands in for an enemy (its stats, from EnemyHealth), shows
    // which faction that enemy belongs to by tinting its body, and keeps the head marked in
    // a fixed colour as the critical spot. Moving is TargetMover's job; RangeControl
    // drives all three through this component.
    [RequireComponent(typeof(EnemyHealth), typeof(TargetMover))]
    public class RangeDummy : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer _body;
        [Tooltip("The part that counts as a headshot")]
        [SerializeField] private Renderer _head;
        [SerializeField] private Color _critColor = new(0.9f, 0.15f, 0.15f);
        [Header("Hit flash")]
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField, Min(0.01f)] private float _flashTime = 0.12f;

        private EnemyHealth _health;
        private RespawnOnDeath _respawn;
        private StatusEffectController _statusEffects;
        private TargetMover _mover;
        private MaterialPropertyBlock _block;
        private Color _bodyColor = Color.white;
        private float _flash;

        public DummyMovement Movement
        {
            set => _mover.Movement = value;
        }

        // Back to full health with no status effects, even mid-respawn.
        public void Restore()
        {
            if (_statusEffects != null) _statusEffects.Clear();
            if (_respawn != null) _respawn.RespawnNow();
            else _health.Revive();
        }

        public void MoveTo(Vector3 position) => _mover.MoveHome(position);

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _mover  = GetComponent<TargetMover>();
            TryGetComponent(out _respawn);
            TryGetComponent(out _statusEffects);
            _block  = new MaterialPropertyBlock();
            Paint(_head, _critColor);
        }

        private void OnEnable() => _health.OnHit += StartFlash;

        private void OnDisable() => _health.OnHit -= StartFlash;

        // OnHit, not OnDamaged: a hit the shield soaks up flashes too.
        private void StartFlash(DamageInfo hit) => _flash = _flashTime;

        // Both parts blink toward the flash colour on a hit and settle back to their own.
        private void Update()
        {
            if (_flash <= 0f) return;

            _flash = Mathf.Max(0f, _flash - Time.deltaTime);
            float amount = _flash / _flashTime;
            Paint(_body, Color.Lerp(_bodyColor, _flashColor, amount));
            Paint(_head, Color.Lerp(_critColor, _flashColor, amount));
        }

        // Takes on the enemy's stats and shows its faction's colour.
        public void Play(EnemyData enemy, Color factionColor)
        {
            _health.UseData(enemy);
            _bodyColor = factionColor;
            Paint(_body, factionColor);
        }

        private void Paint(Renderer target, Color color)
        {
            target.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            target.SetPropertyBlock(_block);
        }
    }
}
