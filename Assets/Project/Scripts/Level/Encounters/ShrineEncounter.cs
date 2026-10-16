using System.Collections.Generic;
using UnityEngine;
using CGD.Stats;

namespace CGD.Level
{
    // Shrine: a few altars, each offering a different boon. The player takes one and the
    // others close. No enemies, no sealing — the cost is in the boon itself.
    public class ShrineEncounter : RoomEncounter
    {
        private readonly List<ShrineAltar> _altars = new();

        protected override void OnBegin()
        {
            EncounterSettings settings = Context.Settings;
            if (settings.ShrineAltarPrefab == null || settings.ShrineBoons.Length == 0) return;

            var remaining = new List<StatBoon>(settings.ShrineBoons);
            int offers = Mathf.Min(settings.ShrineOffers, remaining.Count);
            for (int i = 0; i < offers; i++)
            {
                int pick = Context.Random.Range(0, remaining.Count);
                ShrineAltar altar = Instantiate(settings.ShrineAltarPrefab, Context.TakeSpot(), Quaternion.identity, Context.Level);
                altar.Offer(remaining[pick]);
                altar.Claimed += CloseOthers;
                _altars.Add(altar);
                remaining.RemoveAt(pick);
            }
        }

        protected override void OnPlayerEntered() => Notify("A shrine — one boon, then the others close");

        private void CloseOthers(ShrineAltar taken)
        {
            foreach (ShrineAltar altar in _altars)
                if (altar != taken) altar.Close();
        }
    }
}
