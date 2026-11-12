namespace CGD.Enemies
{
    // What an encounter knows about one member when it hands out roles and picks a lead:
    // only what that member believes (its distance to where it thinks the target is, which
    // side of the target it stands on as far as it knows) and its own state.
    public readonly struct RoleCandidate
    {
        public readonly bool      IsRanged;
        // Metres to where it believes the target is.
        public readonly float     Distance;
        // 0 = right in front of where the target was facing, 1 = behind it.
        public readonly float     OffAxis;
        public readonly float     HealthShare;
        public readonly float     ShieldShare;
        public readonly int       Tier;
        public readonly bool      InCover;
        // Seconds it has been part of this encounter.
        public readonly float     Seniority;
        public readonly bool      IsLead;
        public readonly SquadRole CurrentRole;

        public RoleCandidate(bool isRanged, float distance, float offAxis, float healthShare, float shieldShare,
                             int tier, bool inCover, float seniority, bool isLead, SquadRole currentRole)
        {
            IsRanged    = isRanged;
            Distance    = distance;
            OffAxis     = offAxis;
            HealthShare = healthShare;
            ShieldShare = shieldShare;
            Tier        = tier;
            InCover     = inCover;
            Seniority   = seniority;
            IsLead      = isLead;
            CurrentRole = currentRole;
        }
    }
}
