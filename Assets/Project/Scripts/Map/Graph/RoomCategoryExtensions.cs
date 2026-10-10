namespace CGD.Map
{
    public static class RoomCategoryExtensions
    {
        // The zone a category belongs to (GDD table); None for RoomCategory.None.
        public static ShipZone Zone(this RoomCategory category) => category switch
        {
            RoomCategory.Accommodation or RoomCategory.Recreation or RoomCategory.Entertainment
                or RoomCategory.Commercial or RoomCategory.Worship => ShipZone.Civilian,
            RoomCategory.Engineering or RoomCategory.Command or RoomCategory.Transit
                or RoomCategory.Agriculture => ShipZone.Operations,
            RoomCategory.Medical or RoomCategory.Enforcement or RoomCategory.Science => ShipZone.Services,
            _ => ShipZone.None,
        };
    }
}
