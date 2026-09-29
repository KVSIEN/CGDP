using System;
using System.Collections.Generic;
using System.Text;

namespace CGD.DevTools
{
    // Command registry and parser for the in-game console. Plain C#: commands are
    // registered with a handler that takes the arguments and returns what to print.
    // Quoted arguments may contain spaces: give "Scrap Metal" 20.
    public class DevConsole
    {
        private readonly Dictionary<string, Command> _commands = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _history = new();

        public const char CommandPrefix = '/';

        public DevConsole()
        {
            Register("help", "[command]", "List commands, or show one command's usage", Help);
        }

        public IReadOnlyList<string> History => _history;

        public void Register(string name, string usage, string description, Func<string[], string> run) =>
            _commands[name] = new Command(name, usage, description, run);

        public string Execute(string line)
        {
            string trimmed = (line ?? string.Empty).Trim();
            if (trimmed.Length == 0) return string.Empty;

            _history.Add(trimmed);
            if (trimmed[0] != CommandPrefix)
                return $"Commands start with {CommandPrefix}  —  try {CommandPrefix}help.";

            string[] tokens = Tokenize(trimmed.Substring(1));
            if (tokens.Length == 0) return $"Type {CommandPrefix}help for commands.";

            if (!_commands.TryGetValue(tokens[0], out Command command))
                return $"Unknown command '{tokens[0]}'. Type {CommandPrefix}help.";

            var args = new string[tokens.Length - 1];
            Array.Copy(tokens, 1, args, 0, args.Length);

            try
            {
                return command.Run(args) ?? string.Empty;
            }
            catch (UsageException)
            {
                return $"Usage: {CommandPrefix}{command.Name} {command.Usage}";
            }
            catch (Exception e)
            {
                return $"Error: {e.Message}";
            }
        }

        // Command names starting with the typed prefix, alphabetical.
        public List<string> Complete(string prefix)
        {
            prefix = (prefix ?? string.Empty).TrimStart(CommandPrefix);
            var matches = new List<string>();
            foreach (string name in _commands.Keys)
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    matches.Add(CommandPrefix + name);
            matches.Sort(StringComparer.OrdinalIgnoreCase);
            return matches;
        }

        public static string[] Tokenize(string line)
        {
            var tokens  = new List<string>();
            var current = new StringBuilder();
            bool quoted = false;

            foreach (char c in line ?? string.Empty)
            {
                if (c == '"') { quoted = !quoted; continue; }
                if (char.IsWhiteSpace(c) && !quoted)
                {
                    if (current.Length > 0) { tokens.Add(current.ToString()); current.Clear(); }
                    continue;
                }
                current.Append(c);
            }

            if (current.Length > 0) tokens.Add(current.ToString());
            return tokens.ToArray();
        }

        private string Help(string[] args)
        {
            if (args.Length > 0)
                return _commands.TryGetValue(args[0].TrimStart(CommandPrefix), out Command c)
                    ? $"{CommandPrefix}{c.Name} {c.Usage}\n  {c.Description}"
                    : $"Unknown command '{args[0]}'.";

            var names = new List<string>(_commands.Keys);
            names.Sort(StringComparer.OrdinalIgnoreCase);

            var text = new StringBuilder();
            foreach (string name in names)
            {
                Command c = _commands[name];
                text.Append(CommandPrefix).Append(c.Name).Append(' ').Append(c.Usage).Append("  — ").Append(c.Description).Append('\n');
            }
            return text.ToString().TrimEnd();
        }

        private sealed class Command
        {
            public Command(string name, string usage, string description, Func<string[], string> run)
            {
                Name        = name;
                Usage       = usage;
                Description = description;
                Run         = run;
            }

            public string Name        { get; }
            public string Usage       { get; }
            public string Description { get; }
            public Func<string[], string> Run { get; }
        }
    }

    // Thrown by a command handler when its arguments are wrong; the console prints usage.
    public class UsageException : Exception { }
}
