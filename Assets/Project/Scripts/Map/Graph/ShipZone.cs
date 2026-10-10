namespace CGD.Map
{
    // The broad parts of the ship (GDD: Ship Zones and Adjacency). Rooms in the same zone
    // link more often than rooms in different ones. New zones go at the end: assets store
    // these as numbers.
    public enum ShipZone
    {
        None,
        Civilian,    // accommodations, recreation, entertainment, commercial, worship
        Operations,  // engineering, command, transit, agriculture
        Services,    // medical, enforcement, science
    }
}
