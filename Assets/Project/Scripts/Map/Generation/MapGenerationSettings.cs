using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // A map style: the layouts a map can be built on and the content that fills it.
    // LevelBuilder and MapGraphAsset generate from one of these.
    //
    // Shape and content are separate assets so they mix freely — the same Treasure Hunt
    // content on a Hub or a Labyrinth — without copying either. With several layouts,
    // each seed picks one by weight: the style keeps its feel while its shape changes
    // from run to run.
    [CreateAssetMenu(fileName = "MapGenerationSettings", menuName = "CGD/Map/Map Generation Settings")]
    public class MapGenerationSettings : ScriptableObject
    {
        [Tooltip("Layouts this style is built on. Each map picks one by weight")]
        [SerializeField] private List<MapLayoutOption> _layouts = new();
        [SerializeField] private MapContentSettings _content;

        [Header("Run Modifiers")]
        [Tooltip("Twists a run can get, picked by weight")]
        [SerializeField] private List<MapRunModifier> _modifiers = new();
        [Tooltip("How many modifiers each map gets (never the same one twice)")]
        [SerializeField] private IntRange _modifierCount;
        [Tooltip("Most warnings (harder runs) one map can get")]
        [SerializeField, Min(0)] private int _maxWarnings = 2;
        [Tooltip("Most anomalies (twists) one map can get")]
        [SerializeField, Min(0)] private int _maxAnomalies = 1;

        [Header("Layout (editor only)")]
        [SerializeField] private Vector2 _nodeSpacing = new(220f, 110f);

        public IReadOnlyList<MapLayoutOption> Layouts => _layouts;
        public MapContentSettings Content     => _content;
        public Vector2            NodeSpacing => _nodeSpacing;
        public IReadOnlyList<MapRunModifier> Modifiers => _modifiers;
        public IntRange           ModifierCount => _modifierCount;

        public bool CanGenerate => _content != null && _layouts.Exists(o => o.Layout != null);

        // ModifierCount distinct modifiers, by weight, no more of each kind than its maximum.
        public List<MapRunModifier> PickModifiers(RandomStream random)
        {
            var pool   = _modifiers.FindAll(m => m != null && m.Weight > 0f);
            var picked = new List<MapRunModifier>();
            int count  = _modifierCount.Evaluate(random);
            int warnings = 0, anomalies = 0;

            while (picked.Count < count)
            {
                pool.RemoveAll(m => m.Kind == MapRunModifierKind.Warning ? warnings >= _maxWarnings : anomalies >= _maxAnomalies);
                if (pool.Count == 0) break;

                MapRunModifier modifier = random.PickWeighted(pool, m => m.Weight);
                picked.Add(modifier);
                pool.Remove(modifier);
                if (modifier.Kind == MapRunModifierKind.Warning) warnings++;
                else anomalies++;
            }
            return picked;
        }

        // By weight; uniformly when every weight is 0. Null when no layout is assigned.
        public MapLayoutSettings PickLayout(RandomStream random)
        {
            var options = _layouts.FindAll(o => o.Layout != null);
            if (options.Count == 0) return null;

            MapLayoutOption picked = random.PickWeighted(options, o => o.Weight) ?? random.Pick(options);
            return picked.Layout;
        }
    }
}
