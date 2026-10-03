using System;
using UnityEngine;

namespace CGD.Economy
{
    // The house's rules, Unity-independent: what each pull pays out, and how the price
    // climbs with every pull at the same machine.
    [Serializable]
    public class GambleOdds
    {
        [SerializeField, Min(0f)] private float _bustWeight    = 4f;
        [SerializeField, Min(0f)] private float _winWeight     = 5f;
        [SerializeField, Min(0f)] private float _jackpotWeight = 1f;
        [Tooltip("Each pull costs this much more than the last, as a share of the first price")]
        [SerializeField, Min(0f)] private float _priceGrowth = 0.5f;

        public GambleOdds() { }

        public GambleOdds(float bust, float win, float jackpot, float priceGrowth)
        {
            _bustWeight    = bust;
            _winWeight     = win;
            _jackpotWeight = jackpot;
            _priceGrowth   = priceGrowth;
        }

        // `roll` in [0, 1).
        public GambleResult Resolve(float roll)
        {
            float total = _bustWeight + _winWeight + _jackpotWeight;
            if (total <= 0f) return GambleResult.Bust;

            float pick = roll * total;
            if (pick < _bustWeight) return GambleResult.Bust;
            return pick < _bustWeight + _winWeight ? GambleResult.Win : GambleResult.Jackpot;
        }

        // `pulls` already made at this machine.
        public int PriceFor(int basePrice, int pulls) =>
            Mathf.RoundToInt(basePrice * (1f + _priceGrowth * Mathf.Max(0, pulls)));
    }
}
