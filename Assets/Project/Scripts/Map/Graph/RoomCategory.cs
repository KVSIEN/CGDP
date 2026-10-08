namespace CGD.Map
{
    // The kind of space a room was on the original CSS Paradise (GDD: Room Categories),
    // whichever reality holds it now. None leaves the choice to the level builder. New
    // categories go at the end: assets store these as numbers.
    public enum RoomCategory
    {
        None,
        Accommodation,  // living quarters, split by class
        Recreation,     // pool, park, gym, spa, observatory deck
        Entertainment,  // casino, theater, lounge, museum, ballroom
        Commercial,     // markets, restaurants, boutiques
        Medical,        // hospital, clinic, pharmacy, morgue
        Enforcement,    // armory, holding cells, checkpoints, barracks
        Engineering,    // engine room, reactor bay, maintenance tunnels
        Command,        // bridge, comms, offices, records archive
        Agriculture,    // hydroponics, greenhouse, livestock, water treatment
        Science,        // laboratory, library, specimen storage
        Transit,        // cargo bay, docking port, hangar, tram station
        Worship,        // chapel, temple, memorial shrine
    }
}
