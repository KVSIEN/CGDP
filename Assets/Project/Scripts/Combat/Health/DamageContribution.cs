using System;

namespace CGD.Combat
{
    // How much of a character's effective health each team has taken off it: the shield and
    // health a hit actually removed, after armour and without overkill. Kill credit is a
    // team's share of the total, so a character that healed mid-fight still splits 100%.
    public class DamageContribution
    {
        private static readonly int TeamCount = Enum.GetValues(typeof(Team)).Length;

        private readonly float[] _byTeam = new float[TeamCount];
        private float _total;

        public void Record(Team team, float removed)
        {
            if (removed <= 0f) return;

            _byTeam[(int)team] += removed;
            _total             += removed;
        }

        // 0..1; 0 while nothing has been taken off yet.
        public float ShareOf(Team team) => _total > 0f ? _byTeam[(int)team] / _total : 0f;

        public void Clear()
        {
            Array.Clear(_byTeam, 0, _byTeam.Length);
            _total = 0f;
        }
    }
}
