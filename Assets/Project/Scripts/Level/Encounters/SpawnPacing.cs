using UnityEngine;

namespace CGD.Level
{
    // When a spawner sends enemies: a batch every Interval, but never past the cap of
    // enemies alive at once. The timer only runs while there is room, so killing enemies
    // makes it start counting again instead of refilling instantly.
    public class SpawnPacing
    {
        private readonly float _interval;
        private readonly int   _batch;
        private readonly int   _cap;
        private float _timer;

        public SpawnPacing(float interval, int batch, int cap)
        {
            _interval = Mathf.Max(0.1f, interval);
            _batch    = Mathf.Max(1, batch);
            _cap      = Mathf.Max(1, cap);
        }

        // How many enemies to send this frame. The first batch is due straight away.
        public int Advance(float deltaTime, int alive)
        {
            int room = _cap - alive;
            if (room <= 0) return 0;

            _timer -= deltaTime;
            if (_timer > 0f) return 0;

            _timer = _interval;
            return Mathf.Min(_batch, room);
        }
    }
}
