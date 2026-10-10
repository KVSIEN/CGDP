namespace CGD.Map
{
    // The kind of space a room was on the original CSS Paradise (GDD: Room Categories),
    // whichever reality holds it now. None leaves the choice to the level builder. New
    // categories go at the end: assets store these as numbers.
    public enum RoomCategory
    {
        None,
        Residential,    // quarters, cabin rows and commons for every class, and the schools beside them
        Recreation,     // pool, park, gym, spa, observatory deck
        Entertainment,  // casino, theater, lounge, museum, ballroom
        Commercial,     // the promenade, restaurants, food courts and their kitchens
        Medical,        // medical bay, pharmacy, wards, morgue
        Security,       // police and military: security offices, armories, barracks
        Engineering,    // the ship's machinery: engine room, reactor, life support, workshops, processing plants
        Command,        // the bridge, control rooms, offices
        Agriculture,    // hydroponics, farms, water treatment
        Science,        // research and education: laboratories, data centers, libraries
        Transit,        // lobbies, cargo and docking bays, hangars, escape pods, junctions
        Worship,        // chapel, shrine
    }
}
