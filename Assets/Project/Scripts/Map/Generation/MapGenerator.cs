using System;
using System.Collections.Generic;
using CGD.Core;

namespace CGD.Map
{
    // Turns a MapGenerationSettings style and a seed into a MapGraph. Deterministic: the
    // same settings, seed, layer variants and pins always produce the same graph.
    //
    // Runs as separate passes — modifiers, structure, types, categories, intensity, factions — each with its
    // own seed layer, so a layer can be rerolled (SeedVariants.Reroll) without changing
    // the layers before it. Later layers read earlier results, so a new layout still
    // changes the types placed on it. Which of the style's layouts is used belongs to the
    // layout layer, so rerolling the layout can also change the map's shape style. The run
    // modifiers come first: they change the numbers every later pass works with. Sections
    // follow from depth alone and need no layer.
    public class MapGenerator
    {
        public const string ModifiersLayer = "modifiers";
        public const string LayoutLayer    = "layout";
        public const string TypesLayer     = "types";
        public const string CategoriesLayer = "categories";
        public const string IntensityLayer = "intensity";
        public const string FactionsLayer  = "factions";
        public const string TiersLayer     = "tiers";

        public static readonly IReadOnlyList<string> Layers =
            new[] { ModifiersLayer, LayoutLayer, TypesLayer, CategoriesLayer, IntensityLayer, TiersLayer, FactionsLayer };

        private readonly MapGenerationSettings _settings;

        public MapGenerator(MapGenerationSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (!settings.CanGenerate)
                throw new ArgumentException($"{settings.name} needs content and at least one layout.", nameof(settings));
            _settings = settings;
        }

        public MapGenerationResult Generate(Seed seed) => Generate(seed, null, Array.Empty<MapNodePin>());

        public MapGenerationResult Generate(Seed seed, SeedVariants variants, IReadOnlyList<MapNodePin> pins)
        {
            MapRunTuning tuning = PickModifiers(_settings, seed, variants);
            RandomStream layout = MapGenerationContext.StreamFor(seed, variants, LayoutLayer);
            var context = new MapGenerationContext(_settings.PickLayout(layout), _settings.Content, _settings.NodeSpacing,
                                                   seed, variants, tuning);

            // Rooms, extra links, gates and keys all belong to the layout layer.
            new MapLayoutBuilder(context, layout).Build();
            new MapLinkBuilder(context, layout).Build();
            new MapGatePlacer(context, layout).Place();
            new MapDirectDoorPlacer(context, layout).Place();
            context.DeriveSlots();

            var types = new MapTypeAssigner(context);
            types.Assign(pins);

            if (_settings.Content.Categories != null)
                new MapCategoryPainter(context, _settings.Content.Categories).Paint();

            new MapIntensityPainter(context).Paint();
            ApplyPinnedIntensity(context.Graph, pins, types.PinnedNodeIds);
            new MapTierPainter(context).Paint();

            MapFactionMix factionMix = PickFactionMix(_settings, seed, variants, tuning);
            new MapFactionPainter(context, factionMix).Paint();
            PaintSections(context);

            return new MapGenerationResult(context.Graph, context.Layout, types.PinnedNodeIds, context.Warnings, tuning.Modifiers, factionMix);
        }

        // The modifiers a seed gets — also how the editor and validator recover them for a
        // stored map without regenerating it.
        public static MapRunTuning PickModifiers(MapGenerationSettings settings, Seed seed, SeedVariants variants)
        {
            if (settings == null || settings.Modifiers.Count == 0) return MapRunTuning.None;
            return new MapRunTuning(settings.PickModifiers(MapGenerationContext.StreamFor(seed, variants, ModifiersLayer)));
        }

        // The faction mix a seed gets: a modifier's, else the content's roll. Drawn from its
        // own fork of the factions layer, so it rerolls with factions and can be recovered
        // for a stored map like the modifiers.
        public static MapFactionMix PickFactionMix(MapGenerationSettings settings, Seed seed, SeedVariants variants,
                                                   MapRunTuning tuning)
        {
            if (tuning?.FactionMixOverride != null) return tuning.FactionMixOverride;
            if (settings == null || settings.Content == null) return null;
            return settings.Content.PickFactionMix(MapGenerationContext.StreamFor(seed, variants, FactionsLayer).Fork("mix"));
        }

        private static void PaintSections(MapGenerationContext context)
        {
            foreach (MapSlot slot in context.Slots)
                if (context.Graph.TryGetNode(slot.NodeId, out MapNode node))
                    node.Section = context.Content.SectionAt(slot.Progress);
        }

        private static void ApplyPinnedIntensity(MapGraph graph, IReadOnlyList<MapNodePin> pins, IReadOnlyList<int> nodeIds)
        {
            for (int i = 0; i < pins.Count; i++)
                if (graph.TryGetNode(nodeIds[i], out MapNode node))
                    node.Intensity = pins[i].Intensity;
        }
    }
}
