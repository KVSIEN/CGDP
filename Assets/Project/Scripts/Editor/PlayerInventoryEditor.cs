using UnityEditor;
using UnityEngine;
using CGD.Items;
using CGD.Player;

namespace CGD.Editor
{
    // Runtime view of PlayerInventory's contents.
    //
    // The Inventory is a plain C# class behind a get-only property, so Unity's
    // serializer cannot show it and the default Inspector is blank below the
    // authored fields. This draws the live contents instead, and offers a grant
    // control for testing loot and ammo flows without hunting for a pickup.
    //
    // Play mode only — outside it there is no Inventory to read, and the
    // authored StartingStacks array on the component is the thing to edit.
    [CustomEditor(typeof(PlayerInventory))]
    public class PlayerInventoryEditor : UnityEditor.Editor
    {
        private ItemDefinition _grantDefinition;
        private int            _grantCount = 30;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var inventory = (PlayerInventory)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Contents", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter play mode to view contents. Use Starting Contents above to author what the player spawns with.",
                    MessageType.Info);
                return;
            }

            DrawSummary(inventory);
            DrawStacks(inventory.Inventory);
            DrawGear(inventory.Inventory);
            DrawGrantControl(inventory.Inventory);

            // Contents change from gameplay, not from Inspector interaction, so the
            // view needs repainting every frame to stay truthful.
            Repaint();
        }

        private static void DrawSummary(PlayerInventory inventory)
        {
            Inventory contents = inventory.Inventory;

            EditorGUILayout.LabelField(
                $"Slots {contents.SlotCount} / {inventory.MaxSlots}",
                $"Weight {contents.TotalWeight:0.##} / {inventory.MaxWeight:0.##} kg");

            DrawAmmoPools(contents);
        }

        // Ammo is the pool weapons actually draw against, so it gets its own
        // readout rather than being buried among the stacks.
        private static void DrawAmmoPools(Inventory contents)
        {
            bool any = false;

            foreach (AmmoType ammo in System.Enum.GetValues(typeof(AmmoType)))
            {
                if (ammo == AmmoType.None || ammo == AmmoType.Cooldown) continue;

                int count = contents.CountOf(ammo);
                if (count <= 0) continue;

                if (!any)
                {
                    EditorGUILayout.LabelField("Ammo pools", EditorStyles.miniBoldLabel);
                    any = true;
                }

                EditorGUILayout.LabelField($"    {ammo}", count.ToString());
            }
        }

        private static void DrawStacks(Inventory contents)
        {
            EditorGUILayout.LabelField($"Stacks ({contents.Stacks.Count})", EditorStyles.miniBoldLabel);

            if (contents.Stacks.Count == 0)
            {
                EditorGUILayout.LabelField("    empty");
                return;
            }

            foreach (ItemStack stack in contents.Stacks)
            {
                string name = stack.Definition != null ? stack.Definition.DisplayName : "<missing>";
                EditorGUILayout.LabelField($"    {name}", $"x{stack.Count}");
            }
        }

        private static void DrawGear(Inventory contents)
        {
            if (contents.Items.Count == 0) return;

            EditorGUILayout.LabelField($"Gear ({contents.Items.Count})", EditorStyles.miniBoldLabel);

            foreach (ItemInstance item in contents.Items)
                EditorGUILayout.LabelField($"    {item.DisplayName}", $"{item.Tier} Q{item.Quality}");
        }

        private void DrawGrantControl(Inventory contents)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grant (debug)", EditorStyles.miniBoldLabel);

            _grantDefinition = (ItemDefinition)EditorGUILayout.ObjectField(
                "Item", _grantDefinition, typeof(ItemDefinition), false);
            _grantCount = Mathf.Max(1, EditorGUILayout.IntField("Count", _grantCount));

            using (new EditorGUI.DisabledScope(_grantDefinition == null))
            {
                if (GUILayout.Button("Add to Inventory"))
                    contents.Add(_grantDefinition, _grantCount);
            }

            if (_grantDefinition != null && !_grantDefinition.IsStackable)
                EditorGUILayout.HelpBox(
                    "This definition has MaxStack 1, so each unit opens its own stack. Gear should be rolled into an ItemInstance rather than added this way.",
                    MessageType.Warning);
        }
    }
}
