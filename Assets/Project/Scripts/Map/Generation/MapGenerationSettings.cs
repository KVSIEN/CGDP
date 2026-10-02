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

        [Header("Layout (editor only)")]
        [SerializeField] private Vector2 _nodeSpacing = new(220f, 110f);

        public IReadOnlyList<MapLayoutOption> Layouts => _layouts;
        public MapContentSettings Content     => _content;
        public Vector2            NodeSpacing => _nodeSpacing;

        public bool CanGenerate => _content != null && _layouts.Exists(o => o.Layout != null);

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
