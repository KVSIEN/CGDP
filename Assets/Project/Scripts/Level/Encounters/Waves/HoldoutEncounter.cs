using UnityEngine;
using CGD.Combat;
using CGD.Feedback;
using CGD.Interaction;

namespace CGD.Level
{
    // Holdout: an uplink terminal in the room. Starting it seals the room and enemies pour
    // in on a timer until the upload finishes; then the shutters open, a reward drops and
    // (optionally) the ship's whole map is downloaded. Stragglers are left to the player.
    // While it runs the uplink sends a signal out every few seconds that hostiles in earshot
    // go to investigate (a noise, so enemies react as they do to any other: by suspicion).
    public class HoldoutEncounter : RoomEncounter
    {
        private const float CountdownStep = 15f;

        private WaveSchedule _schedule;
        private float _nextCountdown;
        private Vector3 _uplink;
        private float _nextPing;

        protected override void OnBegin()
        {
            _uplink = Context.Center;
            if (Context.Settings.TerminalPrefab == null) return;

            GameObject terminal = Instantiate(Context.Settings.TerminalPrefab, Context.TakeSpot(), Quaternion.identity, Context.Level);
            if (!terminal.TryGetComponent(out LockTerminal console)) return;
            _uplink = terminal.transform.position;
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
            Ping();
            Notify($"Uplink started — hold out for {Mathf.CeilToInt(_schedule.Duration)} s", NotificationStyle.Danger);
        }

        // The signal: enemies in earshot of the terminal go to see what it is.
        private void Ping()
        {
            _nextPing = Time.time + Context.Settings.HoldoutPingInterval;
            if (Context.Player != null) Noise.Emit(_uplink, Context.Settings.HoldoutPingRadius, DamageSource.Of(Context.Player.gameObject));
        }

        protected override void Tick()
        {
            if (_schedule == null) return;

            if (Time.time >= _nextPing) Ping();
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
