using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGD.DevTools
{
    // Options for one argument of a command. previousArgs are the arguments already typed
    // (so a later argument can depend on an earlier one); argIndex is the one being typed.
    public delegate IEnumerable<string> ArgumentCompleter(string[] previousArgs, int argIndex);

    // Command registry and parser for the in-game console. Plain C#: commands are
    // registered with a handler that takes the arguments and returns what to print.
    // Quoted arguments may contain spaces: give "Scrap Metal" 20.
    public class DevConsole
    {
        private const int MaxHelpOptions = 12;

        private static readonly char[] Whitespace = { ' ', '\t' };

        private readonly Dictionary<string, Command> _commands = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _history = new();

        public const char CommandPrefix = '/';

        public DevConsole()
        {
            Register("help", "[command]", "List commands, or show one command's usage and examples", Help,
                example: "/help weapon", complete: (_, index) => index == 0 ? _commands.Keys : null);
        }

        public IReadOnlyList<string> History => _history;

        public void Register(string name, string usage, string description, Func<string[], string> run,
                             string example = null, ArgumentCompleter complete = null) =>
            _commands[name] = new Command(name, usage, description, run, example, complete);

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
                return UnknownCommand(tokens[0]);

            var args = new string[tokens.Length - 1];
            Array.Copy(tokens, 1, args, 0, args.Length);

            try
            {
                return command.Run(args) ?? string.Empty;
            }
            catch (UsageException)
            {
                return UsageText(command);
            }
            catch (Exception e)
            {
                return $"Error: {e.Message}";
            }
        }

        // What Tab can fill in for the word being typed at the end of `line`: a command name
        // for the first word, otherwise that command's options for the current argument.
        public Completion Complete(string line)
        {
            line ??= string.Empty;
            if (line.Length == 0 || line[0] != CommandPrefix) return Completion.None(line);

            int tokenStart = line.LastIndexOfAny(Whitespace) + 1;
            string typed   = line.Substring(tokenStart);

            if (tokenStart == 0)
            {
                IEnumerable<string> names = _commands.Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).Select(n => CommandPrefix + n);
                return new Completion(line, tokenStart, Match(names, typed), null);
            }

            string[] words = Tokenize(line.Substring(0, tokenStart));
            if (words.Length == 0 || !_commands.TryGetValue(words[0].TrimStart(CommandPrefix), out Command command))
                return Completion.None(line);

            string[] previousArgs = words.Skip(1).ToArray();
            IEnumerable<string> options = command.Complete?.Invoke(previousArgs, previousArgs.Length);
            return new Completion(line, tokenStart, Match(options, typed), Signature(command));
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

        // Prefix matches first; only when nothing starts with the text does it fall back to
        // anything containing it, so "bow" finds Bow before Crossbow.
        private static List<string> Match(IEnumerable<string> options, string typed)
        {
            if (options == null) return new List<string>();

            List<string> all = options.Where(o => !string.IsNullOrEmpty(o)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (typed.Length == 0) return all;

            List<string> prefix = all.Where(o => o.StartsWith(typed, StringComparison.OrdinalIgnoreCase)).ToList();
            return prefix.Count > 0 ? prefix : all.Where(o => o.IndexOf(typed, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        private string Help(string[] args)
        {
            return args.Length > 0 ? HelpFor(args[0]) : HelpAll();
        }

        private string HelpAll()
        {
            var text = new StringBuilder();
            foreach (Command c in _commands.Values.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                text.Append(Signature(c)).Append("  — ").Append(c.Description).Append('\n');

            text.Append($"Tab completes commands and arguments. {CommandPrefix}help <command> shows examples; {CommandPrefix}list shows what can be spawned or given.");
            return text.ToString();
        }

        private string HelpFor(string name)
        {
            if (!_commands.TryGetValue(name.TrimStart(CommandPrefix), out Command c))
                return UnknownCommand(name);

            var text = new StringBuilder();
            text.Append(Signature(c)).Append("\n  ").Append(c.Description);
            if (c.Example != null) text.Append("\n  e.g. ").Append(c.Example);

            List<string> options = c.Complete?.Invoke(Array.Empty<string>(), 0)?.Where(o => !string.IsNullOrEmpty(o)).ToList();
            if (options != null && options.Count > 0)
            {
                text.Append("\n  first argument: ").Append(string.Join(", ", options.Take(MaxHelpOptions)));
                if (options.Count > MaxHelpOptions) text.Append($", … +{options.Count - MaxHelpOptions} more");
            }
            return text.ToString();
        }

        private string UnknownCommand(string typed)
        {
            string name = typed.TrimStart(CommandPrefix);
            List<string> close = _commands.Keys
                .Where(k => k.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 || Distance(k, name) <= 2)
                .OrderBy(k => Distance(k, name))
                .Take(3)
                .Select(k => CommandPrefix + k)
                .ToList();

            return close.Count > 0
                ? $"Unknown command '{typed}'. Did you mean {string.Join(" or ", close)}?"
                : $"Unknown command '{typed}'. Type {CommandPrefix}help.";
        }

        private static string UsageText(Command c) =>
            c.Example != null ? $"Usage: {Signature(c)}\n  e.g. {c.Example}" : $"Usage: {Signature(c)}";

        private static string Signature(Command c) =>
            c.Usage.Length > 0 ? $"{CommandPrefix}{c.Name} {c.Usage}" : CommandPrefix + c.Name;

        // Plain edit distance — command names are short, so the textbook version is plenty.
        private static int Distance(string a, string b)
        {
            a = a.ToLowerInvariant();
            b = b.ToLowerInvariant();

            var row = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) row[j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                int diagonal = row[0];
                row[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int above = row[j];
                    row[j] = Math.Min(Math.Min(row[j] + 1, row[j - 1] + 1), diagonal + (a[i - 1] == b[j - 1] ? 0 : 1));
                    diagonal = above;
                }
            }
            return row[b.Length];
        }

        private sealed class Command
        {
            public Command(string name, string usage, string description, Func<string[], string> run,
                           string example, ArgumentCompleter complete)
            {
                Name        = name;
                Usage       = usage ?? string.Empty;
                Description = description;
                Run         = run;
                Example     = example;
                Complete    = complete;
            }

            public string Name        { get; }
            public string Usage       { get; }
            public string Description { get; }
            public string Example     { get; }
            public Func<string[], string> Run { get; }
            public ArgumentCompleter Complete { get; }
        }
    }

    // The result of asking what the word at the end of a command line could become.
    public sealed class Completion
    {
        public Completion(string line, int tokenStart, IReadOnlyList<string> candidates, string signature)
        {
            Line       = line;
            TokenStart = tokenStart;
            Candidates = candidates;
            Signature  = signature;
        }

        public static Completion None(string line) => new(line, line?.Length ?? 0, Array.Empty<string>(), null);

        public string Line { get; }

        // Where the word being completed begins in Line.
        public int TokenStart { get; }

        public IReadOnlyList<string> Candidates { get; }

        // The usage of the command being typed, or null while the command isn't known yet.
        public string Signature { get; }

        public string Typed => Line.Substring(TokenStart);

        // Longest start shared by every candidate (the first candidate's casing).
        public string CommonPrefix
        {
            get
            {
                if (Candidates.Count == 0) return string.Empty;

                string prefix = Candidates[0];
                foreach (string candidate in Candidates)
                    while (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        prefix = prefix.Substring(0, prefix.Length - 1);
                return prefix;
            }
        }

        // The line with the word being completed replaced by a candidate.
        public string Apply(int index) => Line.Substring(0, TokenStart) + Candidates[index];
    }

    // Thrown by a command handler when its arguments are wrong; the console prints usage.
    public class UsageException : Exception { }
}
