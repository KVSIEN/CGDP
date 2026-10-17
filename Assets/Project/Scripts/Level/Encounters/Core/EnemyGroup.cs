using System;
using System.Collections.Generic;
using CGD.Combat;

namespace CGD.Level
{
    // The enemies an encounter is waiting on. Each is forgotten when it dies, so a pooled
    // enemy reused elsewhere later doesn't count again; Cleared fires when the last one goes.
    public class EnemyGroup
    {
        private readonly List<HealthManager> _alive = new();
        private readonly Dictionary<HealthManager, Action> _handlers = new();

        public int  Alive     => _alive.Count;
        public bool IsCleared => _alive.Count == 0;

        public event Action Cleared;

        public void Add(HealthManager enemy)
        {
            if (enemy == null || enemy.IsDead || _handlers.ContainsKey(enemy)) return;

            void OnDeath() => Remove(enemy);
            _handlers[enemy] = OnDeath;
            enemy.OnDeath += OnDeath;
            _alive.Add(enemy);
        }

        public void AddRange(IEnumerable<HealthManager> enemies)
        {
            foreach (HealthManager enemy in enemies) Add(enemy);
        }

        private void Remove(HealthManager enemy)
        {
            if (_handlers.Remove(enemy, out Action handler)) enemy.OnDeath -= handler;
            if (!_alive.Remove(enemy)) return;
            if (_alive.Count == 0) Cleared?.Invoke();
        }
    }
}
