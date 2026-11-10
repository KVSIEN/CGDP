using NUnit.Framework;
using CGD.Interaction;

namespace CGD.Tests
{
    public class LockProgressTests
    {
        [Test]
        public void OpensOnceEnoughDifferentSourcesReport()
        {
            var progress = new LockProgress(2);

            progress.Report(new object());
            Assert.IsFalse(progress.IsOpen);

            progress.Report(new object());
            Assert.IsTrue(progress.IsOpen);
        }

        [Test]
        public void SameSourceCountsOnce()
        {
            var progress = new LockProgress(2);
            var terminal = new object();

            Assert.IsTrue(progress.Report(terminal));
            Assert.IsFalse(progress.Report(terminal));

            Assert.AreEqual(1, progress.Met);
            Assert.IsFalse(progress.IsOpen);
        }

        [Test]
        public void NullSourceIsIgnored()
        {
            var progress = new LockProgress(1);

            Assert.IsFalse(progress.Report(null));
            Assert.IsFalse(progress.IsOpen);
        }

        [Test]
        public void RequiredIsAtLeastOne()
        {
            var progress = new LockProgress(0);

            Assert.AreEqual(1, progress.Required);
            Assert.IsFalse(progress.IsOpen);
        }

        [Test]
        public void OpenedFiresOnceAndStaysOpen()
        {
            var progress = new LockProgress(1);
            int opened = 0;
            progress.Opened += () => opened++;

            progress.Report(new object());
            progress.Report(new object());

            Assert.AreEqual(1, opened);
            Assert.AreEqual(1, progress.Met);
            Assert.IsTrue(progress.IsOpen);
        }

        [Test]
        public void LoweringRequiredOpensAnAlreadyMetLock()
        {
            var progress = new LockProgress(3);
            progress.Report(new object());
            progress.Report(new object());

            progress.SetRequired(2);

            Assert.IsTrue(progress.IsOpen);
        }
    }
}
