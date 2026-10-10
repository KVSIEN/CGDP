namespace CGD.Map
{
    // The kind of space a room was on the original CSS Paradise (GDD: Room Categories),
    // whichever reality holds it now. None leaves the choice to the level builder. New
    // categories go at the end: assets store these as numbers.
    public enum RoomCategory
    {
        None,
        Residential,    // homes split by class (quarters, suites) and the schools beside them
        Recreation,     // pool, park, gym, spa, observatory deck
        Entertainment,  // casino, theater, lounge, museum, ballroom
        Commercial,     // markets, restaurants and their kitchens, boutiques
        Medical,        // hospital, clinic, pharmacy, morgue
        Security,       // police and military: security offices, armories, barracks
        Engineering,    // the ship's machinery: engine room, reactor, maintenance, and industry (chemical plant, fabrication)
        Command,        // the bridge, control rooms, comms, offices
        Agriculture,    // hydroponics, farms, water treatment, food processing
        Science,        // research and education: laboratories, server rooms, libraries
        Transit,        // lobbies, cargo and docking bays, hangars, junctions
        Worship,        // chapel, temple, memorial shrine
    }
}
