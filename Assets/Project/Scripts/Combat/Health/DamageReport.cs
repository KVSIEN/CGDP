using UnityEngine;

namespace CGD.Combat
{
    // One hit after mitigation: how much it took off (shield and health together),
    // whether it was critical and whether it killed.
    public readonly struct DamageReport
    {
        public DamageReport(HealthManager target, DamageSource source, float amount, Vector3 point, bool isCritical, bool killed)
        {
            Target     = target;
            Source     = source;
            Amount     = amount;
            Point      = point;
            IsCritical = isCritical;
            Killed     = killed;
        }

        public HealthManager Target     { get; }
        public DamageSource  Source     { get; }
        public float         Amount     { get; }
        public Vector3       Point      { get; }
        public bool          IsCritical { get; }
        public bool          Killed     { get; }
    }
}
