using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using CGD.Map;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Where enemies may hop low inner walls (VaultLinkPlanner): one link across the middle of
    // each run of low wall, a few tiles at most, broken where something stands against it, and
    // none over a full-height wall.
    public class VaultLinkPlannerTests
    {
        private const int Width = 10, Height = 10, WallX = 5;
        private const float Tile = 2f;

        // A room with a wall between columns 4 and 5 on the given rows.
        private static (LevelLayout Layout, RoomStructure Structure) Walled(float height, int fromRow, int toRow)
        {
            RoomStructure structure = Room(Width, Height, new Vector2Int(0, 0), new Vector2Int(Width - 1, 0));
            var edges = new List<PartitionEdge>();
            for (int y = fromRow; y <= toRow; y++)
                edges.Add(new PartitionEdge(new Vector2Int(WallX - 1, y), new Vector2Int(WallX, y), height, dividesZones: false));
            Assert.That(structure.TryAddPartition(edges), Is.True);

            var layout = new LevelLayout(Tile);
            var room = new LevelRoom(new MapGraph().AddNode(MapNodeType.Combat, Vector2.zero), structure.Footprint, null, 4f);
            room.AttachStructure(structure);
            layout.AddRoom(room);
            return (layout, structure);
        }

        [Test]
        public void ARunOfLowWallIsCrossedAtItsMiddle()
        {
            List<VaultLink> links = VaultLinkPlanner.Plan(Walled(1f, 3, 6).Layout);

            Assert.That(links, Has.Count.EqualTo(1));
            VaultLink link = links[0];
            Assert.That(link.From, Is.EqualTo(new Vector3((WallX - 0.5f) * Tile, 0f, 5f * Tile)));
            Assert.That(link.To,   Is.EqualTo(new Vector3((WallX + 0.5f) * Tile, 0f, 5f * Tile)));
            Assert.That(link.Width, Is.EqualTo(4 * Tile * VaultLinkPlanner.WidthShare).Within(1e-5f));
        }

        [Test]
        public void ALongWallGetsSeveralLinks()
        {
            List<VaultLink> links = VaultLinkPlanner.Plan(Walled(1f, 1, 8).Layout);
            Assert.That(links, Has.Count.EqualTo(2));
        }

        [Test]
        public void AFullHeightWallIsNeverHopped()
        {
            Assert.That(VaultLinkPlanner.Plan(Walled(0f, 3, 6).Layout), Is.Empty);
            Assert.That(VaultLinkPlanner.Plan(Walled(3f, 3, 6).Layout), Is.Empty);
        }

        [Test]
        public void SomethingStandingAgainstTheWallBreaksTheRun()
        {
            (LevelLayout layout, RoomStructure structure) = Walled(1f, 2, 7);
            Assert.That(structure.TryClaim(new[] { new Vector2Int(WallX, 4) }, RoomTileTags.Prop), Is.True);

            List<VaultLink> links = VaultLinkPlanner.Plan(layout);
            Assert.That(links, Has.Count.EqualTo(2), "rows 2–3 and 5–7");
            foreach (VaultLink link in links)
                Assert.That(Mathf.Abs(link.From.z - 4.5f * Tile), Is.GreaterThan(0.5f * Tile), "not across the blocked row");
        }
    }
}
