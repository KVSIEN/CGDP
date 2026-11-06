namespace CGD.Enemies
{
    // How often an enemy's behavior tree makes decisions. Anything with its attention decides
    // every frame. An enemy with nothing going on is only walking its patrol or standing
    // guard, which a few decisions a second handle just as well — the big saving in levels
    // full of idle enemies. Senses keep their own timer, and as soon as they pick something
    // up the enemy is back to every frame.
    public static class TreeTickRate
    {
        // Seconds between decisions while calm with a hostile within sensing range.
        public const float Nearby = 0.1f;
        // Seconds between decisions while calm with nobody around.
        public const float Remote = 0.25f;

        public static float IntervalFor(bool engaged, bool hostilesNearby) =>
            engaged ? 0f : hostilesNearby ? Nearby : Remote;
    }
}
