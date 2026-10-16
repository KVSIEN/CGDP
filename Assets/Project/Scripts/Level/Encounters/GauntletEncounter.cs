using System.Collections.Generic;
using CGD.Combat;
using CGD.Feedback;
using CGD.Stats;

namespace CGD.Level
{
    // Gauntlet: stepping in seals the room and waves follow one another, each once the last
    // is dead and each bigger than the one before. Every wave lays a random twist on the
    // player (a StatBoon) that lasts until the wave is cleared. Clearing the last opens the
    // room; the reward gets luckier with every wave survived.
    public class GauntletEncounter : RoomEncounter
    {
        private CharacterStats _stats;
        private StatBoon       _twist;
        private ObjectiveStatus _status;

        protected override void OnPlayerEntered()
        {
            if (Context.Player != null) _stats = Context.Player.GetComponentInParent<CharacterStats>();

            int waves = Context.Settings.GauntletWaves;
            _status = new ObjectiveStatus("Gauntlet");
            ObjectiveFeed.Show(_status);
            Context.Seal.Seal();
            Notify("Gauntlet — survive every wave", NotificationStyle.Danger);
            RunWaves(waves, wave => StartWave(wave, waves), () =>
            {
                EndTwist();
                ObjectiveFeed.Clear(_status);
                Context.Seal.Release();
                Notify("Gauntlet cleared", NotificationStyle.Success);
                SpawnReward(Context.Center, waves * Context.Settings.GauntletLuckPerWave);
            });
        }

        private List<HealthManager> StartWave(int wave, int waves)
        {
            EndTwist();
            BeginTwist();
            _status.Detail = _twist != null ? $"Wave {wave + 1}/{waves} - {_twist.Label}" : $"Wave {wave + 1}/{waves}";
            int size = Context.WaveSize + wave * Context.Settings.GauntletWaveGrowth;
            return Context.Spawner.Spawn(size, fromSecondRoster: wave % 2 == 1);
        }

        private void BeginTwist()
        {
            StatBoon[] twists = Context.Settings.GauntletTwists;
            if (_stats == null || twists.Length == 0) return;

            _twist = twists[Context.Random.Range(0, twists.Length)];
            _stats.AddPreset(_twist);
            Notify(_twist.Label, NotificationStyle.Warning);
        }

        private void EndTwist()
        {
            if (_twist == null) return;

            _stats.RemoveFrom(_twist);
            _twist = null;
        }
    }
}
