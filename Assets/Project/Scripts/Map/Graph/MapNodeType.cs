namespace CGD.Map
{
    // What the player experiences at a node. Start, Boss and Exit are structural:
    // the generator places them itself, so type rules never produce them. New types go at
    // the end: assets store these as numbers.
    public enum MapNodeType
    {
        Start,
        Combat,
        Elite,      // legacy: no longer generated (room tier 3 replaces it); old graphs treat it as a tier-3 fight
        Puzzle,
        Shop,
        Event,
        Treasure,
        Boss,
        Exit,
        Resupply,   // a calm stop with an ammo cache before more fighting
        Breach,     // a fight where two realities overlap: both factions' enemies
        Lockdown,   // the doors seal on entry and open once every enemy is dead
        Holdout,    // start an uplink, then survive the waves until it finishes
        Ambush,     // poses as a treasure room; taking the loot springs a lockdown
        Stealth,    // a guarded vault: stay unseen, or the alarm seals the room
        Rift,       // a tear between realities: waves from both factions, in turns
        Gamble,     // machines that take credits or health for a shot at loot
        EmergencyExit, // an escape pod: end the run early, keeping only part of the haul
        Hazard,     // a leak that keeps afflicting everyone inside until its vents are shut
        Quiet,      // a rare peaceful room: no enemies, always a resource, ammo cache or loot cache
        Junction,   // a crossroads where three or more routes meet: no encounter, just a place to choose a way
    }
}
