using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Enemies
{
    // How a faction behaves while nothing is going on: where it looks, how it walks its
    // route, where it stops and what it mutters. Scales the PerceptionProfile's guard look
    // settings. The defaults change nothing.
    [Serializable]
    public sealed class IdleDoctrine
    {
        [Header("Gaze (standing watch, at stops and halts)")]
        [SerializeField] private GazePattern _gaze = GazePattern.Scatter;
        [Tooltip("Scales the profile's seconds between glances (over 1 = long stares, under 1 = restless)")]
        [SerializeField, Min(0.1f)] private float _lookIntervalScale = 1f;
        [Tooltip("Scales the profile's arc either side of its facing, up to 180°")]
        [SerializeField, Min(0.1f)] private float _lookAngleScale = 1f;
        [Tooltip("Degrees per second a sweeping gaze moves")]
        [SerializeField, Min(1f)] private float _sweepSpeed = 35f;

        [Header("Walking its route")]
        [Tooltip("Seconds it waits at each waypoint, looking about. 0 = it walks straight on")]
        [SerializeField] private FloatRange _waypointDwell = new(0f, 0f);
        [Tooltip("Scales its patrol speed")]
        [SerializeField, Range(0.3f, 1.5f)] private float _patrolSpeedScale = 1f;
        [Tooltip("How much its pace drifts up and down as it walks, 0 = steady as a machine")]
        [SerializeField, Range(0f, 0.8f)] private float _paceDrift;
        [Tooltip("Times a minute it stops dead between waypoints for no reason")]
        [SerializeField, Min(0f)] private float _haltsPerMinute;
        [SerializeField] private FloatRange _haltDuration = new(2f, 4f);

        [Header("Standing watch at its post")]
        [Tooltip("Metres it wanders from its post now and then. 0 = it stays put")]
        [SerializeField, Min(0f)] private float _roamRadius;
        [Tooltip("Seconds between those wanders")]
        [SerializeField] private FloatRange _roamEvery = new(6f, 12f);

        [Header("Voice")]
        [Tooltip("Seconds between idle mutterings (the Ambient bark). 0 = silent")]
        [SerializeField] private FloatRange _mutterEvery = new(0f, 0f);

        public GazePattern Gaze              => _gaze;
        public float LookIntervalScale       => _lookIntervalScale;
        public float LookAngleScale          => _lookAngleScale;
        public float SweepSpeed              => _sweepSpeed;
        public FloatRange WaypointDwell      => _waypointDwell;
        public float PatrolSpeedScale        => _patrolSpeedScale;
        public float PaceDrift               => _paceDrift;
        public float HaltsPerMinute          => _haltsPerMinute;
        public FloatRange HaltDuration       => _haltDuration;
        public float RoamRadius              => _roamRadius;
        public FloatRange RoamEvery          => _roamEvery;
        public FloatRange MutterEvery        => _mutterEvery;
    }
}
