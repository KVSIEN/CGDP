using NUnit.Framework;
using CGD.Weapons;

namespace CGD.Tests
{
    public class StyleLeansTests
    {
        [Test]
        public void ScalesLeanTowardTheirSideOfTheRange()
        {
            StyleLeans leans = StyleLeans.From(new StyleScales(1.2f, 0.8f, 1f, 1f, 1f, 1f));

            Assert.AreEqual(0.3f, leans.Damage, 0.0001f);
            Assert.AreEqual(-0.3f, leans.FireRate, 0.0001f);
        }

        [Test]
        public void ScalesNearTheMiddleStayNeutral()
        {
            StyleLeans leans = StyleLeans.From(new StyleScales(1.04f, 0.96f, 1f, 1f, 1f, 1f));

            Assert.AreEqual(0f, leans.Damage);
            Assert.AreEqual(0f, leans.FireRate);
        }

        [Test]
        public void FarScalesAreCappedSoAnyRollStaysPossible()
        {
            StyleLeans leans = StyleLeans.From(new StyleScales(3f, 0.2f, 1f, 1f, 1f, 1f));

            Assert.AreEqual(0.6f, leans.Damage, 0.0001f);
            Assert.AreEqual(-0.6f, leans.FireRate, 0.0001f);
        }
    }
}
