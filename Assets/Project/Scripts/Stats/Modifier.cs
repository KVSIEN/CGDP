namespace CGD.Stats
{
    // One change to a stat at runtime, remembered with where it came from so every
    // modifier from a source (an attachment, a buff, a difficulty preset) can be removed
    // together.
    public readonly struct Modifier
    {
        public Modifier(StatModifierOp op, float value, object source)
        {
            Op     = op;
            Value  = value;
            Source = source;
        }

        public StatModifierOp Op     { get; }
        public float          Value  { get; }
        public object         Source { get; }
    }
}
