namespace CGD.Abilities
{
    // How a rigless enemy holds its placeholder arm while it casts an ability (AbilityLook.Pose):
    // a physical move thrusts it out in front, a spell holds it upright like a raised wand.
    public enum CastPose
    {
        Thrust,
        Wand,
    }
}
