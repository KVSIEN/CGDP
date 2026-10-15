using System;
using CGD.Core;

namespace CGD.Director
{
    // Steps through the pacing phases from the player's intensity:
    //   BuildUp     → SustainPeak  once intensity reaches the peak threshold
    //   SustainPeak → PeakFade     after the sustain duration
    //   PeakFade    → Relax        once intensity has dropped to the relax threshold
    //   Relax       → BuildUp      after a rolled relax duration
    // The world is only populated (reinforcements, ambient threats) during BuildUp and
    // SustainPeak; the fade and the calm are what make the next peak feel like one.
    public class PacingCycle
    {
        private readonly PacingTuning _tuning;
        private readonly RandomStream _random;
        private readonly StateMachine<PacingPhase> _machine = new();

        private float _intensity;
        private float _relaxDuration;

        public PacingCycle(PacingTuning tuning, RandomStream random)
        {
            _tuning = tuning;
            _random = random;

            _machine.Add(PacingPhase.BuildUp)
                    .Add(PacingPhase.SustainPeak)
                    .Add(PacingPhase.PeakFade)
                    .Add(PacingPhase.Relax, new DelegateState(enter: RollRelax))
                    .AddTransition(PacingPhase.BuildUp,     PacingPhase.SustainPeak, () => _intensity >= _tuning.PeakThreshold)
                    .AddTransition(PacingPhase.SustainPeak, PacingPhase.PeakFade,    () => _machine.TimeInState >= _tuning.SustainDuration)
                    .AddTransition(PacingPhase.PeakFade,    PacingPhase.Relax,       () => _intensity <= _tuning.RelaxThreshold)
                    .AddTransition(PacingPhase.Relax,       PacingPhase.BuildUp,     () => _machine.TimeInState >= _relaxDuration);
            _machine.Changed += (previous, next) => PhaseChanged?.Invoke(previous, next);
        }

        public PacingPhase Phase       => _machine.Current;
        public float       TimeInPhase => _machine.TimeInState;
        public bool        IsRunning   => _machine.IsRunning;

        public bool AllowsPopulation => _machine.IsRunning && Phase is PacingPhase.BuildUp or PacingPhase.SustainPeak;

        // (previous, next)
        public event Action<PacingPhase, PacingPhase> PhaseChanged;

        // Opens the session with its opening calm.
        public void Start()
        {
            _machine.Start(PacingPhase.Relax);
            _relaxDuration = _tuning.OpeningRelax;
        }

        // Back to a fresh calm, e.g. after the player respawns.
        public void Restart() => _machine.ForceChangeTo(PacingPhase.Relax);

        public void Tick(float deltaTime, float intensity)
        {
            _intensity = intensity;
            _machine.Tick(deltaTime);
        }

        private void RollRelax() => _relaxDuration = _tuning.RelaxDuration.Evaluate(_random);
    }
}
