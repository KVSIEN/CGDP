using System;
using UnityEngine;
using CGD.Enemies;
using CGD.Feedback;

namespace CGD.Director
{
    // What happens when the pacing enters one phase: a line in the HUD feed, and/or an enemy
    // near the player shouting to the others ("Push!", "Hold position"). Either part is optional.
    [Serializable]
    public class NarrationBeat
    {
        [SerializeField] private PacingPhase _phase = PacingPhase.SustainPeak;
        [Tooltip("Shown in the HUD feed. Empty = no message")]
        [SerializeField] private string _message;
        [SerializeField] private NotificationStyle _style = NotificationStyle.Warning;
        [Tooltip("Said by the nearest enemy that knows something is up, within the settings' bark radius. None = no bark")]
        [SerializeField] private BarkKind _enemyBark = BarkKind.None;
        [Tooltip("Share of the times this phase begins that the beat plays")]
        [SerializeField, Range(0f, 1f)] private float _chance = 1f;

        public PacingPhase       Phase     => _phase;
        public string            Message   => _message;
        public NotificationStyle Style     => _style;
        public BarkKind          EnemyBark => _enemyBark;
        public float             Chance    => _chance;
    }
}
