namespace CGD.Feedback
{
    // A preset to play, or a bare message to show, with how strongly to play it.
    public readonly struct FeedbackRequest
    {
        public FeedbackRequest(FeedbackPreset preset, string message, NotificationStyle style, float intensity)
        {
            Preset    = preset;
            Message   = message;
            Style     = style;
            Intensity = intensity;
        }

        public FeedbackPreset    Preset    { get; }
        // Already formatted; null for no message.
        public string            Message   { get; }
        public NotificationStyle Style     { get; }
        // Scales flash, vibration and shake (a small hit rumbles less than a big one).
        public float             Intensity { get; }
    }
}
