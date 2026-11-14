using System.IO;
using CGD.BehaviorTrees;
using CGD.Enemies;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The behavior edits as a designer makes them, from the Behavior Composer or the tree
    // editor's context menu: each one undoable, saved, and announced so an open Behavior Tree
    // Editor redraws.
    internal static class BehaviorActions
    {
        // Asks for a name and moves the branch into a new behavior asset, leaving a Subtree in
        // its place: the tree behaves exactly as before, and the behavior can now be added to
        // other enemy types. False if cancelled or the branch can't be moved.
        public static bool SaveBranchAsBehavior(BehaviorTreeAsset tree, BtNode branch)
        {
            if (branch is BtRoot || tree.FindParent(branch) == null) return false;

            string path = BehaviorLibrary.AskForNewPath(BehaviorLibrary.SuggestName(branch),
                "Save this branch as a behavior other enemy types can use. Put it in a sub-folder to file it under a category (e.g. Behaviors/Fight).");
            if (path == null) return false;

            Undo.RecordObject(tree, "Save Branch as Behavior");
            // Undo puts the branch back in the tree; the new asset stays (delete it if unwanted).
            BehaviorTreeAsset behavior = BehaviorLibrary.CreateEmpty(path);
            BehaviorEditing.ExtractBranch(tree, branch, behavior);

            EditorUtility.SetDirty(behavior);
            Finish(tree, structure: true);
            return true;
        }

        public static void SetOn(BehaviorTreeAsset tree, Subtree behavior, bool on)
        {
            if (behavior.IsOn == on) return;
            Undo.RecordObject(tree, on ? "Switch Behavior On" : "Switch Behavior Off");
            behavior.SetOn(on);
            Finish(tree, structure: false);
        }

        // At `index` in the list, or the end (-1).
        public static void Add(BehaviorTreeAsset tree, BtParentNode list, BehaviorTreeAsset behavior, int index = -1)
        {
            Undo.RecordObject(tree, "Add Behavior");
            BehaviorEditing.AddBehavior(tree, list, behavior, index);
            Finish(tree, structure: true);
        }

        // Asks for a name, makes a new behavior holding an empty Sequence (its checks go first,
        // then what it does), adds it to the list and returns it to open — the way to write a
        // behavior from scratch without first building it inside an enemy's tree. Null if cancelled.
        public static BehaviorTreeAsset CreateEmptyBehavior(BehaviorTreeAsset tree, BtParentNode list, int index = -1)
        {
            string path = BehaviorLibrary.AskForNewPath("NewBehavior",
                "Name the new behavior. Put it in a sub-folder to file it under a category (e.g. Behaviors/Fight).");
            if (path == null) return null;

            BehaviorTreeAsset behavior = BehaviorLibrary.CreateEmpty(path);
            BehaviorEditing.BuildEmptyBehavior(behavior);
            EditorUtility.SetDirty(behavior);

            Undo.RecordObject(tree, "Add New Behavior");
            // Undo takes it out of the list; the new asset stays (delete it if unwanted).
            BehaviorEditing.AddBehavior(tree, list, behavior, index);
            Finish(tree, structure: true);
            return behavior;
        }

        // --- Settings and per-enemy tuning ------------------------------------------------

        // This use's own value for a behavior setting (null clears it back to the behavior's).
        public static void SetUseValue(BehaviorTreeAsset tree, Subtree use, string setting, float? value)
        {
            Undo.RecordObject(tree, value.HasValue ? "Change Behavior Setting" : "Reset Behavior Setting");
            if (value.HasValue) use.SetValue(setting, value.Value);
            else use.ClearValue(setting);
            Finish(tree, structure: false);
        }

        // The enemy type's own value for a setting of `behavior` (null clears it).
        public static void SetEnemyValue(EnemyData enemy, BehaviorTreeAsset behavior, string setting, float? value)
        {
            EditTuning(enemy, behavior, value.HasValue ? "Tune Behavior for Enemy" : "Reset Enemy Tuning", tuning =>
            {
                if (value.HasValue) tuning.SetValue(setting, value.Value);
                else tuning.ClearValue(setting);
            });
        }

        // Switches `behavior` off (or back on) for this enemy type alone.
        public static void SetEnemyOff(EnemyData enemy, BehaviorTreeAsset behavior, bool off)
        {
            EditTuning(enemy, behavior, off ? "Switch Behavior Off for Enemy" : "Switch Behavior On for Enemy", tuning => tuning.SetOff(off));
        }

        private static void EditTuning(EnemyData enemy, BehaviorTreeAsset behavior, string undoName, System.Action<BehaviorTuning> edit)
        {
            Undo.RecordObject(enemy, undoName);
            BehaviorTuning tuning = BehaviorTuning.Find(enemy.BehaviorTuning, behavior);
            if (tuning == null)
            {
                tuning = new BehaviorTuning(behavior);
                enemy.BehaviorTuning.Add(tuning);
            }
            edit(tuning);
            if (tuning.IsEmpty) enemy.BehaviorTuning.Remove(tuning);

            EditorUtility.SetDirty(enemy);
            AssetDatabase.SaveAssets();
            if (enemy.BehaviorTree != null) BtEditorEvents.RaiseAssetEdited(enemy.BehaviorTree);
        }

        public static void Move(BehaviorTreeAsset tree, BtParentNode list, int from, int to)
        {
            Undo.RecordObject(tree, "Reorder Behaviors");
            BehaviorEditing.Move(list, from, to);
            Finish(tree, structure: true);
        }

        // A Subtree just leaves (its behavior asset stays in the library); a branch written
        // into this tree is deleted, after asking.
        public static void Remove(BehaviorTreeAsset tree, BtNode node)
        {
            if (node is not Subtree &&
                !EditorUtility.DisplayDialog("Remove Branch",
                    "This branch is written into this tree, not a shared behavior: removing it deletes its nodes (Undo brings them back). " +
                    "Use Save as Behavior first to keep it.", "Remove", "Cancel"))
                return;

            Undo.RecordObject(tree, "Remove Behavior");
            BehaviorEditing.RemoveBranch(tree, node);
            Finish(tree, structure: true);
        }

        // Points this one Subtree at a copy of its behavior, to change it for this tree alone.
        public static void MakeOwnCopy(BehaviorTreeAsset tree, Subtree behavior)
        {
            if (behavior.Tree == null) return;

            string source = AssetDatabase.GetAssetPath(behavior.Tree);
            string folder = Path.GetDirectoryName(source)?.Replace('\\', '/');
            string owner  = tree.name.EndsWith("BehaviorTree") ? tree.name[..^"BehaviorTree".Length] : tree.name;
            string target = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{owner}{behavior.Tree.name}.asset");
            if (!AssetDatabase.CopyAsset(source, target)) return;

            Undo.RecordObject(tree, "Make Own Copy of Behavior");
            behavior.SetTree(AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(target));
            Finish(tree, structure: false);
        }

        // A new tree laid out like the enemy trees (BehaviorEditing.BuildStarterTree), with the
        // shared regroup, raise alarm and routine behaviors in place; given to `enemy` if set.
        public static BehaviorTreeAsset CreateStarterTree(string path, EnemyData enemy)
        {
            var tree = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            BehaviorEditing.BuildStarterTree(tree,
                Shared("RegroupSubtree"), Shared("RaiseAlarmSubtree"), Shared("RoutineSubtree"));
            AssetDatabase.CreateAsset(tree, path);

            if (enemy != null)
            {
                Undo.RecordObject(enemy, "Give Enemy a New Tree");
                enemy.BehaviorTree = tree;
                EditorUtility.SetDirty(enemy);
            }
            AssetDatabase.SaveAssets();
            return tree;
        }

        private static BehaviorTreeAsset Shared(string name) =>
            AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>($"{BehaviorLibrary.SharedFolder}/{name}.asset");

        private static void Finish(BehaviorTreeAsset tree, bool structure)
        {
            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
            if (structure) BtEditorEvents.RaiseStructureEdited(tree);
            else BtEditorEvents.RaiseAssetEdited(tree);
        }
    }
}
