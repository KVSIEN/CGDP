using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Editor;
using CGD.Level;

namespace CGD.Tests
{
    // The sweep's report of the same rooms read two ways: how well a score tells the factions apart.
    public class RoomSweepReportTests
    {
        private static RoomSweepResult Room(string faction, float narrow) =>
            new("Place", faction, "seed", default, null, 0, 1, default, null, default, new RoomTraitReading(0f, narrow, 0, default, false), default);

        private static List<RoomSweepResult> Rooms(string faction, params float[] narrow)
        {
            var rooms = new List<RoomSweepResult>();
            foreach (float share in narrow) rooms.Add(Room(faction, share));
            return rooms;
        }

        private static float Separation(params List<RoomSweepResult>[] factions)
        {
            var all = new List<RoomSweepResult>();
            foreach (List<RoomSweepResult> faction in factions) all.AddRange(faction);
            return RoomSweepReport.Separation(all, r => r.Rows.NarrowShare);
        }

        [Test]
        public void FactionsFarApartAndEachTightAreSeparatedWell()
        {
            float far = Separation(Rooms("A", 0.10f, 0.12f, 0.14f, 0.11f), Rooms("B", 0.50f, 0.52f, 0.54f, 0.51f));
            float overlapping = Separation(Rooms("A", 0.1f, 0.3f, 0.5f, 0.2f), Rooms("B", 0.2f, 0.4f, 0.6f, 0.3f));

            Assert.That(far, Is.GreaterThan(10f));
            Assert.That(overlapping, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(far, Is.GreaterThan(overlapping));
        }

        [Test]
        public void NothingIsComparedWithOneFactionOrNoSpread()
        {
            Assert.That(Separation(Rooms("A", 0.1f, 0.2f, 0.3f)), Is.Negative, "one faction");
            Assert.That(Separation(Rooms("A", 0.2f, 0.2f), Rooms("B", 0.5f, 0.5f)), Is.Negative, "every room the same within a faction");
            Assert.That(RoomSweepReport.Separation(new List<RoomSweepResult>(), r => r.Rows.NarrowShare), Is.Negative);
        }
    }
}
