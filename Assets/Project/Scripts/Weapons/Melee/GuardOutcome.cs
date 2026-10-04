namespace CGD.Weapons
{
    public enum GuardOutcome
    {
        Open,      // not guarding, or hit from outside the guard's arc: full damage
        Blocked,   // damage reduced, stamina paid
        Parried,   // raised just in time: no damage, the attacker is staggered
        Broken     // couldn't pay the stamina: the guard drops and the hit lands in full
    }
}
