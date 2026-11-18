using System.Text;
using UnityEngine;
using CGD.Factions;
using CGD.Map;

namespace CGD.Level
{
    // What the level tells the player a room is — on door signs and when they walk in: the
    // place it is (its function, else its room type), who holds it and its tier. It names
    // the room as its door sign shows it, so an Ambush passes for the Treasure room it poses as.
    public static class RoomLabel
    {
        public static string Name(LevelRoom room)
        {
            if (room.Function != null && !string.IsNullOrEmpty(room.Function.DisplayName)) return room.Function.DisplayName;
            return Spaced(room.Node.Type.SignType().ToString());
        }

        // "TECH · Tier 2", the faction in its colour (TextMeshPro rich text). "Tier 2" alone
        // for a room nobody holds.
        public static string Details(LevelRoom room)
        {
            string tier = $"Tier {Mathf.Clamp(room.Node.EffectiveTier, 1, 3)}";
            if (room.Faction == null) return tier;
            return $"{Coloured(room.Faction)} · {tier}";
        }

        // The Room Gallery's fuller placard, for judging the room against its recipe: the
        // place's layout, the details above, then the ship side, how deep in its faction's
        // territory it stands, what kind of fight it is and who breaches it —
        // "Table Pits · BIO · Tier 1 · crew · frontier · Combat".
        public static string Exhibit(LevelRoom room, TerritoryDepth depth)
        {
            var text = new StringBuilder();
            if (room.RoomLayout != null) text.Append(room.RoomLayout.DisplayName).Append(" · ");
            text.Append(Details(room));
            string side = SideName(room.Side);
            if (side != null) text.Append(" · ").Append(side);
            if (room.Faction != null && depth != null) text.Append(" · ").Append(depth.NameOf(room.DesignStrength));
            if (room.Node.Type != MapNodeType.Quiet) text.Append(" · ").Append(Spaced(room.Node.Type.ToString()));
            if (room.BreachFaction != null) text.Append(", breached by ").Append(Coloured(room.BreachFaction));
            return text.ToString();
        }

        private static string Coloured(FactionDefinition faction) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(faction.Color)}>{faction.DisplayName}</color>";

        private static string SideName(ShipSide side) => side switch
        {
            ShipSide.GuestFirstClass => "first class",
            ShipSide.GuestSteerage   => "steerage",
            ShipSide.Crew            => "crew",
            _                        => null,
        };

        // EmergencyExit → Emergency Exit.
        private static string Spaced(string name)
        {
            var text = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) text.Append(' ');
                text.Append(name[i]);
            }
            return text.ToString();
        }
    }
}
