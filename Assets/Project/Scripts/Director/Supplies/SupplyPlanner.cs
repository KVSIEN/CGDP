using System;

namespace CGD.Director
{
    // Picks what the player needs most, like Left 4 Dead's director handing out health and
    // ammo by the survivors' state: each need is scored by how far below its threshold the
    // player is (0 at the threshold, 1 at nothing left) and the most urgent one wins.
    public class SupplyPlanner
    {
        private readonly SupplyTuning _tuning;

        public SupplyPlanner(SupplyTuning tuning) => _tuning = tuning;

        public SupplyKind Plan(in SupplyNeeds needs)
        {
            float healing = HealingUrgency(needs);
            float ammo    = AmmoUrgency(needs);
            if (healing <= 0f && ammo <= 0f) return SupplyKind.None;
            return healing >= ammo ? SupplyKind.Healing : SupplyKind.Ammo;
        }

        private float HealingUrgency(in SupplyNeeds needs)
        {
            if (needs.HealingCarried >= _tuning.HealingCarriedEnough) return 0f;
            return Shortfall(needs.HealthShare, _tuning.HealthBelow);
        }

        private float AmmoUrgency(in SupplyNeeds needs) => Shortfall(needs.ReserveMagazines, _tuning.MagazinesBelow);

        private static float Shortfall(float value, float threshold)
        {
            if (threshold <= 0f || value >= threshold) return 0f;
            return Math.Clamp(1f - value / threshold, 0f, 1f);
        }
    }
}
