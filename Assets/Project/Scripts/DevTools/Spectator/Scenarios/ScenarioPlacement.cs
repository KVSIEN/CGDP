namespace CGD.DevTools
{
    // Where a scenario puts an agent.
    public enum ScenarioPlacement
    {
        Home,      // where it was placed in the scene, facing the way it was placed
        Position,  // ScenarioAgent.Position, facing ScenarioAgent.Facing
    }
}
