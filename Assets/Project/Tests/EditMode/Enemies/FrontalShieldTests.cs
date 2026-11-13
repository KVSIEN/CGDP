using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // Which hits a Frontal Shield takes: those from within its arc in front, whatever their height.
    public class FrontalShieldTests
    {
        [Test]
        public void HitsFromTheFrontAreCovered()
        {
            Assert.IsTrue(FrontalShield.Covers(Vector3.forward, Vector3.forward, 60f));
            Assert.IsTrue(FrontalShield.Covers(Vector3.forward, new Vector3(0.5f, 0f, 1f), 60f));
        }

        [Test]
        public void HitsFromTheSideOrBehindGetThrough()
        {
            Assert.IsFalse(FrontalShield.Covers(Vector3.forward, Vector3.right, 60f));
            Assert.IsFalse(FrontalShield.Covers(Vector3.forward, Vector3.back, 60f));
        }

        [Test]
        public void HeightDoesNotMatter()
        {
            Assert.IsTrue(FrontalShield.Covers(Vector3.forward, new Vector3(0f, 5f, 1f), 60f), "a shot from above, in front");
        }

        [Test]
        public void NoDirectionIsNotCovered()
        {
            Assert.IsFalse(FrontalShield.Covers(Vector3.forward, Vector3.zero, 60f));
        }
    }
}
