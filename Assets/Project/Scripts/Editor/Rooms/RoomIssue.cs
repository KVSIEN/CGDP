using UnityEngine;

namespace CGD.Editor
{
    // Something the Room Lab noticed about a room, and the tiles it is about (to highlight).
    public readonly struct RoomIssue
    {
        public enum Severity { Info, Warning, Error }

        public RoomIssue(Severity level, string text, RectInt? area = null)
        {
            Level = level;
            Text  = text;
            Area  = area;
        }

        public Severity Level { get; }
        public string   Text  { get; }
        public RectInt? Area  { get; }
    }
}
