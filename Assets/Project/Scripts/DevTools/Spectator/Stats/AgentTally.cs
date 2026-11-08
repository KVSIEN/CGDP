namespace CGD.DevTools
{
    // One agent's (or everyone else's) running totals for the match (MatchStats).
    public sealed class AgentTally
    {
        public int   Kills;
        public int   Deaths;
        public int   Hits;
        public int   AbilityUses;
        public float DamageDealt;
        public float DamageTaken;

        // Kills it finished, summed: seconds from the victim's first hit (that life) to its death.
        private float _killSeconds;
        private int   _timedKills;

        // When this life first took a hit; negative while untouched.
        internal float UnderFireSince = -1f;

        // Average seconds its kills took, from first hit on the victim to the kill; 0 with none.
        public float AverageTimeToKill => _timedKills > 0 ? _killSeconds / _timedKills : 0f;

        internal void AddKillTime(float seconds)
        {
            _killSeconds += seconds;
            _timedKills++;
        }

        internal void Clear()
        {
            Kills = Deaths = Hits = AbilityUses = 0;
            DamageDealt = DamageTaken = 0f;
            _killSeconds = 0f;
            _timedKills = 0;
            UnderFireSince = -1f;
        }
    }
}
