using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace CGD.Level
{
    // Builds a NavMeshSurface when the scene loads instead of keeping baked data in the
    // project, so test scenes stay walkable however their geometry is changed. Agents placed
    // in the scene loaded before the NavMesh existed; they're snapped onto it afterwards.
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(NavMeshSurface))]
    public class RuntimeNavMesh : MonoBehaviour
    {
        [Tooltip("Parent of the scene's NavMesh agents (e.g. the Enemies object)")]
        [SerializeField] private Transform _agents;

        private void Awake()
        {
            GetComponent<NavMeshSurface>().BuildNavMesh();
            if (_agents == null) return;

            foreach (NavMeshAgent agent in _agents.GetComponentsInChildren<NavMeshAgent>())
                agent.Warp(agent.transform.position);
        }
    }
}
