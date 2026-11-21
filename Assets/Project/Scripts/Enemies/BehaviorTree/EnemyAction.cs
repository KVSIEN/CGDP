using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Base for behavior tree actions that drive an EnemyAI. The tree must run on an enemy.
    [Serializable]
    public abstract class EnemyAction : BtAction
    {
        [NonSerialized] protected EnemyAI Ai;

        protected override void OnInitialize(BtContext context)
        {
            Ai = context.Agent as EnemyAI
                 ?? throw new InvalidOperationException($"{GetType().Name} needs an EnemyAI to drive, not {context.Agent?.GetType().Name ?? "nothing"}.");
        }

        // Sends it on its way: routed there and moving. False (and left as it was) when no route
        // could be requested.
        protected bool MoveTo(Vector3 destination, RouteStyle style = RouteStyle.Direct)
        {
            if (!Ai.Routing.SetDestination(destination, style)) return false;
            Ai.Agent.isStopped = false;
            return true;
        }

        protected static bool HasArrived(NavMeshAgent agent, float distance) =>
            !agent.pathPending && agent.hasPath && agent.remainingDistance < distance;
    }
}
