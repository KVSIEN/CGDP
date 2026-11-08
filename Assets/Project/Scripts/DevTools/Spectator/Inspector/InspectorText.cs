using System.Text;

namespace CGD.DevTools
{
    // The inspector panel's typography: muted labels with values lined up in a column, gold
    // section headings, and short numbers. Every page writes through these so they match.
    internal static class InspectorText
    {
        public const string Muted = "#9AA0B0";
        public const string Gold  = "#FFD24A";
        public const string Good  = "#7CE38B";
        public const string Bad   = "#FF6B5E";
        public const string Warn  = "#FFB347";
        public const string Shield = "#7FC8FF";

        public static StringBuilder Heading(this StringBuilder text, string title) =>
            text.Append("<color=").Append(Gold).Append('>').Append(title).Append("</color>\n");

        public static StringBuilder Row(this StringBuilder text, string label, string value) =>
            text.Append("<color=").Append(Muted).Append('>').Append(label).Append("</color><pos=34%>").Append(value).Append('\n');

        public static StringBuilder Coloured(this StringBuilder text, string colour, string value) =>
            text.Append("<color=").Append(colour).Append('>').Append(value).Append("</color>");

        public static StringBuilder Seconds(this StringBuilder text, float seconds) =>
            text.Append(seconds.ToString(seconds < 10f ? "0.0" : "0")).Append(" s");

        public static StringBuilder Indent(this StringBuilder text, int depth) => text.Append(' ', depth * 2);
    }
}
