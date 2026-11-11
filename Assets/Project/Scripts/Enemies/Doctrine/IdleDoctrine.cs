using System;
using UnityEngine;

namespace CGD.Enemies
{
    // How a faction behaves while nothing is going on: a routine of beats it works through
    // while standing watch and at stops along its route, how it walks, and how it changes
    // when it is on edge. Scales the PerceptionProfile's guard look settings. Without beats an
    // enemy glances about at random and walks straight on, as a plain enemy does.
    [Serializable]
    public sealed class IdleDoctrine
    {
        [Header("Routine")]
        [Tooltip("What it does with idle time (standing watch, and stops on its route). Empty = random glances, no stops")]
        [SerializeField] private IdleBeat[] _beats = Array.Empty<IdleBeat>();
        [SerializeField] private IdleOrder _order = IdleOrder.Weighted;
        [Tooltip("Chance it stops at each waypoint to do a beat")]
        [SerializeField, Range(0f, 1f)] private float _waypointStopChance;
        [Tooltip("Times a minute it stops dead between waypoints to do a beat")]
        [SerializeField, Min(0f)] private float _haltsPerMinute;
        [Tooltip("Degrees per second a sweeping gaze moves")]
        [SerializeField, Min(1f)] private float _sweepSpeed = 35f;

        [Header("Walking")]
        [Tooltip("Scales its patrol speed")]
        [SerializeField, Range(0.3f, 1.5f)] private float _patrolSpeedScale = 1f;
        [Tooltip("How much its pace drifts up and down as it walks, 0 = steady as a machine")]
        [SerializeField, Range(0f, 0.8f)] private float _paceDrift;

        [Header("On edge")]
        [Tooltip("Scales how long each beat lasts while it is on edge (under 1 = jumpier, over 1 = more patient)")]
        [SerializeField, Min(0.1f)] private float _edgeBeatScale = 1f;
        [Tooltip("Scales a sweeping gaze's speed while it is on edge")]
        [SerializeField, Min(0.1f)] private float _edgeSweepScale = 1f;

        public IdleBeat[] Beats          => _beats;
        public IdleOrder  Order          => _order;
        public float WaypointStopChance  => _waypointStopChance;
        public float HaltsPerMinute      => _haltsPerMinute;
        public float SweepSpeed          => _sweepSpeed;
        public float PatrolSpeedScale    => _patrolSpeedScale;
        public float PaceDrift           => _paceDrift;
        public float EdgeBeatScale       => _edgeBeatScale;
        public float EdgeSweepScale      => _edgeSweepScale;
    }
}
