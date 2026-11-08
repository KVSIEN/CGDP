using System;
using UnityEngine;
using CGD.Enemies;

namespace CGD.DevTools
{
    // The spectator's match: the stats since the last reset (MatchStats, fed by
    // MatchStatsRecorder) and the scenario presets (ScenarioRunner, picked from ScenarioMenu)
    // that set the arena up — loading one can start the stats over, pause the game and pick
    // whom to watch. Restart loads the last one again exactly as it began.
    internal sealed class SpectatorMatch : IDisposable
    {
        private readonly EnemyAI[]          _agents;
        private readonly MatchStats         _stats;
        private readonly MatchStatsRecorder _recorder;
        private readonly MatchStatsText     _statsText = new();
        private readonly ScenarioRunner     _runner;
        private readonly ScenarioMenu       _menu;
        private readonly SpectatorTime      _time;
        private readonly Action<string>     _toast;
        private readonly Action<int>        _watch;

        private AIScenario _current;

        public SpectatorMatch(EnemyAI[] agents, AIScenario[] scenarios, RectTransform canvas, SpectatorInputSettings settings,
                              SpectatorTime time, Action<string> toast, Action<int> watch)
        {
            _agents   = agents;
            _time     = time;
            _toast    = toast;
            _watch    = watch;
            _stats    = new MatchStats(agents.Length);
            _stats.Reset(Time.time);
            _recorder = new MatchStatsRecorder(agents, _stats);
            _runner   = new ScenarioRunner(agents);
            _menu     = new ScenarioMenu(canvas, scenarios ?? Array.Empty<AIScenario>(),
                                         settings.PromptFor(SpectatorAction.Scenarios),
                                         settings.PromptFor(SpectatorAction.RestartScenario), Load);
        }

        public bool IsMenuOpen => _menu.IsOpen;

        public void ToggleMenu() => _menu.Toggle();
        public void CloseMenu()  => _menu.Close();

        public void ResetStats()
        {
            _stats.Reset(Time.time);
            _toast("Match stats reset");
        }

        public void Restart()
        {
            if (_current == null)
            {
                _toast("No scenario loaded yet");
                return;
            }
            Load(_current);
        }

        public string StatsText(string resetKey) =>
            _statsText.Write(_stats, _agents, Time.time, _current != null ? _current.DisplayName : null, resetKey);

        public void Dispose() => _recorder.Dispose();

        private void Load(AIScenario scenario)
        {
            if (scenario == null) return;
            _menu.Close();
            _current = scenario;

            int watch = _runner.Load(scenario);
            if (scenario.ResetStats) _stats.Reset(Time.time);
            if (scenario.StartPaused != _time.IsPaused) _time.TogglePause();
            if (watch >= 0) _watch(watch);
            _toast($"Scenario: {scenario.DisplayName}{(scenario.StartPaused ? "  ·  paused" : "")}");
        }
    }
}
