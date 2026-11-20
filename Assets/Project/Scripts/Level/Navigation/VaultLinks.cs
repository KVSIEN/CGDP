using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CGD.Level
{
    // Holds the NavMesh links a level added for hopping low cover (VaultLinkBuilder), and takes
    // them off the NavMesh with the level.
    public class VaultLinks : MonoBehaviour
    {
        private readonly List<NavMeshLinkInstance> _links = new();

        public int Count => _links.Count;

        internal void Add(NavMeshLinkInstance link) => _links.Add(link);

        private void OnDestroy()
        {
            foreach (NavMeshLinkInstance link in _links) NavMesh.RemoveLink(link);
            _links.Clear();
        }
    }
}
