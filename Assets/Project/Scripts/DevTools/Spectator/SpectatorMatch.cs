using System;
using UnityEngine;

namespace CGD.DevTools
{
    // The spectator's match: the stats since the last reset (MatchStats, fed by
    // MatchStatsRecorder) and the scenario presets (ScenarioRunner, picked from ScenarioMenu)
    // that set the arena up — loading one can start the stats over, pause the game and pick
    // whom to watch. Restart loads the last one again exactly as it began.
    internal sealed class SpectatorMatch : IDisposable
    {
        private readonly AgentRoster        _agents;
        private readonly MatchStats         _stats;
        private readonly MatchStatsRecorder _recorder;
        private readonly MatchStatsText     _statsText = new();
        private readonly ScenarioRunner     _runner;
        private readonly ScenarioMenu       _menu;
        private readonly SquadMenu          _squadMenu;
        private readonly SpectatorTime      _time;
        private readonly Action<string>     _toast;
        private readonly Action<int>        _watch;

        private AIScenario _current;

        public SpectatorMatch(AgentRoster agents, Transform agentsRoot, GameObject[] agentPrefabs, AIScenario[] scenarios, RectTransform canvas, SpectatorInputSettings settings,
                              SpectatorTime time, Action<string> toast, Action<int> watch)
        {
            _agents   = agents;
            _time     = time;
            _toast    = toast;
            _watch    = watch;
            _stats    = new MatchStats(agents.Count);
            _stats.Reset(Time.time);
            _recorder = new MatchStatsRecorder(agents, _stats);
            _runner   = new ScenarioRunner(agents);
            _menu     = new ScenarioMenu(canvas, scenarios ?? Array.Empty<AIScenario>(),
                                         settings.PromptFor(SpectatorAction.Scenarios),
                                         settings.PromptFor(SpectatorAction.RestartScenario), Load);
            _squadMenu = new SquadMenu(canvas, new SquadSizer(agents, _runner, agentsRoot, agentPrefabs), settings.PromptFor(SpectatorAction.Squads), toast);
        }

        public bool IsMenuOpen => _menu.IsOpen || _squadMenu.IsOpen;

        public void ToggleMenu()
        {
            _squadMenu.Close();
            _menu.Toggle();
        }

        public void ToggleSquadMenu()
        {
            _menu.Close();
            _squadMenu.Toggle();
        }

        public void CloseMenu()
        {
            _menu.Close();
            _squadMenu.Close();
        }

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
