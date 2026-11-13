using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CGD.Combat;
using CGD.Enemies;
using CGD.Factions;

namespace CGD.Editor
{
    // CGD › AI › New Enemy Type: a new kind of enemy without touching prefabs or code. Pick an
    // existing enemy to start from, name it, choose which of its parts it should get its own
    // copy of, and it is ready to tune (EnemyData inspector), re-think (Behavior Composer)
    // and drop into a scene or a faction's roster. See EnemyTypeCreator for what gets made.
    internal sealed class NewEnemyTypeWindow : EditorWindow
    {
        private readonly EnemyTypeCreator.Request _request = new();
        private List<GameObject> _templates;
        private string[]         _templateNames;
        private int              _templateIndex = -1;
        private Vector2          _scroll;

        [MenuItem("CGD/AI/New Enemy Type…")]
        public static void OpenWindow() => Open(null);

        public static void Open(GameObject template)
        {
            var window = GetWindow<NewEnemyTypeWindow>(true, "New Enemy Type");
            window.minSize = new Vector2(420f, 520f);
            window.LoadTemplates();
            if (template != null) window.UseTemplate(template);
            window.Show();
        }

        private void OnEnable() => LoadTemplates();

        private void LoadTemplates()
        {
            _templates     = EnemyTypeCreator.Templates();
            _templateNames = new string[_templates.Count];
            for (int i = 0; i < _templates.Count; i++) _templateNames[i] = _templates[i].name;
            _templateIndex = _templates.IndexOf(_request.Template);
        }

        private void UseTemplate(GameObject template)
        {
            _request.Template = template;
            _templateIndex    = _templates.IndexOf(template);
            EnemyData data = template != null && template.TryGetComponent(out EnemyAI ai) ? ai.Data : null;
            if (data == null) return;

            _request.Tier = data.Tier;
            _request.Team = data.Team;
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.HelpBox(
                "Starts from an enemy that already works and copies it. Afterwards, tune the numbers in the new " +
                "Enemy Data, and pick its behaviors in the Behavior Composer.", MessageType.None);

            Section("1. Start from");
            int picked = EditorGUILayout.Popup("Template", _templateIndex, _templateNames);
            if (picked != _templateIndex && picked >= 0) UseTemplate(_templates[picked]);
            EnemyData template = TemplateData();
            if (template != null)
                foreach (string line in EnemyDataReview.Summary(template))
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);

            Section("2. Name it");
            _request.Name        = EditorGUILayout.TextField(new GUIContent("Asset Name", "Used for its files, e.g. Stalker → StalkerEnemyData, Stalker.prefab"), _request.Name);
            _request.DisplayName = EditorGUILayout.TextField(new GUIContent("Display Name", "Shown in game (kill feed, labels). Empty = the asset name"), _request.DisplayName);
            _request.Tier        = EditorGUILayout.IntSlider(new GUIContent("Tier", "1 = rank and file; higher tiers are tougher and roll more affixes"), _request.Tier, 1, 3);
            _request.Team        = (Team)EditorGUILayout.EnumPopup(new GUIContent("Team", "Who it fights: enemies attack every other team"), _request.Team);

            Section("3. What it gets its own copy of");
            EditorGUILayout.LabelField(
                "Ticked parts are copied so you can change them for this enemy alone. Unticked parts stay shared " +
                "with the template: editing them later changes every enemy that uses them.", EditorStyles.wordWrappedMiniLabel);
            DrawTreeChoice(template);
            _request.OwnSenses           = OwnCopyToggle("Senses", "How quickly it notices, remembers and searches", template != null ? template.Senses : null, _request.OwnSenses);
            _request.OwnTargetPriorities = OwnCopyToggle("Target Priorities", "Whom it prefers to fight among several hostiles", template != null ? template.TargetPriorities : null, _request.OwnTargetPriorities);
            _request.OwnDoctrine         = OwnCopyToggle("Doctrine", "Its faction's manner: morale, idling, tells, escalation", template != null ? template.Doctrine : null, _request.OwnDoctrine);
            _request.OwnVoice            = OwnCopyToggle("Voice", "What it says out loud", template != null ? template.Voice : null, _request.OwnVoice);

            Section("4. Where it spawns (optional)");
            _request.Faction = (FactionDefinition)EditorGUILayout.ObjectField(
                new GUIContent("Faction", "Adds the new prefab to this faction's roster at its tier, so generated levels spawn it"),
                _request.Faction, typeof(FactionDefinition), false);

            EditorGUILayout.Space();
            DrawPlan();
            EditorGUILayout.EndScrollView();
            DrawCreateButton();
        }

        private EnemyData TemplateData() =>
            _request.Template != null && _request.Template.TryGetComponent(out EnemyAI ai) ? ai.Data : null;

        private static readonly GUIContent[] TreeChoices =
        {
            new("Share the template's tree", "Thinks exactly like the template; edits to that tree change both"),
            new("Copy the template's tree", "Starts with all of the template's behaviors, to switch off, reorder or change"),
            new("Start from a starter tree", "Regroup, raise alarm, an empty fight list and the routine: pick its behaviors from the library"),
        };

        private void DrawTreeChoice(EnemyData template)
        {
            int choice = _request.StarterTree ? 2 : _request.OwnTree ? 1 : 0;
            choice = EditorGUILayout.Popup(new GUIContent("Behavior Tree", "How it decides what to do"), choice, TreeChoices);
            _request.OwnTree     = choice == 1 && template != null && template.BehaviorTree != null;
            _request.StarterTree = choice == 2;
        }

        private static bool OwnCopyToggle(string label, string tooltip, Object shared, bool value)
        {
            using (new EditorGUI.DisabledScope(shared == null))
            {
                string state = shared == null ? "(template has none)" : value ? "own copy" : $"shares {shared.name}";
                return EditorGUILayout.ToggleLeft(new GUIContent($"{label}  —  {state}", tooltip), value && shared != null);
            }
        }

        private void DrawPlan()
        {
            string problem = EnemyTypeCreator.Problem(_request);
            if (problem != null)
            {
                EditorGUILayout.HelpBox(problem, MessageType.Info);
                return;
            }

            var plan = new List<string>
            {
                EnemyTypeCreator.DataPath(_request.Name),
                $"{EnemyTypeCreator.PrefabPath(_request)}  (variant of {_request.Template.name})",
            };
            if (_request.Faction != null) plan.Add($"added to {_request.Faction.name}, tier {_request.Tier}");
            EditorGUILayout.HelpBox("Will create:\n• " + string.Join("\n• ", plan), MessageType.None);
        }

        private void DrawCreateButton()
        {
            using (new EditorGUI.DisabledScope(EnemyTypeCreator.Problem(_request) != null))
            {
                if (!GUILayout.Button("Create Enemy Type", GUILayout.Height(32f))) return;
            }

            EnemyTypeCreator.Result result = EnemyTypeCreator.Create(_request);
            Selection.activeObject = result.Data;
            EditorGUIUtility.PingObject(result.Data);
            if ((_request.OwnTree || _request.StarterTree) && result.Data.BehaviorTree != null)
                BehaviorComposerWindow.Open(result.Data, result.Data.BehaviorTree);
            Close();
            GUIUtility.ExitGUI();
        }

        private static void Section(string title)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }
    }
}
