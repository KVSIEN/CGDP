namespace CGD.Level
{
    // One thing the map director found in a map: which check, the room it is about (-1 for the
    // map as a whole), what it found in words, and whether the director already put it right
    // (Fixed: a note of what it changed, not a problem left).
    public readonly struct DirectorFinding
    {
        public DirectorFinding(DirectorCheck check, int roomId, string message, bool fixedByDirector = false)
        {
            Check   = check;
            RoomId  = roomId;
            Message = message;
            Fixed   = fixedByDirector;
        }

        public DirectorCheck Check   { get; }
        public int           RoomId  { get; }
        public string        Message { get; }
        public bool          Fixed   { get; }

        public override string ToString() => Fixed ? $"{Check} (fixed): {Message}" : $"{Check}: {Message}";
    }
}
