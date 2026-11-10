using NUnit.Framework;
using CGD.Weapons;

namespace CGD.Tests
{
    public class ComboStateTests
    {
        [Test]
        public void ClosedComboStartsAtFirstStep()
        {
            var combo = new ComboState();
            Assert.AreEqual(0, combo.StepAt(0f));
        }

        [Test]
        public void SwingingKeepsNextStepOpenUntilReleased()
        {
            var combo = new ComboState();
            combo.Begin(0, 3);

            Assert.AreEqual(1, combo.StepAt(100f));
        }

        [Test]
        public void ReleaseGivesTheNextStepALimitedWindow()
        {
            var combo = new ComboState();
            combo.Begin(0, 3);
            combo.Release(10f, 1f);

            Assert.AreEqual(1, combo.StepAt(10.5f));
            Assert.AreEqual(0, combo.StepAt(11.5f));
        }

        [Test]
        public void LastStepWrapsToTheFirst()
        {
            var combo = new ComboState();
            combo.Begin(2, 3);

            Assert.AreEqual(0, combo.StepAt(0f));
        }

        [Test]
        public void ExtendOnlyLengthensAnOpenCombo()
        {
            var combo = new ComboState();
            combo.Extend(5f, 2f);
            Assert.IsFalse(combo.IsOpen(5f), "a closed combo stays closed");

            combo.Begin(0, 3);
            combo.Release(5f, 1f);
            combo.Extend(5f, 3f);
            Assert.IsTrue(combo.IsOpen(7.5f));

            combo.Extend(5f, 0.1f);
            Assert.IsTrue(combo.IsOpen(7.5f), "a shorter extension never shrinks the window");
        }

        [Test]
        public void ResetClosesTheCombo()
        {
            var combo = new ComboState();
            combo.Begin(0, 3);
            combo.Reset();

            Assert.IsFalse(combo.IsOpen(0f));
            Assert.AreEqual(0, combo.StepAt(0f));
        }
    }
}
