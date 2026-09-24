using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using CGD.Core;
using CGD.Items;
using CGD.Player;
using CGD.Quests;
using CGD.Stats;
using CGD.Timing;
using CGD.Weapons;
using CGD.WorldMap;

namespace CGD.DevTools
{
    // The cheats behind the dev console. Lives on the Player and builds the console's
    // command set from whatever it is wired to — commands for missing systems explain
    // what to assign instead of failing. Off in release builds unless allowed.
    public class DevCommands : MonoBehaviour
    {
        [SerializeField] private DevCatalog _catalog;
        [Tooltip("Where 'spawn' aims from — usually the Main Camera")]
        [SerializeField] private Transform _aim;
        [SerializeField] private QuestTracker _quests;
        [SerializeField] private WorldMapArea _map;
        [SerializeField] private bool _allowInReleaseBuilds;

        private PlayerInventory     _inventory;
        private PlayerWeaponLoadout _loadout;
        private PlayerHealth        _health;
        private CharacterStats      _stats;
        private TimeScaleRequest    _timeScale;

        public DevConsole Console { get; } = new();

        public bool IsAvailable => Debug.isDebugBuild || _allowInReleaseBuilds;

        private void Awake()
        {
            TryGetComponent(out _inventory);
            TryGetComponent(out _loadout);
            TryGetComponent(out _health);
            TryGetComponent(out _stats);
            RegisterAll();
        }

        private void RegisterAll()
        {
            Console.Register("give",      "<item> [count]",           "Add items to the inventory (gear is rolled)", Give);
            Console.Register("weapon",    "<category> [seed] [tier]", "Add a generated weapon — same seed and tier give the same gun", Weapon);
            Console.Register("spawn",     "<enemy> [count]",          "Spawn enemies where you're looking", Spawn);
            Console.Register("god",       "[on|off]",                 "Toggle taking no damage", God);
            Console.Register("heal",      "",                         "Restore full health", _ => Heal());
            Console.Register("buff",      "<preset> [seconds]",       "Apply a stat modifier preset for a while", Buff);
            Console.Register("timescale", "<scale>",                  "Set game speed (1 = normal)", TimeScale);
            Console.Register("revealmap", "",                         "Uncover all fog of war", _ => RevealMap());
            Console.Register("quest",     "list | start <q> | step <q> <n> | complete <q>", "Inspect or jump quest progress", Quest);
            Console.Register("list",      "items|weapons|enemies|buffs", "Show what the catalog can hand out", List);
        }

        // --- Items -----------------------------------------------------------------

        private string Give(string[] args)
        {
            if (args.Length < 1) throw new UsageException();
            if (!Need(_inventory, "PlayerInventory", out string missing)) return missing;

            ItemDefinition item = Find(_catalog != null ? _catalog.Items : null, args[0], i => i.DisplayName, out string error);
            if (item == null) return error;

            int count = args.Length > 1 ? ParseInt(args[1]) : 1;
            if (item is GearDefinition gear)
                for (int i = 0; i < count; i++) _inventory.Inventory.Add(gear.CreateInstance());
            else
                _inventory.Inventory.Add(item, count);

            return $"Added {count} × {item.DisplayName}.";
        }

        private string Weapon(string[] args)
        {
            if (args.Length < 1) throw new UsageException();
            if (!Need(_loadout, "PlayerWeaponLoadout", out string missing)) return missing;

            WeaponCategoryData category = Find(_catalog != null ? _catalog.Weapons : null, args[0], c => c.DisplayName, out string error);
            if (category == null) return error;

            Seed seed = args.Length > 1 ? Seed.Parse(args[1]) : Seed.Random();
            ItemTier tier = args.Length > 2 ? ParseTier(args[2]) : category.Tier;
            WeaponInstance weapon = WeaponGenerator.Generate(category, category.Roll(tier, seed));

            WeaponInstance replaced = _loadout.AddWeapon(weapon);
            if (replaced != null && _inventory != null) _inventory.Inventory.Add(replaced);
            return $"{weapon.DisplayName} ({weapon.Tier}, Q{weapon.Quality}) — seed {seed}";
        }

