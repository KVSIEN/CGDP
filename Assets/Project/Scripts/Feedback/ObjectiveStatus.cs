using CGD.Combat;

namespace CGD.Feedback
{
    // What the objective panel shows while an encounter has a goal in progress. The
    // encounter owns it and keeps it current; the panel only reads it.
    public class ObjectiveStatus
    {
        public ObjectiveStatus(string title) => Title = title;

        public string Title { get; }

        // A second line, e.g. "Wave 2/4".
        public string Detail { get; set; }

        // Seconds left; negative hides the timer.
        public float TimeRemaining { get; set; } = -1f;

        // Shown as a health bar when set.
        public HealthManager Health { get; set; }
    }
}
