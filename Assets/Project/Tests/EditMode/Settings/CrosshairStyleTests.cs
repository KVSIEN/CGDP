using NUnit.Framework;
using UnityEngine;
using CGD.Settings;

namespace CGD.Tests
{
    public class CrosshairStyleTests
    {
        [Test]
        public void AllArmsAreShownByDefault()
        {
            var style = new CrosshairStyle();
            Assert.IsTrue(style.Has(CrosshairArms.Top));
            Assert.IsTrue(style.Has(CrosshairArms.Bottom));
            Assert.IsTrue(style.Has(CrosshairArms.Left));
            Assert.IsTrue(style.Has(CrosshairArms.Right));
        }

        [Test]
        public void ArmsCanBeLeftOut()
        {
            var style = new CrosshairStyle { Arms = CrosshairArms.All & ~CrosshairArms.Top };
            Assert.IsFalse(style.Has(CrosshairArms.Top));
            Assert.IsTrue(style.Has(CrosshairArms.Bottom));
            Assert.IsFalse(new CrosshairStyle { Arms = CrosshairArms.None }.Has(CrosshairArms.Left));
        }

        [Test]
        public void FillAndEdgeColoursTakeTheOpacity()
        {
            var style = new CrosshairStyle { Tint = new Color(0.1f, 0.2f, 0.3f), OutlineColor = Color.black, Opacity = 0.4f };
            Assert.AreEqual(0.4f, style.FillColor().a, 0.0001f);
            Assert.AreEqual(0.2f, style.FillColor().g, 0.0001f);
            Assert.AreEqual(0.4f, style.EdgeColor().a, 0.0001f);
        }

        [Test]
        public void OutlineSizeIsZeroWhileTheOutlineIsOff()
        {
            var style = new CrosshairStyle { OutlineThickness = 3f, Outline = false };
            Assert.AreEqual(0f, style.OutlineSize);
            style.Outline = true;
            Assert.AreEqual(3f, style.OutlineSize);
        }

        [Test]
        public void CloningSettingsDoesNotShareTheCrosshair()
        {
            var original = new SettingsData();
            SettingsData copy = original.Clone();
            copy.Crosshair.GapX = 20f;
            Assert.AreEqual(new CrosshairStyle().GapX, original.Crosshair.GapX);
        }

        [Test]
        public void TheCrosshairSurvivesTheJsonRoundTrip()
        {
            var data = new SettingsData();
            data.Crosshair.Arms = CrosshairArms.Horizontal;
            data.Crosshair.ShowDot = true;
            data.Crosshair.DotWidth = 7f;
            data.Crosshair.Tint = new Color(1f, 0.5f, 0f);

            SettingsData loaded = JsonUtility.FromJson<SettingsData>(JsonUtility.ToJson(data));

            Assert.AreEqual(CrosshairArms.Horizontal, loaded.Crosshair.Arms);
            Assert.IsTrue(loaded.Crosshair.ShowDot);
            Assert.AreEqual(7f, loaded.Crosshair.DotWidth);
            Assert.AreEqual(0.5f, loaded.Crosshair.Tint.g, 0.0001f);
        }

        [Test]
        public void AnOlderSaveWithoutACrosshairKeepsTheDefaultLook()
        {
            SettingsData loaded = JsonUtility.FromJson<SettingsData>("{\"MasterVolume\":0.5}");
            Assert.AreEqual(0.5f, loaded.MasterVolume);
            Assert.IsNotNull(loaded.Crosshair);
            Assert.AreEqual(CrosshairArms.All, loaded.Crosshair.Arms);
        }
    }
}
