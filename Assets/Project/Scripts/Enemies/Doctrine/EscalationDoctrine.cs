using System;
using UnityEngine;

namespace CGD.Enemies
{
    // How a faction raises the stakes: how far its cries carry, whether it reports what it
    // finds, and whether it asks for help when it is losing. The defaults change nothing.
    [Serializable]
    public sealed class EscalationDoctrine
    {
        [Tooltip("Scales how far its call carries when it spots a hostile (a roar carries, a whisper does not)")]
        [SerializeField, Min(0f)] private float _calloutRadiusScale = 1f;
        [Tooltip("Finding a fallen ally sends its kin to look, over the radio")]
        [SerializeField] private bool _reportsFallenKin;
        [Tooltip("Its morale breaking calls its kin to the spot it is falling back from")]
        [SerializeField] private bool _callsForHelpWhenShaken;

        public float CalloutRadiusScale     => _calloutRadiusScale;
        public bool  ReportsFallenKin       => _reportsFallenKin;
        public bool  CallsForHelpWhenShaken => _callsForHelpWhenShaken;
    }
}
