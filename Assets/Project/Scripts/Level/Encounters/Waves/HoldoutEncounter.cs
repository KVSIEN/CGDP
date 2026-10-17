using UnityEngine;
using CGD.Feedback;
using CGD.Interaction;

namespace CGD.Level
{
    // Holdout: an uplink terminal in the room. Starting it seals the room and enemies pour
    // in on a timer until the upload finishes; then the shutters open, a reward drops and
    // (optionally) the ship's whole map is downloaded. Stragglers are left to the player.
    public class HoldoutEncounter : RoomEncounter
    {
        private const float CountdownStep = 15f;

        private WaveSchedule _schedule;
        private float _nextCountdown;

        protected override void OnBegin()
        {
            if (Context.Settings.TerminalPrefab == null) return;

            GameObject terminal = Instantiate(Context.Settings.TerminalPrefab, Context.TakeSpot(), Quaternion.identity, Context.Level);
            if (!terminal.TryGetComponent(out LockTerminal console)) return;
            console.SetLabel("Start uplink");
            console.SwitchedOn += _ => StartUplink();
        }

        protected override void OnPlayerEntered() =>
            Notify("Uplink terminal — starting it will draw every hostile nearby");

        private void StartUplink()
        {
            _schedule = new WaveSchedule(Context.Settings.HoldoutDuration, Context.Settings.HoldoutWaveInterval);
            _nextCountdown = _schedule.Duration - CountdownStep;
            Context.Seal.Seal();
            Notify($"Uplink started — hold out for {Mathf.CeilToInt(_schedule.Duration)} s", NotificationStyle.Danger);
        }

        protected override void Tick()
        {
            if (_schedule == null) return;

            int waves = _schedule.Advance(Time.deltaTime);
            for (int i = 0; i < waves; i++) SpawnWave(fromSecondRoster: Context.Random.Chance(0.5f));

            if (!_schedule.IsFinished)
            {
                if (_schedule.Remaining > _nextCountdown) return;
                Notify($"Uplink {Mathf.CeilToInt(_schedule.Remaining)} s", NotificationStyle.Warning);
                _nextCountdown -= CountdownStep;
                return;
            }

            _schedule = null;
            Context.Seal.Release();
            SpawnReward(Context.Center);
            if (Context.Settings.HoldoutRevealsMap && Context.WorldMap != null && Context.WorldMap.Fog != null)
            {
                Context.WorldMap.Fog.RevealAll();
                Notify("Uplink complete — ship map downloaded", NotificationStyle.Success);
            }
            else
            {
                Notify("Uplink complete", NotificationStyle.Success);
            }
        }
    }
}
