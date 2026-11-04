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

        private EnemyHealth _health;
        private RespawnOnDeath _respawn;
        private StatusEffectController _statusEffects;
        private TargetMover _mover;
        private MaterialPropertyBlock _block;

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

        // Takes on the enemy's stats and shows its faction's colour.
        public void Play(EnemyData enemy, Color factionColor)
        {
            _health.UseData(enemy);
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
