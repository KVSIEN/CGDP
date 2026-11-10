using NUnit.Framework;
using UnityEngine;
using CGD.UI;

namespace CGD.Tests
{
    public class HUDThemeTests
    {
        [Test]
        public void WithAlphaKeepsTheColourAndChangesOnlyOpacity()
        {
            Color result = new Color(0.2f, 0.4f, 0.6f, 1f).WithAlpha(0.25f);
            Assert.AreEqual(0.2f, result.r, 0.0001f);
            Assert.AreEqual(0.4f, result.g, 0.0001f);
            Assert.AreEqual(0.6f, result.b, 0.0001f);
            Assert.AreEqual(0.25f, result.a, 0.0001f);
        }

        [Test]
        public void FallbackIsOneSharedInstanceWithReadableText()
        {
            Assert.AreSame(HUDTheme.Fallback, HUDTheme.Fallback);
            Assert.AreEqual(1f, HUDTheme.Fallback.TextPrimary.a);
            Assert.Greater(HUDTheme.Fallback.Window.a, HUDTheme.Fallback.Panel.a);
        }
    }
}
