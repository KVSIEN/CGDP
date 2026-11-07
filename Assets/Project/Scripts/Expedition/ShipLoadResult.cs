namespace CGD.Expedition
{
    public enum ShipLoadResult
    {
        NoSave,
        Loaded,
        // The file was damaged or from an unknown version: it was moved aside and the ship starts empty.
        SetAside,
    }
}
