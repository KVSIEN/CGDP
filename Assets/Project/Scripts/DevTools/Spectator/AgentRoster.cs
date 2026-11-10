using System;
using System.Collections.Generic;
using CGD.Enemies;

namespace CGD.DevTools
{
    // The agents the spectator watches, numbered by position (the number is the watch key and the
    // row in the stats). It only grows: agents brought in by the squad sizes are added at the
    // end, and one sent away stays on the list, switched off, to be brought back later. Whoever
    // keeps per-agent state listens to Added.
    internal sealed class AgentRoster
    {
        private readonly List<EnemyAI> _agents;

        public AgentRoster(IEnumerable<EnemyAI> agents) => _agents = new List<EnemyAI>(agents);

        public event Action<EnemyAI> Added;

        public int     Count => _agents.Count;
        public EnemyAI this[int index] => _agents[index];

        public void Add(EnemyAI agent)
        {
            _agents.Add(agent);
            Added?.Invoke(agent);
        }
    }
}
