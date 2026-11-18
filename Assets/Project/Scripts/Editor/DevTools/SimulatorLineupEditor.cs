using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CGD.Combat;
using CGD.DevTools;
using CGD.Enemies;

namespace CGD.Editor
{
    // The SimulatorLineup inspector: squads as cards — name, an enemy dropdown with a count for
    // each kind, where they appear and which route they walk — plus handles in the Scene view
    // to drag each squad's spot where it should start. A button hands the lineup to the open
    // scene's LineupSpawner, so what is set here is what plays.
    [CustomEditor(typeof(SimulatorLineup))]
    public sealed class SimulatorLineupEditor : UnityEditor.Editor
    {
        private const float HandleSize  = 0.6f;
        private const float StepOutward = 6f;   // metres a new squad starts from the last one

        private sealed class Choice
        {
            public GameObject Prefab;
            public string     Path;     // "Bio/BioTestAgent", the dropdown's grouping
            public Team       Team;
            public Color      Color;
        }

        private static readonly Color Neutral = new(0.75f, 0.78f, 0.85f);

        private readonly HashSet<LineupSquad> _folded = new();
        private List<Choice>   _choices;
        private LineupSpawner  _spawner;
        private LineupSquad    _selected;
        private System.Action  _deferred;   // list changes wait until the squads have all been drawn

        private SimulatorLineup Lineup => (SimulatorLineup)target;

        private void OnEnable()
        {
            LoadChoices();
            _spawner = FindFirstObjectByType<LineupSpawner>();
            SceneView.duringSceneGui += DrawInScene;
        }

        private void OnDisable() => SceneView.duringSceneGui -= DrawInScene;

        // Picks up prefabs made while the inspector was open when it comes back into focus.
        private void OnFocus() => LoadChoices();

        private void LoadChoices()
        {
            _choices = new List<Choice>();
            foreach (GameObject prefab in EnemyAssetLibrary.AllEnemyPrefabs())
            {
                EnemyData data = prefab.GetComponent<EnemyAI>().Data;
                Team team = data != null ? data.Team : Team.None;
                _choices.Add(new Choice
                {
                    Prefab = prefab,
                    Path   = $"{team}/{prefab.name}",
                    Team   = team,
                    Color  = data != null ? data.DamageNumberColor : Neutral,
                });
            }
            _choices.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
        }

        public override void OnInspectorGUI()
        {
            DrawSummary();
            DrawSceneLink();
            EditorGUILayout.Space(6f);

            List<LineupSquad> squads = Lineup.Squads;
            for (int i = 0; i < squads.Count; i++)
                DrawSquad(squads, i);

            if (_deferred != null)
            {
                System.Action change = _deferred;
                _deferred = null;
                change();
                Repaint();
            }

            EditorGUILayout.Space(4f);
            if (EditorGUILayout.DropdownButton(new GUIContent("+ Add squad"), FocusType.Passive))
                ShowAddSquadMenu();
        }

        private void DrawSummary()
        {
            int squads = 0, enemies = 0;
            foreach (LineupSquad squad in Lineup.Squads)
            {
                if (!squad.Enabled) continue;
                squads++;
                enemies += squad.Size;
            }
            EditorGUILayout.HelpBox($"{squads} squad{(squads == 1 ? "" : "s")}  ·  {enemies} enem{(enemies == 1 ? "y" : "ies")} on play", MessageType.None);
        }

        private void DrawSceneLink()
        {
            if (_spawner == null) _spawner = FindFirstObjectByType<LineupSpawner>();
            if (_spawner == null)
            {
                EditorGUILayout.HelpBox("The open scene has no LineupSpawner. Open AISimulator to play this lineup.", MessageType.Info);
                return;
            }

            if (_spawner.Lineup == Lineup)
            {
                EditorGUILayout.HelpBox($"Played by {_spawner.gameObject.scene.name} ({_spawner.name}).", MessageType.None);
                return;
            }

            if (!GUILayout.Button($"Play this lineup in {_spawner.gameObject.scene.name}")) return;
            var spawner = new SerializedObject(_spawner);
            spawner.FindProperty("_lineup").objectReferenceValue = Lineup;
            spawner.ApplyModifiedProperties();
            EditorUtility.SetDirty(_spawner);
        }

