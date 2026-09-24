using UnityEngine;

namespace CGD.Targeting
{
    // Just the caster (self-buffs, self-heals). Ignores the affiliation filter.
    [CreateAssetMenu(fileName = "SelfTargetSelector", menuName = "CGD/Targeting/Self")]
    public class SelfTargetSelector : TargetSelector
    {
        protected override void Collect(in TargetingRequest request, in TargetFilter filter, TargetSet result)
        {
            if (request.Caster == null || request.Caster.IsDead) return;

            result.Targets.Add(request.Caster);
            result.SetPoint(request.CasterPosition);
        }
    }
}
