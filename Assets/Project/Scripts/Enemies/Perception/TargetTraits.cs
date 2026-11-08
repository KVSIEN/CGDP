namespace CGD.Enemies
{
    // What an enemy makes of one hostile it can sense, for choosing whom to fight
    // (TargetWeights). Only what it can tell from where it stands.
    public readonly struct TargetTraits
    {
        // The hostile it is already focused on.
        public readonly bool  IsCurrent;
        // Hurt it moments ago (TargetPriorities.RetaliationMemory).
        public readonly bool  HurtItRecently;
        // Busy fighting someone else, so not watching this enemy.
        public readonly bool  IsDistracted;
        // Health and shield left, 0–1 of the hostile's full amount.
        public readonly float HealthShare;

        public TargetTraits(bool isCurrent, bool hurtItRecently, bool isDistracted, float healthShare)
        {
            IsCurrent      = isCurrent;
            HurtItRecently = hurtItRecently;
            IsDistracted   = isDistracted;
            HealthShare    = healthShare;
        }
    }
}
