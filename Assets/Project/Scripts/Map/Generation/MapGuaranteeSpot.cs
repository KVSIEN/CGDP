namespace CGD.Map
{
    // Where a guaranteed room goes (see MapGuarantee).
    public enum MapGuaranteeSpot
    {
        WithinDepth,       // Count rooms Depth rooms from Start, on a branch when one reaches that far
        BehindEveryGate,   // one in every area behind a Locked or Secret gate, at its far end when it can
        BeforeBoss,        // the main path room just before the Boss
    }
}
