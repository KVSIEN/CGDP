namespace CGD.Enemies
{
    // Who leads an encounter by its faction's rule (SuccessionRule). Pure, so it can be tested.
    public static class Succession
    {
        // Index of the member who should lead, or -1 with none.
        public static int PickLead(RoleCandidate[] candidates, int count, SuccessionRule rule)
        {
            int   best      = -1;
            float bestClaim = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                float claim = Claim(candidates[i], rule);
                if (claim <= bestClaim) continue;
                bestClaim = claim;
                best      = i;
            }
            return best;
        }

        // Tier always counts; within a tier, seniority (rank), health (strength) or ward (depth)
        // decides. Strength lets health outweigh a tier: a fresh tier 2 beats a tier 3 at 30%.
        private static float Claim(in RoleCandidate candidate, SuccessionRule rule) => rule switch
        {
            SuccessionRule.Strength => candidate.Tier + candidate.HealthShare * 1.5f,
            SuccessionRule.Depth    => candidate.Tier + candidate.ShieldShare * 0.5f,
            _                       => candidate.Tier * 1000f + candidate.Seniority,
        };
    }
}
