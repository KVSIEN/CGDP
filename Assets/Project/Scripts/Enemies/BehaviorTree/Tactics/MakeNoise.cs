using System;
using UnityEngine;
using CGD.Audio;
using CGD.BehaviorTrees;
using CGD.Combat;

namespace CGD.Enemies
{
    // A lure: makes a noise somewhere it isn't — beside its target, or behind it — that every
    // listener treats as real (enemies of other sides turn to investigate; its kin come to see
    // what the fight is), with a sound the player hears from that spot. Turn toward the noise
    // and something else comes from the side. Succeeds at once; put a Cooldown above it.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Make Noise", "Makes a noise (and a sound) away from itself, beside or behind its target, to draw attention there. Succeeds at once.")]
    public sealed class MakeNoise : EnemyAction
    {
        public enum Place
        {
            // To one side of the target, a few metres off.
            BesideTarget,
            // Behind the target, as it faces.
            BehindTarget,
            // On the far side of itself from the target (draws the target past where it hides).
            BeyondSelf,
        }

        [SerializeField] private Place _where = Place.BesideTarget;
        [Tooltip("Metres from the reference point the noise comes from")]
        [SerializeField, Min(0f)] private float _offset = 7f;
        [Tooltip("Metres the noise carries")]
        [SerializeField, Min(1f)] private float _radius = 25f;
        [Tooltip("What the player hears there (footsteps, a cry, a shot). Empty = only enemies hear it")]
        [SerializeField] private SoundBank _sound;

        public override string Summary => $"{_where}, {_offset:0} m";

        protected override BtStatus OnTick(BtContext context)
        {
            EnemyPerception perception = Ai.Perception;
            if (perception.Target == null) return BtStatus.Failure;

            Vector3 spot = Spot(perception.TargetPosition, perception.TargetFacing);
            Noise.Emit(spot, _radius, DamageSource.Of(Ai.gameObject));
            _sound.TryPlay(spot + Vector3.up);
            return BtStatus.Success;
        }

        private Vector3 Spot(Vector3 target, Vector3 facing)
        {
            Vector3 toTarget = target - Ai.transform.position;
            toTarget.y = 0f;
            Vector3 forward = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Ai.transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, forward) * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
            return _where switch
            {
                Place.BehindTarget => target - (facing.sqrMagnitude > 0.01f ? facing : -forward) * _offset,
                Place.BeyondSelf   => Ai.transform.position - forward * _offset,
                _                  => target + side * _offset,
            };
        }
    }
}
