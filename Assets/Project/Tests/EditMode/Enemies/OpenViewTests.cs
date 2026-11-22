using NUnit.Framework;
using CGD.Enemies;

namespace CGD.Tests
{
    // Idle looks go where there is something to see, not into a wall a step away.
    public class OpenViewTests
    {
        private const float Open = 10f;
        private const float Wall = 1f;

        [Test]
        public void AllOpenKeepsTheWholeArc()
        {
            var reaches = new[] { Open, Open, Open, Open, Open };
            Assert.IsTrue(OpenView.Fit(reaches, 90f, 3.5f, out float centre, out float half));
            Assert.AreEqual(0f, centre, 0.01f);
            Assert.AreEqual(90f, half, 0.01f);
        }

        [Test]
        public void AWallOnOneSideShiftsTheLookToTheOther()
        {
            // -90, -45 blocked; 0, 45, 90 open.
            var reaches = new[] { Wall, Wall, Open, Open, Open };
            OpenView.Fit(reaches, 90f, 3.5f, out float centre, out float half);
            Assert.AreEqual(45f, centre, 0.01f);
            Assert.AreEqual(45f, half, 0.01f);
        }

        [Test]
        public void TheWidestOpeningWinsOverANarrowOneStraightAhead()
        {
            // -90 and -45 open, 0 blocked, 45 open, 90 blocked: the widest opening is on the left.
            var reaches = new[] { Open, Open, Wall, Open, Wall };
            OpenView.Fit(reaches, 90f, 3.5f, out float centre, out _);
            Assert.AreEqual(-67.5f, centre, 0.01f);
        }

        [Test]
        public void EqualOpeningsGoToTheOneNearestStraightAhead()
        {
            var reaches = new[] { Open, Wall, Open, Wall, Open };
            OpenView.Fit(reaches, 90f, 3.5f, out float centre, out float half);
            Assert.AreEqual(0f, centre, 0.01f);
            Assert.AreEqual(0f, half, 0.01f);
        }

        [Test]
        public void BoxedInItLooksTheWayItCanSeeFurthest()
        {
            var reaches = new[] { 1f, 2f, 0.5f, 3f, 1f };
            Assert.IsFalse(OpenView.Fit(reaches, 90f, 3.5f, out float centre, out float half));
            Assert.AreEqual(45f, centre, 0.01f);
            Assert.AreEqual(0f, half, 0.01f);
        }
    }
}
