using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace CGD.Tests
{
    // Guards the project's own assets against the silent breakages that crept in before:
    // a component whose script is gone, the same component twice on one object (one HUD then
    // watched a StatusEffectController that never got the effects), overrides of objects a
    // prefab no longer has, and folders left empty after a move. Reads the YAML as text, so it
    // checks scenes without opening them.
    public class ProjectHealthTests
    {
        private const string Root = "Assets/Project";

        private static readonly Regex ScriptRef  = new(@"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32})");
        private static readonly Regex OwnerRef   = new(@"m_GameObject: \{fileID: (\d+)\}");
        private static readonly Regex BlockStart = new(@"\n(?=--- !u!)");
        private static readonly Regex OverrideTarget = new(@"- target: \{fileID: (-?\d+), guid: ([0-9a-f]{32}), type: 3\}\r?\n\s*propertyPath: ([^\r\n]+)");

        private static IEnumerable<string> Files(params string[] extensions) =>
            Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)
                     .Where(path => extensions.Contains(Path.GetExtension(path)))
                     .Select(path => path.Replace('\\', '/'));

        [Test]
        public void EveryComponentHasItsScript()
        {
            var problems = new List<string>();
            foreach (string path in Files(".prefab", ".unity", ".asset"))
            {
                var missing = ScriptRef.Matches(File.ReadAllText(path)).Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .Where(guid => string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                    .Distinct().ToList();
                if (missing.Count > 0) problems.Add($"{path}: {missing.Count} missing script(s) ({string.Join(", ", missing)})");
            }

            Assert.IsEmpty(problems, "Components whose script no longer exists:\n" + string.Join("\n", problems));
        }

        [Test]
        public void NoComponentIsOnAGameObjectTwice()
        {
            var problems = new List<string>();
            foreach (string path in Files(".prefab", ".unity"))
            {
                var perObject = new Dictionary<string, List<string>>();
                foreach (string block in BlockStart.Split(File.ReadAllText(path)))
                {
                    Match owner = OwnerRef.Match(block), script = ScriptRef.Match(block);
                    if (!owner.Success || !script.Success || owner.Groups[1].Value == "0") continue;

                    string go = owner.Groups[1].Value;
                    if (!perObject.TryGetValue(go, out List<string> scripts)) perObject[go] = scripts = new List<string>();
                    scripts.Add(script.Groups[1].Value);
                }

                foreach (var pair in perObject)
                    foreach (var twice in pair.Value.GroupBy(guid => guid).Where(g => g.Count() > 1))
                        problems.Add($"{path}: GameObject {pair.Key} has {Path.GetFileName(AssetDatabase.GUIDToAssetPath(twice.Key))} {twice.Count()} times");
            }

            Assert.IsEmpty(problems, "Components added twice (only one of them is ever found by GetComponent):\n" + string.Join("\n", problems));
        }

        // An override on a prefab instance whose target is no longer in the prefab (a component
        // that was removed) does nothing but linger in the Overrides dropdown. A prefab with
        // nested prefabs gives their objects derived ids, so only prefabs without any are judged.
        [Test]
        public void NoOverrideTargetsAMissingObject()
        {
            var problems = new List<string>();
            var prefabs = new Dictionary<string, string>();
            foreach (string path in Files(".prefab", ".unity"))
            {
                foreach (Match m in OverrideTarget.Matches(File.ReadAllText(path)))
                {
                    string source = AssetDatabase.GUIDToAssetPath(m.Groups[2].Value);
                    if (!source.EndsWith(".prefab")) continue;

                    if (!prefabs.TryGetValue(source, out string text)) prefabs[source] = text = File.ReadAllText(source);
                    if (text.Contains("--- !u!1001 &")) continue;
                    if (!text.Contains($" &{m.Groups[1].Value}\n") && !text.Contains($" &{m.Groups[1].Value}\r\n"))
                        problems.Add($"{path}: override of {m.Groups[3].Value} on object {m.Groups[1].Value}, which {Path.GetFileName(source)} no longer has");
                }
            }

            Assert.IsEmpty(problems, "Overrides of objects that are gone (Overrides › Remove unused overrides):\n" + string.Join("\n", problems));
        }

        [Test]
        public void NoFolderIsEmpty()
        {
            var empty = Directory.EnumerateDirectories(Root, "*", SearchOption.AllDirectories)
                .Where(dir => !Directory.EnumerateFileSystemEntries(dir).Any(entry => !entry.EndsWith(".meta")))
                .Select(dir => dir.Replace('\\', '/'))
                .ToList();

            Assert.IsEmpty(empty, "Empty folders (delete them with their .meta):\n" + string.Join("\n", empty));
        }
    }
}
