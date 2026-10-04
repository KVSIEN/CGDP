namespace CGD.Combat
{
    // Which way a reflected projectile or timeline is sent.
    public enum ReflectAim
    {
        Aim,        // where the character is looking (aim-based deflect)
        ToAttacker, // straight back at whoever dealt the hit
        Mirror,     // bounced off the character's facing like a mirror
    }
}