        private void DrawSquad(List<LineupSquad> squads, int index)
        {
            LineupSquad squad = squads[index];
            bool folded = _folded.Contains(squad);
            bool open = !folded;
            Color tint = ColorOf(squad);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Color before = GUI.color;
                    GUI.color = tint;
                    open = EditorGUILayout.Foldout(!folded, GUIContent.none, true);
                    GUI.color = before;
                    if (open == folded) Fold(squad, !open);

                    EditorGUI.BeginChangeCheck();
                    string name = EditorGUILayout.TextField(squad.Name, EditorStyles.boldLabel);
                    bool enabled = EditorGUILayout.ToggleLeft($"{squad.Size}", squad.Enabled, GUILayout.Width(44f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Edit("Edit squad");
                        squad.Name = name;
                        squad.Enabled = enabled;
                    }

                    if (GUILayout.Button("◎", GUILayout.Width(24f))) Select(squad, frame: true);
                    if (GUILayout.Button("▲", GUILayout.Width(24f)) && index > 0) _deferred = () => Move(squads, index, index - 1);
                    if (GUILayout.Button("▼", GUILayout.Width(24f)) && index < squads.Count - 1) _deferred = () => Move(squads, index, index + 1);
                    if (GUILayout.Button("⧉", GUILayout.Width(24f))) _deferred = () => Duplicate(squads, index);
                    if (GUILayout.Button("✕", GUILayout.Width(24f))) _deferred = () => Remove(squads, index);
                }

                if (!open) return;
                DrawMembers(squad);
                DrawPlacement(squad);
            }
        }

        private void Fold(LineupSquad squad, bool fold)
        {
            if (fold) _folded.Add(squad);
            else _folded.Remove(squad);
        }

        private void DrawMembers(LineupSquad squad)
        {
            for (int i = 0; i < squad.Members.Count; i++)
            {
                LineupMember member = squad.Members[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    Choice choice = ChoiceOf(member.Prefab);
                    string label = choice != null ? choice.Path : member.Prefab != null ? member.Prefab.name : "— pick an enemy —";
                    if (EditorGUILayout.DropdownButton(new GUIContent(label), FocusType.Passive))
                        ShowEnemyMenu(picked => { Edit("Change enemy"); member.Prefab = picked; });

                    if (GUILayout.Button("−", GUILayout.Width(24f)) && member.Count > 1) { Edit("Fewer enemies"); member.Count--; }
                    EditorGUI.BeginChangeCheck();
                    int count = EditorGUILayout.IntField(member.Count, GUILayout.Width(36f));
                    if (EditorGUI.EndChangeCheck()) { Edit("Set enemy count"); member.Count = Mathf.Max(1, count); }
                    if (GUILayout.Button("+", GUILayout.Width(24f))) { Edit("More enemies"); member.Count++; }

                    if (GUILayout.Button("✕", GUILayout.Width(24f))) { int at = i; _deferred = () => { Edit("Remove enemy"); squad.Members.RemoveAt(at); }; }
                }
            }

            if (squad.Members.Count == 0)
                EditorGUILayout.HelpBox("Empty squad: nothing spawns.", MessageType.Warning);

            if (EditorGUILayout.DropdownButton(new GUIContent("+ Add enemy"), FocusType.Passive))
                ShowEnemyMenu(picked => AddEnemy(squad, picked));
        }

        private void DrawPlacement(LineupSquad squad)
        {
            EditorGUILayout.Space(2f);
            EditorGUI.BeginChangeCheck();
            Vector3 position = EditorGUILayout.Vector3Field("Spawn spot", squad.Position);
            float facing     = EditorGUILayout.Slider("Facing", squad.Facing, 0f, 360f);
            float spread     = EditorGUILayout.FloatField("Spread (m)", squad.Spread);
            string patrol    = DrawPatrol(squad.Patrol);
            if (!EditorGUI.EndChangeCheck()) return;

            Edit("Edit squad placement");
            squad.Position = position;
            squad.Facing   = facing;
            squad.Spread   = Mathf.Max(0.5f, spread);
            squad.Patrol   = patrol;
        }

        // A dropdown of the open scene's waypoint groups when there is one, else plain text.
        private string DrawPatrol(string current)
        {
            if (_spawner == null) return EditorGUILayout.TextField("Patrol route", current);

            List<string> groups = LineupLayout.PatrolGroups(_spawner.Waypoints);
            const string Hold = "(explore on its own)";
            var labels = new List<string> { Hold };
            labels.AddRange(groups);

            int selected = string.IsNullOrWhiteSpace(current) ? 0 : labels.IndexOf(current);
            if (selected < 0)
            {
                labels.Add($"{current} (missing)");
                selected = labels.Count - 1;
            }
            int picked = EditorGUILayout.Popup("Patrol route", selected, labels.ToArray());
            return picked == 0 ? "" : picked < groups.Count + 1 ? groups[picked - 1] : current;
        }

