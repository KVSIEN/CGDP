using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Loot;
using CGD.Map;

namespace CGD.Level
{
    // Once a level's props are placed: leans each piece of searchable furniture toward what its
    // room's place keeps (RoomFunction.SearchLean), then hands the level's search finds out
    // across all of them (SearchFinds), weighted by the piece and by its room's place on the map
    // (SearchLootSettings), and tells every piece whether it holds one (LootContainer.PlanSearch).
    public static class SearchLootDistributor
    {
        public static void Distribute(LevelLayout layout, MapGraph graph, IReadOnlyDictionary<int, List<GameObject>> roomObjects,
                                      SearchLootSettings settings, Seed seed)
        {
            var analysis = new MapGraphAnalysis(graph);
            var containers = new List<LootContainer>();
            var spots = new List<SearchSpot>();
            var found = new List<LootContainer>();

            foreach (LevelRoom room in layout.Rooms.Values)
            {
                if (!roomObjects.TryGetValue(room.Node.Id, out List<GameObject> objects)) continue;
                float roomWeight = settings.RoomWeight(analysis.IsOnMainPath(room.Node.Id), analysis.IsBehindGate(room.Node.Id));
                WeightedLootFilter[] lean = room.Function != null ? room.Function.SearchLean : null;

                foreach (GameObject placed in objects)
                {
                    if (placed == null) continue;
                    placed.GetComponentsInChildren(true, found);
                    foreach (LootContainer container in found)
                    {
                        if (container.FindWeight <= 0f) continue;
                        if (lean != null && lean.Length > 0) container.Dropper.LeanTo(lean);
                        containers.Add(container);
                        spots.Add(new SearchSpot(container.FindWeight * roomWeight, container.FindsAdded));
                    }
                }
            }

            bool[] holds = SearchFinds.Choose(spots, seed.Stream());
            for (int i = 0; i < containers.Count; i++) containers[i].PlanSearch(holds[i]);
        }
    }
}
