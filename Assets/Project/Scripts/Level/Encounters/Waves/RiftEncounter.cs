using System.Collections.Generic;
using CGD.Combat;
using CGD.Feedback;

namespace CGD.Level
{
    // Rift: a tear between the room's two realities. Stepping in seals the room; waves
    // come through from one reality, then the other, the last from both at once. When the
    // last falls the rift collapses, the shutters open and a reward drops.
    public class RiftEncounter : RoomEncounter
    {
        protected override void OnPlayerEntered()
        {
            Context.Seal.Seal();
            Notify("A rift tears open", NotificationStyle.Danger);

            int waves = Context.Settings.RiftWaves;
            RunWaves(waves, wave => wave == waves - 1 && waves > 1 ? MixedWave() : SpawnWave(fromSecondRoster: wave % 2 == 1), () =>
            {
                Context.Seal.Release();
                Notify("The rift collapses", NotificationStyle.Success);
                SpawnReward(Context.TakeSpot());   // the rift's core stands in the middle
            });
        }

        private List<HealthManager> MixedWave()
        {
            int size  = Context.WaveSize;
            var wave  = Context.Spawner.Spawn(size / 2);
            wave.AddRange(Context.Spawner.Spawn(size - size / 2, fromSecondRoster: true));
            return wave;
        }
    }
}
