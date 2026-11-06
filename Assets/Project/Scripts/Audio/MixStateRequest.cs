namespace CGD.Audio
{
    // One system asking for a MixState. Hold it while the situation lasts and Release it
    // when it ends; requests stack, so two systems never cancel each other out.
    public sealed class MixStateRequest
    {
        // Returned when there is no mixer controller, so callers never need a null check.
        public static readonly MixStateRequest Inert = new(null, null);

        private readonly AudioMixerController _controller;

        internal MixStateRequest(AudioMixerController controller, MixState state)
        {
            _controller = controller;
            State       = state;
        }

        public MixState State { get; }

        public void Release()
        {
            if (_controller != null) _controller.Release(this);
        }
    }
}
