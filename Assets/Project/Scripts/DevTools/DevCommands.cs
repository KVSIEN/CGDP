using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using CGD.Core;
using CGD.Expedition;
using CGD.Items;
using CGD.Player;
using CGD.Quests;
using CGD.Stealth;
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
            Console.Register("give",      "<item> [count]",           "Add items to the inventory (gear is rolled)", Give,
                example: "/give scrap 20",
                complete: (_, i) => i == 0 ? DevCatalog.Tokens(_catalog != null ? _catalog.Items : null, d => d.DisplayName) : null);
            Console.Register("weapon",    "<category> [seed] [tier] [layer:variant…]", "Add a generated weapon — same seed, tier and layers give the same gun", Weapon,
                example: "/weapon sniper 42 legendary",
                complete: WeaponOptions);
            Console.Register("reroll",    "<layer>",                  "Reroll one seed layer of the weapon in hand; the rest stays the same", Reroll,
                example: "/reroll stats",
                complete: (_, i) => i == 0 ? HeldWeaponLayers() : null);
            Console.Register("spawn",     "<enemy> [count]",          "Spawn enemies where you're looking", Spawn,
                example: "/spawn husk 3",
                complete: (_, i) => i == 0 ? DevCatalog.Tokens(_catalog != null ? _catalog.Enemies : null, null) : null);
            Console.Register("god",       "[on|off]",                 "Toggle taking no damage", God,
                complete: (_, i) => i == 0 ? OnOff : null);
            Console.Register("heal",      "",                         "Restore full health", _ => Heal());
            Console.Register("restrain",  "[taps]",                   "Hold the player in place until they mash Interact (placeholder grab)", Restrain,
                example: "/restrain 10");
            Console.Register("free",      "",                         "Let go of the player", _ => Free());
            Console.Register("cloak",     "[seconds]",                "Become hidden anywhere for a while", Cloak,
                example: "/cloak 60");
            Console.Register("buff",      "<preset> [seconds]",       "Apply a stat modifier preset for a while", Buff,
                example: "/buff damage 10",
                complete: (_, i) => i == 0 ? DevCatalog.Tokens(_catalog != null ? _catalog.Buffs : null, null) : null);
            Console.Register("dodge",     "[type]",                   "Show or switch the dodge type", Dodge,
                example: "/dodge roll",
                complete: (_, i) => i == 0 ? DevCatalog.Tokens(_catalog != null ? _catalog.Dodges : null, d => d.DisplayName) : null);
            Console.Register("timescale", "<scale>",                  "Set game speed (1 = normal)", TimeScale,
                example: "/timescale 0.5",
                complete: (_, i) => i == 0 ? TimeScales : null);
            Console.Register("revealmap", "",                         "Uncover all fog of war", _ => RevealMap());
            Console.Register("quest",     "list | start <q> | step <q> <n> | complete <q>", "Inspect or jump quest progress", Quest,
                example: "/quest step main 2",
                complete: QuestOptions);
            Console.Register("save",      "[info|write|wipe]",        "Show the ship save, write it now, or wipe it for a fresh start", Save,
                example: "/save wipe",
                complete: (_, i) => i == 0 ? SaveVerbs : null);
            Console.Register("list",      "items|weapons|enemies|buffs|dodges", "Show what the catalog can hand out — the names printed are what you type", ListCatalog,
                example: "/list weapons",
                complete: (_, i) => i == 0 ? ListKinds : null);
        }

        private static readonly string[] OnOff      = { "on", "off" };
        private static readonly string[] TimeScales = { "0.25", "0.5", "1", "2", "5" };
        private static readonly string[] ListKinds  = { "items", "weapons", "enemies", "buffs", "dodges" };
        private static readonly string[] QuestVerbs = { "list", "start", "step", "complete" };
        private static readonly string[] SaveVerbs  = { "info", "write", "wipe" };

        // --- Completion ------------------------------------------------------------

        private IEnumerable<string> WeaponOptions(string[] previous, int index)
        {
            if (index == 0) return DevCatalog.Tokens(_catalog != null ? _catalog.Weapons : null, c => c.DisplayName);
            if (index == 2) return Enum.GetNames(typeof(ItemTier));
            return null;
        }

        private IEnumerable<string> HeldWeaponLayers() =>
            _loadout != null && _loadout.Active != null && _loadout.Active.Definition is WeaponCategory category ? category.SeedLayers : null;

        private IEnumerable<string> QuestOptions(string[] previous, int index)
        {
            if (index == 0) return QuestVerbs;
            if (index != 1 || _quests == null || Is(previous[0], "list")) return null;

            return _quests.Log.Quests.Select(q => DevCatalog.Token(q.Definition, d => d.Title));
        }

        // --- Items -----------------------------------------------------------------

        private string Give(string[] args)
        {
            if (args.Length < 1) throw new UsageException();
            if (!Need(_inventory, "PlayerInventory", out string missing)) return missing;

            ItemDefinition item = Find(_catalog != null ? _catalog.Items : null, args[0], i => i.DisplayName, "items", out string error);
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

            WeaponCategory category = Find(_catalog != null ? _catalog.Weapons : null, args[0], c => c.DisplayName, "weapons", out string error);
            if (category == null) return error;

            Seed seed = args.Length > 1 ? Seed.Parse(args[1]) : Seed.Random();
            ItemTier tier = args.Length > 2 ? ParseTier(args[2]) : category.Tier;
            SeedVariants variants = args.Length > 3 ? ParseVariants(string.Join(" ", args, 3, args.Length - 3).ToLowerInvariant()) : null;
            WeaponItem weapon = category.Generate(category.Roll(tier, seed, variants));

            WeaponItem replaced = _loadout.AddWeapon(weapon);
            if (replaced != null && _inventory != null) _inventory.Inventory.Add(replaced);
            return Describe(weapon);
        }

        // Swaps the weapon in hand for the same weapon with one layer rerolled. Attachments
        // and the loaded magazine don't carry over; the old weapon is discarded.
        private string Reroll(string[] args)
        {
            if (args.Length < 1) throw new UsageException();
            if (!Need(_loadout, "PlayerWeaponLoadout", out string missing)) return missing;
            if (!Need(_inventory, "PlayerInventory", out missing)) return missing;

            WeaponItem held = _loadout.Active;
            if (held == null || held.Definition is not WeaponCategory category) return "No generated weapon in hand.";

            string layer = args[0].ToLowerInvariant();
            if (!category.SeedLayers.Contains(layer))
                return $"No layer '{args[0]}'. Layers: {string.Join(", ", category.SeedLayers)}.";

            WeaponItem rerolled = category.Generate(category.RerollLayer(held, layer));
            _inventory.Inventory.Add(rerolled);
            if (!_loadout.AssignMain(_loadout.ActiveSlot, rerolled))
            {
                _inventory.Inventory.Remove(rerolled);
                return "Couldn't swap the weapon in hand.";
            }
            _inventory.Inventory.Remove(held);
            return Describe(rerolled);
        }

        // Ends in the seed text, so "weapon <category> <seed text's seed> <tier> <its layers>" rebuilds the gun.
        private static string Describe(WeaponItem weapon) =>
            $"{weapon.DisplayName} ({weapon.Tier}, Q{weapon.Quality}) — seed {weapon.SeedText}";

        // --- World -----------------------------------------------------------------

        private string Spawn(string[] args)
        {
            if (args.Length < 1) throw new UsageException();

            GameObject prefab = Find(_catalog != null ? _catalog.Enemies : null, args[0], null, "enemies", out string error);
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

        private string Restrain(string[] args)
        {
            if (!TryGetComponent(out PlayerRestraint restraint)) return "Needs a PlayerRestraint on the Player.";

            int taps = args.Length > 0 ? Mathf.RoundToInt(ParseFloat(args[0])) : 0;
            restraint.Restrain("Break free", taps);
            return "Restrained. Mash Interact to break free.";
        }

        private string Free()
        {
            if (!TryGetComponent(out PlayerRestraint restraint)) return "Needs a PlayerRestraint on the Player.";

            restraint.Release();
            return "Released.";
        }

        private string Cloak(string[] args)
        {
            if (!TryGetComponent(out Stealthable stealth)) return "Needs a Stealthable on the Player.";

            float seconds = args.Length > 0 ? ParseFloat(args[0]) : 30f;
            stealth.Cloak(seconds, breaksOnAttack: false);
            return $"Cloaked for {seconds:0.#}s.";
        }

        private string Buff(string[] args)
        {
            if (args.Length < 1) throw new UsageException();
            if (!Need(_stats, "CharacterStats", out string missing)) return missing;

            StatModifierPreset preset = Find(_catalog != null ? _catalog.Buffs : null, args[0], null, "buffs", out string error);
            if (preset == null) return error;

            float seconds = args.Length > 1 ? ParseFloat(args[1]) : 30f;
            _stats.AddTimed(preset, seconds);
            return $"{preset.name} for {seconds:0.#}s.";
        }

        private string Dodge(string[] args)
        {
            if (!TryGetComponent(out PlayerDodge dodge)) return "No PlayerDodge on this object.";
            if (args.Length == 0)
                return dodge.Definition != null ? $"Dodge: {dodge.Definition.name}. Switch with dodge <type> (see list dodges)." : "No dodge type set.";

            DodgeDefinition definition = Find(_catalog != null ? _catalog.Dodges : null, args[0], d => d.DisplayName, "dodges", out string error);
            if (definition == null) return error;

            dodge.Definition = definition;
            return $"Dodge: {definition.name}.";
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

        // --- Save ------------------------------------------------------------------

        private string Save(string[] args)
        {
            ExpeditionSession session = ExpeditionSession.Instance;
            if (session == null) return "No ExpeditionSession — the scene needs the GameFlow prefab.";

            ShipPersistence save = session.Persistence;
            if (save == null) return "Saving is off: assign an ItemCatalog on ExpeditionSession (and tick Save In Editor to save in the editor).";

            string verb = args.Length > 0 ? args[0] : "info";
            if (Is(verb, "write"))
                return save.Save() ? $"Saved to {save.Path}{SkippedNote(save.SkippedOnSave, "left out")}" : $"Couldn't write {save.Path}.";
            if (Is(verb, "wipe"))
            {
                save.Wipe();
                return "Ship hold and kit emptied, save deleted.";
            }
            if (!Is(verb, "info")) throw new UsageException();

            string loaded = save.LastLoad switch
            {
                ShipLoadResult.Loaded   => $"loaded at start{SkippedNote(save.SkippedOnLoad, "couldn't be restored")}",
                ShipLoadResult.SetAside => "unreadable at start, moved aside (*.unreadable)",
                _                       => "none at start",
            };
            return $"{save.Path}\nSave: {loaded}. {(save.IsDirty ? "Unsaved changes." : "Up to date.")}";
        }

        private static string SkippedNote(int skipped, string what) => skipped > 0 ? $" ({skipped} entries {what})" : string.Empty;

        // --- Catalog ---------------------------------------------------------------

        private string ListCatalog(string[] args)
        {
            if (args.Length < 1) return $"Pick one: {string.Join(", ", ListKinds)}. Try /list weapons.";
            if (_catalog == null) return "Assign a DevCatalog to DevCommands.";

            List<string> names =
                Is(args[0], "items")   ? ListNames(_catalog.Items, i => i.DisplayName)
                : Is(args[0], "weapons") ? ListNames(_catalog.Weapons, w => w.DisplayName)
                : Is(args[0], "enemies") ? ListNames(_catalog.Enemies, null)
                : Is(args[0], "buffs")   ? ListNames(_catalog.Buffs, null)
                : Is(args[0], "dodges")  ? ListNames(_catalog.Dodges, d => d.DisplayName)
                : throw new UsageException();

            return names.Count > 0 ? string.Join("\n", names) : "(empty)";
        }

        // One line per entry: the word to type, and the display name when it differs.
        private static List<string> ListNames<T>(IReadOnlyList<T> options, Func<T, string> displayName) where T : UnityEngine.Object
        {
            var lines = new List<string>();
            foreach (T option in options)
            {
                if (option == null) continue;

                string token   = DevCatalog.Token(option, displayName);
                string display = displayName?.Invoke(option);
                lines.Add(string.IsNullOrEmpty(display) || display == token ? token : $"{token}  ({display})");
            }
            return lines;
        }

        // --- Helpers ---------------------------------------------------------------

        // On a miss, points at the matching /list so the player can see what is available.
        private T Find<T>(IReadOnlyList<T> options, string query, Func<T, string> displayName, string listKind, out string error)
            where T : UnityEngine.Object
        {
            if (options == null)
            {
                error = "Assign a DevCatalog to DevCommands.";
                return null;
            }

            T found = DevCatalog.Find(options, query, displayName, out error);
            if (found == null) error += $" Type /list {listKind} to see them.";
            return found;
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

        private static SeedVariants ParseVariants(string text) =>
            SeedVariants.TryParse(text, out SeedVariants variants) ? variants : throw new UsageException();

        private static ItemTier ParseTier(string text) =>
            Enum.TryParse(text, true, out ItemTier tier) ? tier
            : int.TryParse(text, out int index) && index >= 0 && index < ItemTiers.TierCount ? (ItemTier)index
            : throw new UsageException();
    }
}
