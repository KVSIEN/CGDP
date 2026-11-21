namespace CGD.Player
{
    // How the player's actions give way to each other, in one place: change a rule here and
    // every action follows it. Only actions are listed. States checked every frame (a dodge
    // under way, a sprint, a stun or mantle) interrupt a reload or a bow's draw through
    // PlayerInterrupts, and whether the player may act at all is PlayerMovement.CanAct.
    public static class PlayerActionRules
    {
        // Starting one of these takes the hands off a reload: lost before its commit point,
        // done early after it.
        public static bool CancelsReload(PlayerActionKind kind) =>
            kind is PlayerActionKind.Melee or PlayerActionKind.Ability or PlayerActionKind.Item or PlayerActionKind.Throw;

        // Finishing one of these between melee swings keeps the combo going (a weave): its
        // timer carries on instead of the next swing starting the combo over.
        public static bool WeavesIntoCombo(PlayerActionKind kind) =>
            kind is PlayerActionKind.Ability or PlayerActionKind.Item or PlayerActionKind.Throw or PlayerActionKind.Offhand;
    }
}
