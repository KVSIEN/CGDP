using UnityEngine;

namespace CGD.Enemies
{
    // Running to an alarm panel and using it, shared by RaiseAlarmState and the Raise Alarm
    // node. It reports where it last saw the target. A target that closes in, or a panel
    // disabled before it gets there, ends the attempt; either way it only tries once per fight.
    internal sealed class RaiseAlarmStep
    {
        public enum Result { Running, Raised, Abandoned }

        private const float UseDistance    = 1.2f;
        private const float InterruptRange = 4f;

        private AlarmPanel _panel;
        private float      _useLeft;

        public bool Begin(EnemyAI ai)
        {
            ai.MarkAlarmTried();
            if (!ai.TryFindAlarmIgnoringTried(out _panel)) return false;

            _useLeft = _panel.UseTime;
            ai.Agent.isStopped = false;
            ai.Agent.SetDestination(_panel.transform.position);
            return true;
        }

        public Result Tick(EnemyAI ai, float deltaTime)
        {
            if (_panel == null || !_panel.IsAvailable) return Result.Abandoned;

            var perception = ai.Perception;
            if (perception.IsTargetDetected &&
                Vector3.Distance(perception.TargetPosition, ai.transform.position) < InterruptRange)
                return Result.Abandoned;

            ai.SetSpeed(ai.Data.ChaseSpeed);
            Vector3 panel = _panel.transform.position;
            if ((panel - ai.transform.position).sqrMagnitude > UseDistance * UseDistance && !Arrived(ai)) return Result.Running;

            ai.Agent.isStopped = true;
            ai.FaceTowards(panel, deltaTime);
            _useLeft -= deltaTime;
            if (_useLeft > 0f) return Result.Running;

            _panel.Raise(ai, perception.LastKnownPosition);
            return Result.Raised;
        }

        private static bool Arrived(EnemyAI ai)
        {
            var agent = ai.Agent;
            return !agent.pathPending && agent.hasPath && agent.remainingDistance <= UseDistance;
        }
    }
}
