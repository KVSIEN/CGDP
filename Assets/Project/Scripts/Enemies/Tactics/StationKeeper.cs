using UnityEngine;

namespace CGD.Enemies
{
    // Makes "stand N m off that ally" read as a loose station rather than a rigid slot. The ideal
    // spot is recomputed from moving squadmates all the time, so following it exactly makes an agent
    // twitch: it keeps its current goal until the ideal moves a real distance away, and it stands
    // still once close enough, setting off again only after it has drifted well clear (two
    // thresholds, so it doesn't flip between walking and stopping at the edge of one).
    // Plain logic, tested without the editor.
    public sealed class StationKeeper
    {
        private readonly float _retarget;
        private readonly float _arrive;
        private readonly float _depart;

        private Vector3 _goal;
        private bool    _hasGoal;
        private bool    _moving;

        public StationKeeper(float retarget = 2.5f, float arrive = 1f, float depart = 2.5f)
        {
            _retarget = retarget;
            _arrive   = arrive;
            _depart   = Mathf.Max(depart, arrive);
        }

        public Vector3 Goal   => _goal;
        public bool    Moving => _moving;

        public void Reset()
        {
            _hasGoal = false;
            _moving  = false;
        }

        // Feed it the ideal spot and where the agent stands; read Goal and Moving afterwards.
        public void Update(Vector3 ideal, Vector3 self)
        {
            if (!_hasGoal || Flat(ideal - _goal).sqrMagnitude > _retarget * _retarget)
            {
                _goal    = ideal;
                _hasGoal = true;
            }

            float away = Flat(_goal - self).magnitude;
            if (_moving) _moving = away > _arrive;
            else         _moving = away > _depart;
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
