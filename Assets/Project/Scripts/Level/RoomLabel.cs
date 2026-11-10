using System.Text;
using UnityEngine;
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
            return $"<color=#{ColorUtility.ToHtmlStringRGB(room.Faction.Color)}>{room.Faction.DisplayName}</color> · {tier}";
        }

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