        // --- World -----------------------------------------------------------------

        private string Spawn(string[] args)
        {
            if (args.Length < 1) throw new UsageException();

            GameObject prefab = Find(_catalog != null ? _catalog.Enemies : null, args[0], null, out string error);
            if (prefab == null) return error;

            int count = args.Length > 1 ? Mathf.Clamp(ParseInt(args[1]), 1, 50) : 1;
            Vector3 point = SpawnPoint();
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = count > 1 ? Quaternion.Euler(0f, 360f * i / count, 0f) * Vector3.forward * 1.5f : Vector3.zero;
                PrefabPool.Spawn(prefab, OnNavMesh(point + offset), Quaternion.LookRotation(-FlatForward()));
            }
            return $"Spawned {count} × {prefab.name}.";
        }

        private string RevealMap()
        {
            if (!Need(_map, "WorldMapArea", out string missing)) return missing;
            if (_map.Fog == null) return "This map has no fog of war.";

            _map.Fog.RevealAll();
            return "Map revealed.";
        }

        private string TimeScale(string[] args)
        {
            if (args.Length < 1) throw new UsageException();

            float scale = Mathf.Clamp(ParseFloat(args[0]), 0.05f, 10f);
            _timeScale?.Release();
            _timeScale = Mathf.Approximately(scale, 1f) ? null : GameTime.Instance.Clock.RequestScale(scale);
            return $"Time scale {scale:0.##}.";
        }

        // --- Player ----------------------------------------------------------------

        private string God(string[] args)
        {
            if (!Need(_health, "PlayerHealth", out string missing)) return missing;

            _health.IsInvulnerable = args.Length > 0 ? ParseOnOff(args[0]) : !_health.IsInvulnerable;
            return _health.IsInvulnerable ? "God mode on." : "God mode off.";
        }

        private string Heal()
        {
            if (!Need(_health, "PlayerHealth", out string missing)) return missing;

            _health.Heal(_health.MaxHealth);
            return "Healed.";
        }

        private string Buff(string[] args)
        {
            if (args.Length < 1) throw new UsageException();
            if (!Need(_stats, "CharacterStats", out string missing)) return missing;

            StatModifierPreset preset = Find(_catalog != null ? _catalog.Buffs : null, args[0], null, out string error);
            if (preset == null) return error;

            float seconds = args.Length > 1 ? ParseFloat(args[1]) : 30f;
            _stats.AddTimed(preset, seconds);
            return $"{preset.name} for {seconds:0.#}s.";
        }

        // --- Quests ----------------------------------------------------------------

        private string Quest(string[] args)
        {
            if (args.Length < 1) throw new UsageException();
            if (!Need(_quests, "QuestTracker", out string missing)) return missing;

            QuestLog log = _quests.Log;
            if (Is(args[0], "list")) return ListQuests(log);
            if (args.Length < 2) throw new UsageException();

            QuestProgress quest = FindQuest(log, args[1], out string error);
            if (quest == null) return error;
            QuestDefinition definition = quest.Definition;

            if (Is(args[0], "start"))
                return log.SkipTo(definition, 0) ? $"Started {definition.Title}." : "Couldn't start it.";

            if (Is(args[0], "complete"))
                return log.SkipTo(definition, int.MaxValue) ? $"Completed {definition.Title}." : "Couldn't complete it.";

            if (Is(args[0], "step"))
            {
                if (args.Length < 3) throw new UsageException();
                int step = Mathf.Max(0, ParseInt(args[2]));
                log.SkipTo(definition, step);
                return $"{definition.Title} now at objective {step} ({quest.State}).";
            }

            throw new UsageException();
        }

