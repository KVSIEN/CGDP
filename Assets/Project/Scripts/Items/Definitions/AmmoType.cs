namespace CGD.Items
{
    // The GDD's ammo economies, refined into caliber pools so weapon combinations
    // create real supply pressure. Two SMGs share a pool (great spray, no diversity);
    // an SMG + sniper cover two pools (less redundancy, wider coverage). A magnum
    // revolver or Deagle draws from the same scarce heavy pool as a sniper, so a
    // hand cannon costs the same round a headshot rifle would.
    //
    // None covers melee; Cooldown covers VOID relics, which are paced by time
    // rather than supply and never draw from a pool.
    public enum AmmoType
    {
        None           = 0,
        Arrows         = 1,   // BIO bows + crossbow bolts (merged for now)
        LightRounds    = 2,   // 9mm-class: SMGs, standard pistols
        StandardRounds = 3,   // 5.56/7.62 intermediate: ARs, carbines, LMGs
        HeavyRounds    = 4,   // .308+/.50 AE/.44 Magnum: snipers, DMRs, hand cannons
        ShotgunShells  = 5,
        EnergyCells    = 6,   // TECH energy weapons
        Cooldown       = 7,   // VOID relics — no pool, timer-driven
    }
}
