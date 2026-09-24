using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;

namespace CGD.Targeting
{
    // Output of a TargetSelector: the characters picked and, for aimed or ground-targeted
    // selections, the world point the selection is centred on. Owned and reused by the
    // caller so selections don't allocate.
    public class TargetSet
    {
        public List<HealthManager> Targets { get; } = new();
        public bool    HasPoint { get; private set; }
        public Vector3 Point    { get; private set; }

        public int Count => Targets.Count;

        public void SetPoint(Vector3 point)
        {
            Point    = point;
            HasPoint = true;
        }

        public void Clear()
        {
            Targets.Clear();
            HasPoint = false;
            Point    = Vector3.zero;
        }
    }
}
