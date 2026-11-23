using NUnit.Framework;
using CGD.Combat;

namespace CGD.Tests
{
    public class CurseEffectTests
    {
        [Test]
        public void EachStackAddsItsShareScaledByPotencyUpToTheCap()
        {
            var curse = UnityEngine.ScriptableObject.CreateInstance<CurseEffect>();
            curse.SharePerStack = 0.1f;
            curse.MaxShare      = 0.5f;

            Assert.AreEqual(0.1f, curse.ShareOf(1, 1f), 0.0001f);
            Assert.AreEqual(0.3f, curse.ShareOf(3, 1f), 0.0001f);
            Assert.AreEqual(0.3f, curse.ShareOf(2, 1.5f), 0.0001f);
            Assert.AreEqual(0.5f, curse.ShareOf(5, 2f), 0.0001f);
            UnityEngine.Object.DestroyImmediate(curse);
        }
    }
}
