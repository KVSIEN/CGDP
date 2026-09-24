using System.Collections.Generic;
using CGD.Core;
using UnityEngine;

namespace CGD.Map
{
    // A map graph kept as an asset, holding two copies:
    //   Generated — exactly what the generator produced (plus locked nodes). Never edited.
    //   Graph     — the working copy: the generated graph plus hand edits.
    // Keeping both means edits can be reverted and shown as edits, and regenerating
    // never has to guess which parts were authored.
    //
    // Locked nodes survive regeneration: each becomes a pin, so the new map keeps a
    // node of that type at a similar depth with the same intensity.
    //
    // Each generation layer (MapGenerator.Layers) has a reroll counter on top of the
    // seed, so one layer can be regenerated while the others stay exactly as they were.
    [CreateAssetMenu(fileName = "MapGraph", menuName = "CGD/Map/Map Graph")]
    public class MapGraphAsset : ScriptableObject
    {
        [SerializeField] private MapGenerationSettings _settings;
        [SerializeField] private int                   _seed;
        [SerializeField, HideInInspector] private SeedVariants _layerVariants = new();
        // The seed the layer variants belong to; they reset when generating from another.
        [SerializeField, HideInInspector] private int          _variantsSeed;
        [SerializeField, HideInInspector] private MapGraph     _generated = new();
        [SerializeField, HideInInspector] private MapGraph     _graph     = new();
        [SerializeField, HideInInspector] private List<int>    _lockedNodeIds      = new();
        [SerializeField, HideInInspector] private List<string> _generationWarnings = new();

        public MapGenerationSettings Settings => _settings;
        public int                   Seed     => _seed;

        public MapGraph Graph     => _graph;
        public MapGraph Generated => _generated;

        public IReadOnlyList<string> GenerationWarnings => _generationWarnings;

        public int LayerVariant(string layer) => _layerVariants.VariantOf(layer);

        public bool IsLocked(int nodeId) => _lockedNodeIds.Contains(nodeId);

        public void SetLocked(int nodeId, bool locked)
        {
            if (!locked)
            {
                _lockedNodeIds.Remove(nodeId);
                return;
            }

            if (!_lockedNodeIds.Contains(nodeId) && _graph.TryGetNode(nodeId, out _))
                _lockedNodeIds.Add(nodeId);
        }

        // Use this rather than Graph.RemoveNode so the node's lock goes with it.
        public void RemoveNode(int nodeId)
        {
            _graph.RemoveNode(nodeId);
            _lockedNodeIds.Remove(nodeId);
        }

        // Replaces both graphs. Hand edits are discarded; locked nodes are carried over.
        // A different seed starts every layer from scratch; the same seed keeps any
        // layer rerolls.
        public bool Regenerate(int seed)
        {
            if (_settings == null) return false;
            if (seed != _variantsSeed) _layerVariants.Clear();

            _seed         = seed;
            _variantsSeed = seed;
            Generate();
            return true;
        }

        // Regenerates with new numbers for one layer only (see MapGenerator.Layers).
        public bool RerollLayer(string layer)
        {
            if (_settings == null) return false;
            if (_seed != _variantsSeed) _layerVariants.Clear();

            _variantsSeed = _seed;
            _layerVariants.Reroll(layer);
            Generate();
            return true;
        }

        private void Generate()
        {
            List<MapNodePin> pins = BuildPins();
            MapGenerationResult result = new MapGenerator(_settings).Generate(CGD.Core.Seed.From(_seed), _layerVariants, pins);

            _generated = result.Graph;
            _graph     = result.Graph.Clone();

            _lockedNodeIds.Clear();
            foreach (int id in result.PinnedNodeIds)
                if (id != MapGenerationResult.PinNotPlaced)
                    _lockedNodeIds.Add(id);

            _generationWarnings = new List<string>(result.Warnings);
        }

        public void RevertEdits()
        {
            _graph = _generated.Clone();
            _lockedNodeIds.RemoveAll(id => !_graph.TryGetNode(id, out _));
        }

        private List<MapNodePin> BuildPins()
        {
            var pins     = new List<MapNodePin>();
            var analysis = new MapGraphAnalysis(_graph);

            foreach (int id in _lockedNodeIds)
            {
                if (!_graph.TryGetNode(id, out MapNode node)) continue;
                pins.Add(new MapNodePin(node.Type, analysis.Progress(id), analysis.IsOnMainPath(id), node.Intensity));
            }
            return pins;
        }
    }
}
