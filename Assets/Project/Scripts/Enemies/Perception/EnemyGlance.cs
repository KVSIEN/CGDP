using UnityEngine;
using UnityEngine.AI;

namespace CGD.Enemies
{
    // An enemy on the move looking about instead of staring down its path or at its target: every
    // few seconds it turns its head to a flank, behind it, or where it last felt a threat, then goes
    // back to what it was doing. Perception, ground learning and belief all read where the body
    // faces, so each glance is also a chance to spot an enemy, learn the terrain and refresh what it
    // believes. The more on edge it is, the more often it looks.
    //
    // Only the moving enemy glances, and only when its facing is not committed to something (EnemyMotion
    // says so): never while aiming, attacking, hopping cover or dodging.
    internal sealed class EnemyGlance
    {
        private const float MinMoveSpeed   = 0.5f;
        private const float LookDistance   = 8f;
        private const float MaxThreatRange = 40f;

        // Seconds between glances: calm, wary (suspicious, searching, cautious) and in a fight.
        private static readonly Vector2 CalmGap    = new(4f, 8f);
        private static readonly Vector2 WaryGap    = new(2.2f, 4.5f);
        private static readonly Vector2 FightGap   = new(1.4f, 3f);
        private static readonly Vector2 HoldTime   = new(0.45f, 0.9f);
        private static readonly Vector2 SideAngle  = new(55f, 110f);
        private const float SecondLookChance = 0.45f;
        private const float RearAngleSpread  = 35f;

        private readonly EnemyAI _ai;

        private float   _nextGlanceAt;
        private bool    _scheduled;
        private bool    _looking;
        private float   _lookUntil;
        private Vector3 _lookPoint;
        private int     _looksLeft;

        public EnemyGlance(EnemyAI ai) => _ai = ai;

        // Where the enemy should be looking right now, if it is glancing. `facingFree` is false
        // while the enemy's facing is committed elsewhere, which ends any glance in progress.
        public bool TryGetLook(bool facingFree, float time, out Vector3 point)
        {
            point = _lookPoint;
            if (!facingFree || !IsMovingFreely())
            {
                _looking = false;
                _scheduled = false;
                return false;
            }

            if (!_scheduled)
            {
                _scheduled = true;
                _nextGlanceAt = time + Random.Range(CalmGap.x, CalmGap.y) * 0.5f;
            }

            if (_looking)
            {
                if (time < _lookUntil) return true;
                if (_looksLeft > 0)
                {
                    BeginLook(time, Vector3.zero, flip: true);
                    point = _lookPoint;
                    return true;
                }
                _looking = false;
                _nextGlanceAt = time + NextGap();
                return false;
            }

            if (time < _nextGlanceAt) return false;

            _looksLeft = Random.value < SecondLookChance ? 1 : 0;
            BeginLook(time, PickDirection(), flip: false);
            point = _lookPoint;
            return true;
        }

        private bool IsMovingFreely()
        {
            EnemyAI ai = _ai;
            if (ai.State is EnemyAI.AiState.Dead or EnemyAI.AiState.Stunned) return false;

            NavMeshAgent agent = ai.Agent;
            if (!agent.enabled || !agent.isOnNavMesh || agent.isStopped) return false;
            if (agent.velocity.sqrMagnitude < MinMoveSpeed * MinMoveSpeed) return false;

            return !ai.Vault.IsVaulting && !ai.IsAttacking && !ai.RangedCombat.IsBursting &&
                   !ai.Tells.IsCommitting && !ai.Reflexes.IsActive;
        }

        private void BeginLook(float time, Vector3 direction, bool flip)
        {
            _looking = true;
            _lookUntil = time + Random.Range(HoldTime.x, HoldTime.y);

            if (flip)
            {
                // A sweep: after one side, the other.
                _looksLeft--;
                Vector3 offset = _lookPoint - _ai.transform.position;
                direction = Quaternion.Euler(0f, 180f + Random.Range(-30f, 30f), 0f) * offset.normalized;
            }

            _lookPoint = _ai.transform.position + direction * LookDistance;
        }

        // What to look at: where it last felt a threat, behind it when it is wary, or a flank.
        private Vector3 PickDirection()
        {
            EnemyPerception perception = _ai.Perception;
            Vector3 position = _ai.transform.position;
            Vector3 heading  = Heading();
            bool wary   = IsWary();
            float roll  = Random.value;

            if (TryThreatDirection(perception, position, out Vector3 threat) && roll < 0.4f) return threat;
            if (wary && roll < 0.65f)
                return Quaternion.Euler(0f, 180f + Random.Range(-RearAngleSpread, RearAngleSpread), 0f) * heading;

            float side = Random.value < 0.5f ? -1f : 1f;
            return Quaternion.Euler(0f, side * Random.Range(SideAngle.x, SideAngle.y), 0f) * heading;
        }

        // The last place it believes a threat was or the fire came from, as a flat direction.
        private static bool TryThreatDirection(EnemyPerception perception, Vector3 position, out Vector3 direction)
        {
            direction = default;
            Vector3 toward;
            if (perception.IsUnderFire)
                toward = -perception.ThreatDirection;
            else if (perception.Target != null || perception.HasLead)
                toward = perception.LastKnownPosition - position;
            else
                return false;

            toward.y = 0f;
            if (toward.sqrMagnitude < 1f || toward.sqrMagnitude > MaxThreatRange * MaxThreatRange) return false;

            direction = toward.normalized;
            return true;
        }

        private Vector3 Heading()
        {
            Vector3 velocity = _ai.Agent.velocity;
            velocity.y = 0f;
            return velocity.sqrMagnitude > 0.01f ? velocity.normalized : _ai.transform.forward;
        }

        private bool IsWary() =>
            _ai.Perception.Level != AwarenessLevel.Unaware || _ai.Perception.IsCautious || _ai.Perception.HasLead;

        private float NextGap()
        {
            EnemyAI.AiState state = _ai.State;
            bool fighting = state is EnemyAI.AiState.Chase or EnemyAI.AiState.Retreat || _ai.Perception.IsUnderFire;
            Vector2 gap = fighting ? FightGap : IsWary() ? WaryGap : CalmGap;
            return Random.Range(gap.x, gap.y);
        }
    }
}
