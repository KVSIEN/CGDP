using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Editor
{
    // CGD › AI › Enemy Roster: every enemy type side by side — toughness, damage, senses, speed,
    // what it thinks with and whether anything about it looks wrong — sortable and searchable,
    // for balancing one type against the others. Click a name to edit it, a tree to open it.
    internal sealed class EnemyRosterWindow : EditorWindow
    {
        private sealed class Column
        {
            public readonly string Title;
            public readonly float  Width;
            public readonly Func<EnemyData, float> SortKey;   // null: sorted by text instead
            public readonly Func<EnemyData, string> Text;

            public Column(string title, float width, Func<EnemyData, string> text, Func<EnemyData, float> sortKey = null)
            {
                Title = title; Width = width; Text = text; SortKey = sortKey;
            }
        }

        private static readonly Column[] Columns =
        {
            new("Enemy",       170f, d => d.name),
            new("Tier",         40f, d => d.Tier.ToString(),                              d => d.Tier),
            new("Team",         70f, d => d.Team.ToString()),
            new("Type",         60f, d => d.CombatType.ToString()),
            new("Toughness",    75f, d => d.EffectiveHealth.ToString("0"),                 d => d.EffectiveHealth),
            new("Melee /s",     65f, d => d.MeleeDamagePerSecond.ToString("0.#"),          d => d.MeleeDamagePerSecond),
            new("Ranged /s",    70f, d => d.RangedDamagePerSecond.ToString("0.#"),         d => d.RangedDamagePerSecond),
            new("Sight",        55f, d => $"{d.SightRange:0} m",                          d => d.SightRange),
            new("Run",          55f, d => $"{d.ChaseSpeed:0.#} m/s",                      d => d.ChaseSpeed),
            new("Tree",        170f, d => d.BehaviorTree != null ? d.BehaviorTree.name : "—"),
            new("Doctrine",    100f, d => d.Doctrine != null ? d.Doctrine.name : "—"),
        };

        private const float IssuesWidth = 60f;

        private List<EnemyData> _all = new();
        private readonly Dictionary<EnemyData, List<EnemyDataReview.Issue>> _issues = new();
        private string  _search = "";
        private int     _sortColumn;
        private bool    _descending;
        private Vector2 _scroll;

        [MenuItem("CGD/AI/Enemy Roster")]
        public static void OpenWindow()
        {
            var window = GetWindow<EnemyRosterWindow>("Enemy Roster");
            window.minSize = new Vector2(600f, 300f);
            window.Refresh();
            window.Show();
        }

        private void OnEnable() => Refresh();

        // Picks up edits made elsewhere (inspector, tree editor) when the window comes back into focus.
        private void OnFocus() => Refresh();

        private void Refresh()
        {
            _all = EnemyAssetLibrary.AllEnemyData();
            _issues.Clear();
            foreach (EnemyData data in _all) _issues[data] = EnemyDataReview.Check(data);
            Sort();
        }

        private void Sort()
        {
            Column column = Columns[_sortColumn];
            _all.Sort((a, b) =>
            {
                int order = column.SortKey != null
                    ? column.SortKey(a).CompareTo(column.SortKey(b))
                    : string.Compare(column.Text(a), column.Text(b), StringComparison.OrdinalIgnoreCase);
                return _descending ? -order : order;
            });
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawHeader();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (EnemyData data in _all)
            {
                if (data == null) continue;
                if (_search.Length > 0 && data.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                DrawRow(data);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("Toughness = raw damage to kill (health + shield, behind armour). " +
                                       "Damage /s = non-stop attacking at base speed, every hit landing.", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.Width(220f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton)) Refresh();
                if (GUILayout.Button("New Enemy Type…", EditorStyles.toolbarButton)) NewEnemyTypeWindow.OpenWindow();
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < Columns.Length; i++)
                {
                    string arrow = i == _sortColumn ? (_descending ? " ▼" : " ▲") : "";
                    if (!GUILayout.Button(Columns[i].Title + arrow, EditorStyles.miniButton, GUILayout.Width(Columns[i].Width))) continue;

                    _descending = i == _sortColumn && !_descending;
                    _sortColumn = i;
                    Sort();
                }
                GUILayout.Label("Checks", EditorStyles.miniBoldLabel, GUILayout.Width(IssuesWidth));
            }
        }

        private void DrawRow(EnemyData data)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < Columns.Length; i++)
                {
                    Column column = Columns[i];
                    if (i == 0)
                    {
                        if (GUILayout.Button(column.Text(data), EditorStyles.linkLabel, GUILayout.Width(column.Width)))
                            Selection.activeObject = data;
                    }
                    else if (column.Title == "Tree" && data.BehaviorTree != null)
                    {
                        if (GUILayout.Button(column.Text(data), EditorStyles.linkLabel, GUILayout.Width(column.Width)))
                            BehaviorTreeWindow.Open(data.BehaviorTree);
                    }
                    else
                    {
                        GUILayout.Label(column.Text(data), GUILayout.Width(column.Width));
                    }
                }
                DrawChecks(data);
            }
        }

        private void DrawChecks(EnemyData data)
        {
            if (!_issues.TryGetValue(data, out List<EnemyDataReview.Issue> issues)) return;

            BtIssueSeverity worst = BtIssueSeverity.Info;
            int count = 0;
            var tooltip = new System.Text.StringBuilder();
            foreach (EnemyDataReview.Issue issue in issues)
            {
                if (issue.Severity == BtIssueSeverity.Info) continue;
                count++;
                if (issue.Severity > worst) worst = issue.Severity;
                tooltip.AppendLine(issue.Message);
            }

            string label = count == 0 ? "✓" : $"{(worst == BtIssueSeverity.Error ? "⛔" : "⚠")} {count}";
            GUILayout.Label(new GUIContent(label, tooltip.ToString().TrimEnd()), GUILayout.Width(IssuesWidth));
        }
    }
}
