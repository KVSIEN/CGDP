using UnityEngine;
using CGD.Feedback;
using CGD.Loot;

namespace CGD.Level
{
    // Ambush: a Treasure room in every way the player can see — sign, cache in the middle —
    // until the cache is opened. Then the doors seal and waves drop in until the room is
    // clear. The loot is real; it just costs a fight.
    public class AmbushEncounter : RoomEncounter
    {
        private bool _sprung;

        protected override void OnBegin()
        {
            GameObject cache = SpawnReward(Context.Center);
            if (cache != null && cache.TryGetComponent(out LootContainer container))
                container.Opened += Spring;
        }

        private void Spring()
        {
            if (_sprung) return;
            _sprung = true;

            Context.Seal.Seal();
            Notify("It's a trap!", NotificationStyle.Danger);
            RunWaves(Context.Settings.AmbushWaves, wave => SpawnWave(fromSecondRoster: wave % 2 == 1), () =>
            {
                Context.Seal.Release();
                Notify("Ambush survived", NotificationStyle.Success);
            });
        }
    }
}
