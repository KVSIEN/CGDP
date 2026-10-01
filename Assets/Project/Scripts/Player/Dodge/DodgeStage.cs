using System;
using UnityEngine;

namespace CGD.Player
{
    // One movement of a dodge: a sidestep, a roll, a dash. Sets the player's horizontal
    // velocity for Duration seconds; then the next stage follows (see Advance).
    [Serializable]
    public class DodgeStage
    {
        [Tooltip("Shown on the dodge HUD while this stage runs")]
        public string Label = "DODGE";

        [Header("Motion")]
        [Min(0.01f)] public float Duration = 0.3f;
        [Tooltip("Metres per second")]
        [Min(0f)] public float Speed = 10f;
        [Tooltip("Speed multiplier over the stage (0 = start, 1 = end). Flat = a constant burst; falling = a burst that eases out")]
        public AnimationCurve SpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        public DodgeSteering Steering = DodgeSteering.Locked;
        [Tooltip("Degrees per second, for TurnRate steering")]
        [Min(0f)] public float TurnRate = 180f;
        [Tooltip("Take the current movement input as the direction when this stage starts")]
        public bool Redirect = true;
        [Tooltip("Hold height instead of falling (air dashes)")]
        public bool IgnoreGravity;

        [Header("Commitment")]
        [Tooltip("No attacking, aiming, abilities or other actions until the stage ends")]
        public bool Commits;
        [Tooltip("Seconds into the stage during which damage is ignored (i-frames). Both 0 = none")]
        [Min(0f)] public float InvulnerableFrom;
        [Min(0f)] public float InvulnerableTo;
        public DodgePose Pose;

        [Header("Next")]
        public DodgeAdvance Advance = DodgeAdvance.Automatic;
        [Tooltip("For OnDodgePress: seconds after the movement in which the next press continues the dodge (presses during the movement count too)")]
        [Min(0f)] public float FollowUpWindow = 0.5f;
        [Tooltip("Cooldown when the dodge ends after this stage")]
        [Min(0f)] public float Cooldown = 1f;

        public bool IsInvulnerableAt(float time) =>
            InvulnerableTo > InvulnerableFrom && time >= InvulnerableFrom && time <= InvulnerableTo;
    }
}
