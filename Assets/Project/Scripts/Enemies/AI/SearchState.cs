namespace CGD.Enemies
{
    // Looks into a lead with EnemySearch: stops to stare, goes to where it believes the thing
    // is and searches around it. Registered twice — Investigate (merely curious) and Hunt
    // (knows a hostile is about) — sharing one search, which moves up to a Hunt if the
    // enemy learns more on the way. Spotting a hostile starts the chase; a search that turns
    // up nothing sends it back to its post, on edge.
    public class SearchState : EnemyState
    {
        private readonly EnemyAI.AiState _id;

        public SearchState(EnemyAI ai, EnemyAI.AiState id) : base(ai) => _id = id;

        public override EnemyAI.AiState Id => _id;

        // Investigate → Hunt keeps the search already under way.
        public override void Enter()
        {
            if (!Ai.Search.IsActive) Ai.Search.Begin();
        }

        public override void Tick(float deltaTime)
        {
            var perception = Ai.Perception;
            if (perception.IsTargetDetected)
            {
                Ai.ChangeState(EnemyAI.AiState.Chase);
                return;
            }

            if (_id == EnemyAI.AiState.Investigate && perception.Level == AwarenessLevel.Searching)
            {
                Ai.ChangeState(EnemyAI.AiState.Hunt);
                return;
            }

            if (Ai.Search.Tick(deltaTime)) Ai.GiveUpSearch();
        }
    }
}
