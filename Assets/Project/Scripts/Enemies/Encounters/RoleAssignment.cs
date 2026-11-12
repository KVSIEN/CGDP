namespace CGD.Enemies
{
    // Hands out an encounter's roles, slot by slot in order: the lead first where a slot asks
    // for it, then whoever fits best, until the slot is full; anyone left over gets the
    // fallback role. Pure, so it can be tested; the caller owns the arrays.
    public static class RoleAssignment
    {
        public static void Assign(RoleCandidate[] candidates, int count, RoleSlot[] slots, SquadRole fallback,
                                  SquadRole[] roles, bool[] taken)
        {
            float furthest = 0f;
            for (int i = 0; i < count; i++)
            {
                roles[i] = fallback;
                taken[i] = false;
                if (candidates[i].Distance > furthest) furthest = candidates[i].Distance;
            }
            if (slots == null) return;

            foreach (RoleSlot slot in slots)
            {
                if (slot == null) continue;
                int capacity = slot.Count <= 0 ? count : slot.Count;
                int filled   = 0;

                if (slot.LeadFirst)
                {
                    int lead = IndexOfLead(candidates, count);
                    if (lead >= 0 && !taken[lead] && slot.Accepts(candidates[lead]))
                    {
                        Give(lead, slot.Role, roles, taken);
                        filled++;
                    }
                }

                while (filled < capacity)
                {
                    int   best    = -1;
                    float bestFit = float.MinValue;
                    for (int i = 0; i < count; i++)
                    {
                        if (taken[i] || !slot.Accepts(candidates[i])) continue;
                        float fit = slot.Fit(candidates[i], furthest);
                        if (fit <= bestFit) continue;
                        bestFit = fit;
                        best    = i;
                    }
                    if (best < 0) break;

                    Give(best, slot.Role, roles, taken);
                    filled++;
                }
            }
        }

        private static void Give(int index, SquadRole role, SquadRole[] roles, bool[] taken)
        {
            roles[index] = role;
            taken[index] = true;
        }

        private static int IndexOfLead(RoleCandidate[] candidates, int count)
        {
            for (int i = 0; i < count; i++)
                if (candidates[i].IsLead) return i;
            return -1;
        }
    }
}
