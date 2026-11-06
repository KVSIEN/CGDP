using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // The cover spots one enemy knows about. It starts with its home turf — around where it
    // spawned and along its patrol route — and learns more by seeing them: every so often
    // it notices a few unknown spots in front of it with a clear line from its eyes. An
    // enemy that wandered into an unfamiliar room has to look around before it can use it.
    internal sealed class CoverKnowledge
    {
        private const float EyeHeight      = 1.5f;
        private const float HomeRadius     = 10f;
        private const float LearnInterval  = 0.5f;
        private const float LearnRange     = 14f;
        private const int   LearnPerCheck  = 4;    // raycasts per check at most

        private readonly EnemyAI       _ai;
        private readonly LayerMask     _obstacleMask;
        private readonly HashSet<int>  _known = new();
        private readonly List<int>     _scratch = new();
        private CoverMap _map;
        private float    _nextLearn;
        private int      _cursor;

        public CoverKnowledge(EnemyAI ai, LayerMask obstacleMask)
        {
            _ai           = ai;
            _obstacleMask = obstacleMask;
            _nextLearn    = Random.value * LearnInterval;
        }

        public CoverMap Map => Current();

        public bool Knows(int index) => _known.Contains(index);

        // Spots it knows within radius of a position.
        public void Known(Vector3 center, float radius, List<int> results)
        {
            results.Clear();
            CoverMap map = Current();
            if (map == null) return;

            map.Query(center, radius, _scratch);
            foreach (int index in _scratch)
                if (_known.Contains(index)) results.Add(index);
        }

        public void Tick(float time)
        {
            if (time < _nextLearn) return;
            _nextLearn = time + LearnInterval;

            CoverMap map = Current();
            if (map != null) LearnInView(map);
        }

        // Forgets everything; the next use relearns its (possibly new) home turf.
        public void Forget()
        {
            _known.Clear();
            _map = null;
        }

        // The map can appear (or be rebuilt) after the enemy wakes: start over with home turf.
        private CoverMap Current()
        {
            CoverMap active = CoverMap.Active;
            if (active == _map) return _map;

            _map = active;
            _known.Clear();
            if (_map != null) LearnHomeTurf(_map);
            return _map;
        }

        private void LearnHomeTurf(CoverMap map)
        {
            LearnAround(map, _ai.transform.position);
            foreach (Transform waypoint in _ai.Waypoints)
                if (waypoint != null) LearnAround(map, waypoint.position);
        }

        private void LearnAround(CoverMap map, Vector3 center)
        {
            map.Query(center, HomeRadius, _scratch);
            foreach (int index in _scratch) _known.Add(index);
        }

        // A few unknown spots in front of it, checked in turn so no single frame does many raycasts.
        private void LearnInView(CoverMap map)
        {
            Transform self = _ai.transform;
            map.Query(self.position, LearnRange, _scratch);
            if (_scratch.Count == 0) return;

            Vector3 eye       = self.position + Vector3.up * EyeHeight;
            float   halfAngle = _ai.Data.SightAngle * 0.5f;
            int     checks    = 0;

            for (int n = 0; n < _scratch.Count && checks < LearnPerCheck; n++)
            {
                int index = _scratch[(_cursor + n) % _scratch.Count];
                if (_known.Contains(index)) continue;

                Vector3 spot = map[index].Position + Vector3.up * 0.5f;
                if (Vector3.Angle(self.forward, spot - eye) > halfAngle) continue;

                checks++;
                if (!Physics.Linecast(eye, spot, _obstacleMask, QueryTriggerInteraction.Ignore)) _known.Add(index);
            }
            _cursor += LearnPerCheck;
        }
    }
}
