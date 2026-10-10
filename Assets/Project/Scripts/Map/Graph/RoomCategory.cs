namespace CGD.Map
{
    // The kind of space a room was on the original CSS Paradise (GDD: Room Categories),
    // whichever reality holds it now. None leaves the choice to the level builder. New
    // categories go at the end: assets store these as numbers.
    public enum RoomCategory
    {
        None,
        Accommodation,  // living quarters split by class, and the schools beside them
        Recreation,     // pool, park, gym, spa, observatory deck
        Entertainment,  // casino, theater, lounge, museum, ballroom
        Commercial,     // markets, restaurants and their kitchens, boutiques
        Medical,        // hospital, clinic, pharmacy, morgue
        Enforcement,    // police and military: stations, checkpoints, holding cells, armories, barracks
        Engineering,    // the ship's machinery: engine room, reactor, maintenance, and industry (chemical plant, fabrication)
        Command,        // bridge, comms, offices, records archive
        Agriculture,    // hydroponics, greenhouse, livestock, water treatment
        Science,        // research and education: laboratories, server rooms, libraries
        Transit,        // cargo bay, docking port, hangar, tram station
        Worship,        // chapel, temple, memorial shrine
    }
}
