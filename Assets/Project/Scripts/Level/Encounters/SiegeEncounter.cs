using UnityEngine;
using CGD.Feedback;

namespace CGD.Level
{
    // Siege: an objective stands in the middle of the room. Stepping in seals the room and
    // waves come for it (and the player) on a timer. Hold until the timer runs out and a
    // reward drops; if the objective falls first, the room opens and there is nothing to win.
    public class SiegeEncounter : RoomEncounter
    {
        private SiegeObjective _objective;
        private WaveSchedule   _schedule;
        private bool           _over;

        protected override void OnBegin()
        {
            if (Context.Settings.SiegePrefab == null) return;

            _objective = Instantiate(Context.Settings.SiegePrefab, Context.Center, Quaternion.identity, Context.Level);
            _objective.OnDeath += Fail;
        }

        protected override void OnPlayerEntered()
        {
            if (_objective == null || _over) return;

            _schedule = new WaveSchedule(Context.Settings.SiegeDuration, Context.Settings.SiegeWaveInterval);
            Context.Seal.Seal();
            Notify($"Defend the objective for {Mathf.CeilToInt(_schedule.Duration)} s", NotificationStyle.Danger);
        }

        protected override void Tick()
        {
            if (_schedule == null) return;

            int waves = _schedule.Advance(Time.deltaTime);
            for (int i = 0; i < waves; i++) SpawnWave();
            if (!_schedule.IsFinished) return;

            _schedule = null;
            _over = true;
            _objective.OnDeath -= Fail;
            Context.Seal.Release();
            Notify("Siege survived", NotificationStyle.Success);
            SpawnReward(Context.Center);
        }

        private void Fail()
        {
            if (_over) return;

            _over = true;
            _schedule = null;
            Context.Seal.Release();
            Notify("The objective fell", NotificationStyle.Danger);
        }
    }
}
