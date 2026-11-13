using UnityEngine;

namespace CGD.Enemies
{
    // The alarm panels an enemy knows about — those near its post — and whether it has already
    // tried to raise one since it last headed back to its post (one try per alert).
    internal sealed class AlarmKnowledge
    {
        // Alarm panels within this distance of its post are ones it knows about.
        private const float KnownRadius = 25f;

        private readonly EnemyAI _ai;
        private bool _tried;

        public AlarmKnowledge(EnemyAI ai) => _ai = ai;

        // A panel near its post it can still raise, if this enemy raises alarms and hasn't tried yet.
        public bool TryFind(out AlarmPanel panel)
        {
            panel = null;
            return !_tried && TryFindIgnoringTried(out panel);
        }

        public bool TryFindIgnoringTried(out AlarmPanel panel)
        {
            panel = null;
            if (!_ai.Data.RaisesAlarm) return false;

            Vector3 position = _ai.transform.position;
            float best = float.MaxValue;
            foreach (AlarmPanel candidate in AlarmPanel.All)
            {
                if (!candidate.IsAvailable || candidate.Team != _ai.Health.Team) continue;
                if (!_ai.Post.IsNear(candidate.transform.position, KnownRadius)) continue;

                float distance = (candidate.transform.position - position).sqrMagnitude;
                if (distance < best) { best = distance; panel = candidate; }
            }
            return panel != null;
        }

        public void MarkTried() => _tried = true;

        // Stood down (heading home, respawned): it may try again next time.
        public void Forget() => _tried = false;
    }
}