        private static string ListQuests(QuestLog log)
        {
            if (log.Quests.Count == 0) return "No quests registered.";

            var text = new StringBuilder();
            foreach (QuestProgress quest in log.Quests)
            {
                text.Append(quest.Definition.name).Append("  [").Append(quest.State).Append("]  ").Append(quest.Definition.Title).Append('\n');
                for (int i = 0; i < quest.Objectives.Count; i++)
                {
                    ObjectiveProgress o = quest.Objectives[i];
                    text.Append("   ").Append(i).Append(": ").Append(o.Definition.Description)
                        .Append(' ').Append(o.Count).Append('/').Append(o.Definition.RequiredCount).Append('\n');
                }
            }
            return text.ToString().TrimEnd();
        }

        private static QuestProgress FindQuest(QuestLog log, string query, out string error)
        {
            var definitions = new QuestDefinition[log.Quests.Count];
            for (int i = 0; i < definitions.Length; i++) definitions[i] = log.Quests[i].Definition;

            QuestDefinition found = DevCatalog.Find(definitions, query, d => d.Title, out error);
            return found != null ? log.Get(found) : null;
        }

        // --- Catalog ---------------------------------------------------------------

        private string List(string[] args)
        {
            if (args.Length < 1) throw new UsageException();
            if (_catalog == null) return "Assign a DevCatalog to DevCommands.";

            var text = new StringBuilder();
            if (Is(args[0], "items"))        foreach (var i in _catalog.Items)   { if (i != null) text.Append(i.name).Append("  (").Append(i.DisplayName).Append(")\n"); }
            else if (Is(args[0], "weapons")) foreach (var w in _catalog.Weapons) { if (w != null) text.Append(w.name).Append('\n'); }
            else if (Is(args[0], "enemies")) foreach (var e in _catalog.Enemies) { if (e != null) text.Append(e.name).Append('\n'); }
            else if (Is(args[0], "buffs"))   foreach (var b in _catalog.Buffs)   { if (b != null) text.Append(b.name).Append('\n'); }
            else throw new UsageException();

            return text.Length > 0 ? text.ToString().TrimEnd() : "(empty)";
        }

        // --- Helpers ---------------------------------------------------------------

        private T Find<T>(System.Collections.Generic.IReadOnlyList<T> options, string query, Func<T, string> displayName, out string error)
            where T : UnityEngine.Object
        {
            if (options == null)
            {
                error = "Assign a DevCatalog to DevCommands.";
                return null;
            }
            return DevCatalog.Find(options, query, displayName, out error);
        }

        private Vector3 SpawnPoint()
        {
            Transform aim = _aim != null ? _aim : transform;
            return Physics.Raycast(aim.position, aim.forward, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore)
                ? hit.point
                : aim.position + aim.forward * 8f;
        }

        private static Vector3 OnNavMesh(Vector3 point) =>
            NavMesh.SamplePosition(point, out NavMeshHit hit, 5f, NavMesh.AllAreas) ? hit.position : point;

        private Vector3 FlatForward()
        {
            Vector3 forward = (_aim != null ? _aim : transform).forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        }

        private static bool Need(UnityEngine.Object dependency, string what, out string message)
        {
            message = dependency == null ? $"Needs a {what} (on the Player, or assigned on DevCommands)." : null;
            return dependency != null;
        }

        private static bool Is(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static int ParseInt(string text) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : throw new UsageException();

        private static float ParseFloat(string text) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : throw new UsageException();

        private static bool ParseOnOff(string text) =>
            Is(text, "on") || Is(text, "1") || Is(text, "true") ? true
            : Is(text, "off") || Is(text, "0") || Is(text, "false") ? false
            : throw new UsageException();

        private static ItemTier ParseTier(string text) =>
            Enum.TryParse(text, true, out ItemTier tier) ? tier
            : int.TryParse(text, out int index) && index >= 0 && index < ItemTiers.TierCount ? (ItemTier)index
            : throw new UsageException();
    }
}
