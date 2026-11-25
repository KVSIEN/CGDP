using System.Collections.Generic;

namespace CGD.Level
{
    // Decides which rooms are overgrown, with what and where. In a level, a room grows what its
    // holder's faction design names (FactionDesign › Overgrowth), as strongly as that design
    // shows there (thinner toward the frontier of its territory). A Breach room whose holder
    // grows nothing is overgrown by the faction breaching it: on its own side where the room is
    // split (BreachSplit), all over where it isn't; a holder that grows keeps to its side the
    // same way. The Room Generator can instead fix one profile, or none, for its room.
    public sealed class OvergrowthSource
    {
        private readonly LevelBuildSettings _settings;
        private readonly bool _fixed;
        private readonly OvergrowthProfile _fixedProfile;

        private OvergrowthSource(LevelBuildSettings settings, bool isFixed, OvergrowthProfile fixedProfile)
        {
            _settings     = settings;
            _fixed        = isFixed;
            _fixedProfile = fixedProfile;
        }

        // Every room as its factions grow it.
        public static OvergrowthSource FromFactions(LevelBuildSettings settings) => new(settings, false, null);

        // Every room overgrown all over by `profile`, at full strength; null = no room is.
        public static OvergrowthSource Fixed(OvergrowthProfile profile) => new(null, true, profile);

        public bool TryGet(LevelRoom room, out OvergrowthRequest request)
        {
            request = default;
            if (room?.Structure == null) return false;

            if (_fixed)
            {
                request = new OvergrowthRequest(_fixedProfile);
                return _fixedProfile != null;
            }

            OvergrowthProfile holder = _settings.DesignFor(room.Faction)?.Overgrowth;
            OvergrowthProfile breach = room.BreachFaction != null ? _settings.DesignFor(room.BreachFaction)?.Overgrowth : null;
            if (holder == null && breach == null) return false;

            bool breachSide = holder == null;
            BreachSplit split = room.BreachFaction != null ? BreachSplit.Of(room.Structure, room.BreachEntrances) : null;
            HashSet<int> zones = split?.Zones(breachSide);
            RoomStructure structure = room.Structure;
            request = new OvergrowthRequest(breachSide ? breach : holder, breachSide ? 1f : room.DesignStrength,
                                            zones == null ? null : tile => zones.Contains(structure.ZoneOf(tile)));
            return true;
        }
    }
}
