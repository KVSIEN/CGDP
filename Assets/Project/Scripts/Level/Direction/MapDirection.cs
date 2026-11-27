using System.Collections.Generic;

namespace CGD.Level
{
    // What the map director decided for a map (MapDirector.Plan): every room's beat, how much its
    // size is scaled for it, and what the pacing pass changed and found (DirectorFinding). A room
    // it has no word on is None at size 1. These are the beats planned from the graph; a room's
    // own LevelRoom.Beat is the final one (a peak can move once the rooms are placed).
    public sealed class MapDirection
    {
        public static readonly MapDirection None = new(new Dictionary<int, RoomBeat>(), new Dictionary<int, float>(), new List<DirectorFinding>());

        private readonly Dictionary<int, RoomBeat> _beats;
        private readonly Dictionary<int, float> _sizes;

        public MapDirection(Dictionary<int, RoomBeat> beats, Dictionary<int, float> sizes, List<DirectorFinding> findings)
        {
            _beats = beats;
            _sizes = sizes;
            Findings = findings;
        }

        public IReadOnlyDictionary<int, RoomBeat> Beats => _beats;
        public IReadOnlyList<DirectorFinding> Findings { get; }

        public RoomBeat BeatOf(int id) => _beats.TryGetValue(id, out RoomBeat beat) ? beat : RoomBeat.None;

        public float SizeScaleOf(int id) => _sizes.TryGetValue(id, out float size) ? size : 1f;
    }
}