        private void ShowAddSquadMenu()
        {
            var menu = new GenericMenu();
            var teams = new List<Team>();
            foreach (Choice choice in _choices)
            {
                if (teams.Contains(choice.Team)) continue;
                teams.Add(choice.Team);
                Team team = choice.Team;
                menu.AddItem(new GUIContent($"{team} squad"), false, () => AddSquad(team));
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Empty squad"), false, () => AddSquad(Team.None));
            menu.ShowAsContext();
        }

        // A squad of two of the faction's first enemy, a step beyond the last squad, on the faction's route if the scene has one.
        private void AddSquad(Team team)
        {
            Edit("Add squad");
            var squad = new LineupSquad { Name = team == Team.None ? "Squad" : $"{team} squad" };

            Choice first = _choices.Find(c => c.Team == team && team != Team.None);
            if (first != null) squad.Members.Add(new LineupMember { Prefab = first.Prefab, Count = 2 });

            List<LineupSquad> squads = Lineup.Squads;
            if (squads.Count > 0) squad.Position = squads[squads.Count - 1].Position + Vector3.right * StepOutward;
            if (_spawner != null && LineupLayout.PatrolGroups(_spawner.Waypoints).Contains(team.ToString())) squad.Patrol = team.ToString();

            squads.Add(squad);
            Select(squad, frame: true);
        }

        // Count goes up when the squad already has that enemy, so a kind never takes two rows.
        private void AddEnemy(LineupSquad squad, GameObject prefab)
        {
            Edit("Add enemy");
            LineupMember existing = squad.Members.Find(m => m.Prefab == prefab);
            if (existing != null) existing.Count++;
            else squad.Members.Add(new LineupMember { Prefab = prefab, Count = 1 });
        }

        private void ShowEnemyMenu(System.Action<GameObject> picked)
        {
            var menu = new GenericMenu();
            foreach (Choice choice in _choices)
            {
                GameObject prefab = choice.Prefab;
                menu.AddItem(new GUIContent(choice.Path), false, () => { picked(prefab); EditorUtility.SetDirty(Lineup); Repaint(); });
            }
            menu.ShowAsContext();
        }

        private void Move(List<LineupSquad> squads, int from, int to)
        {
            Edit("Reorder squad");
            (squads[from], squads[to]) = (squads[to], squads[from]);
        }

        private void Duplicate(List<LineupSquad> squads, int index)
        {
            Edit("Duplicate squad");
            LineupSquad source = squads[index];
            var copy = new LineupSquad
            {
                Name = source.Name + " copy", Enabled = source.Enabled, Position = source.Position + Vector3.right * StepOutward,
                Facing = source.Facing, Spread = source.Spread, Patrol = source.Patrol,
            };
            foreach (LineupMember member in source.Members)
                copy.Members.Add(new LineupMember { Prefab = member.Prefab, Count = member.Count });
            squads.Insert(index + 1, copy);
        }

        private void Remove(List<LineupSquad> squads, int index)
        {
            Edit("Remove squad");
            if (_selected == squads[index]) _selected = null;
            squads.RemoveAt(index);
        }

        private void Select(LineupSquad squad, bool frame)
        {
            _selected = squad;
            if (frame && SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.Frame(new Bounds(squad.Position, Vector3.one * 12f), false);
            SceneView.RepaintAll();
            Repaint();
        }

        private void Edit(string label)
        {
            Undo.RecordObject(Lineup, label);
            EditorUtility.SetDirty(Lineup);
        }

        private Choice ChoiceOf(GameObject prefab) => prefab == null ? null : _choices.Find(c => c.Prefab == prefab);

        private Color ColorOf(LineupSquad squad)
        {
            foreach (LineupMember member in squad.Members)
            {
                Choice choice = ChoiceOf(member.Prefab);
                if (choice != null) return choice.Color;
            }
            return Neutral;
        }

        // Each squad as a ring with a dot per member, an arrow for the way it faces and its name; the
        // selected one can be dragged, the others picked by clicking their centre.
        private void DrawInScene(SceneView view)
        {
            SimulatorLineup lineup = Lineup;
            if (lineup == null) return;

            foreach (LineupSquad squad in lineup.Squads)
            {
                if (!squad.Enabled) continue;
                DrawSquadInScene(squad);
            }
            if (_selected != null && lineup.Squads.Contains(_selected)) DragSquad(_selected);
        }

        private void DrawSquadInScene(LineupSquad squad)
        {
            Handles.color = ColorOf(squad);
            int total = squad.Size;
            if (total > 1) Handles.DrawWireDisc(squad.Position, Vector3.up, squad.Spread);
            for (int i = 0; i < total; i++)
                Handles.DrawSolidDisc(LineupLayout.SpotOf(squad, i, total), Vector3.up, 0.4f);

            Handles.ArrowHandleCap(0, squad.Position, LineupLayout.FacingOf(squad), 2f, EventType.Repaint);
            Handles.Label(squad.Position + Vector3.up * 2.5f, $"{squad.Name} · {total}", EditorStyles.boldLabel);

            if (squad != _selected && Handles.Button(squad.Position, Quaternion.identity, HandleSize, HandleSize, Handles.SphereHandleCap))
                Select(squad, frame: false);
        }

        private void DragSquad(LineupSquad squad)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.PositionHandle(squad.Position, Quaternion.identity);
            if (!EditorGUI.EndChangeCheck()) return;

            Edit("Move squad");
            squad.Position = moved;
            Repaint();
        }
    }
}
