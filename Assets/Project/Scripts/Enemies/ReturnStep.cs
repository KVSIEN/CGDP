namespace CGD.Enemies
{
    // The pause-then-walk-home routine behind the Return To Post node.
    internal static class ReturnStep
    {
        // True once it is back at its post (or the nearest point of its route).
        public static bool Tick(EnemyAI ai, float deltaTime, ref float pauseLeft, ref bool walking, float arrivalDistance)
        {
            if (pauseLeft > 0f)
            {
                pauseLeft -= deltaTime;
                ai.Agent.isStopped = true;
                ai.FaceTowards(ai.ReturnLookAt, deltaTime);
                return false;
            }

            var perception = ai.Perception;
            float speed = ai.Data.PatrolSpeed;
            if (perception.IsCautious) speed *= perception.Profile.CautionPatrolSpeed;
            ai.SetSpeed(speed);

            var agent = ai.Agent;
            if (!walking)
            {
                walking = true;
                agent.isStopped = false;
                agent.SetDestination(ai.Post.ReturnPoint());
                return false;
            }

            return !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= arrivalDistance);
        }
    }
}
