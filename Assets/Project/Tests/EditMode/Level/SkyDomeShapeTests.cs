using NUnit.Framework;
using UnityEngine;
using CGD.Level;

namespace CGD.Tests
{
    // The sky dome over an open-air room: its rim is the room's top edge, it rises to a crown
    // above the middle, and every face looks inward.
    public class SkyDomeShapeTests
    {
        private static readonly Vector2 Size = new(30f, 18f);
        private const float Height = 24f;

        private static SkyDomeShape Dome() => new(Size, Height, 48, 10);

        [Test]
        public void TheRimIsTheRoomsTopEdgeAndTheCrownIsAboveTheMiddle()
        {
            SkyDomeShape dome = Dome();
            for (int j = 0; j < dome.Segments; j++)
            {
                Vector3 rim = dome.Vertices[j];
                Assert.AreEqual(0f, rim.y, 1e-4f);
                Assert.LessOrEqual(Mathf.Abs(rim.x), Size.x * 0.5f + 1e-3f, $"vertex {j} sticks out past the room");
                Assert.LessOrEqual(Mathf.Abs(rim.z), Size.y * 0.5f + 1e-3f, $"vertex {j} sticks out past the room");
                bool onEdge = Mathf.Abs(Mathf.Abs(rim.x) - Size.x * 0.5f) < 1e-3f || Mathf.Abs(Mathf.Abs(rim.z) - Size.y * 0.5f) < 1e-3f;
                Assert.IsTrue(onEdge, $"vertex {j} is inside the rim, not on it");
            }
            Assert.AreEqual(new Vector3(0f, Height, 0f), dome.Vertices[dome.Crown]);
        }

        [Test]
        public void EveryFaceLooksInward()
        {
            SkyDomeShape dome = Dome();
            Vector3 inside = new(0f, Height * 0.2f, 0f);
            for (int t = 0; t < dome.Triangles.Length; t += 3)
            {
                Vector3 a = dome.Vertices[dome.Triangles[t]], b = dome.Vertices[dome.Triangles[t + 1]], c = dome.Vertices[dome.Triangles[t + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                Assert.Greater(normal.sqrMagnitude, 1e-8f, $"triangle {t / 3} has no area");
                Assert.Greater(Vector3.Dot(normal, inside - (a + b + c) / 3f), 0f, $"triangle {t / 3} faces outward");
            }
        }

        [Test]
        public void IndicesAreValidAndElevationRisesFromRimToCrown()
        {
            SkyDomeShape dome = Dome();
            foreach (int index in dome.Triangles) Assert.That(index, Is.InRange(0, dome.Vertices.Length - 1));

            for (int k = 1; k < dome.Rings; k++)
                Assert.Greater(dome.Elevation[k * dome.Segments], dome.Elevation[(k - 1) * dome.Segments]);
            Assert.AreEqual(0f, dome.Elevation[0]);
            Assert.AreEqual(1f, dome.Elevation[dome.Crown]);
        }
    }
}
