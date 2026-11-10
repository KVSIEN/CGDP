using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using CGD.UI;

namespace CGD.Tests
{
    public class HUDSkinTests
    {
        private readonly List<GameObject> _made = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _made)
                if (go != null) Object.DestroyImmediate(go);
            _made.Clear();
        }

        private RectTransform NewRect(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            _made.Add(go);
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private Image NewImage(string name, Transform parent)
        {
            RectTransform rect = NewRect(name, parent);
            return rect.gameObject.AddComponent<Image>();
        }

        private RectTransform NewSkin(Vector2 size)
        {
            RectTransform root = NewRect("StatsSkin");
            root.sizeDelta = size;
            RectTransform panel = NewRect("Panel", root);
            NewImage("HealthFill", panel);
            NewRect("NotAnImage", panel);
            return root;
        }

        [Test]
        public void FindsAPartAtAnyDepthByName()
        {
            RectTransform host = NewRect("Host");
            SkinParts skin = SkinParts.Create(NewSkin(new Vector2(200f, 40f)), host, "StatsHUD");

            Assert.IsNotNull(skin.GetImage("HealthFill"));
            Assert.IsTrue(skin.Accept());
        }

        [Test]
        public void TheSkinIsParentedAndStretchedUnderTheElement()
        {
            RectTransform host = NewRect("Host");
            SkinParts skin = SkinParts.Create(NewSkin(new Vector2(200f, 40f)), host, "StatsHUD");

            Assert.AreEqual(host, skin.Root.parent);
            Assert.AreEqual("StatsSkin", skin.Root.name);
            Assert.AreEqual(Vector2.zero, skin.Root.anchorMin);
            Assert.AreEqual(Vector2.one, skin.Root.anchorMax);
        }

        [Test]
        public void AMissingPartRejectsTheSkinAndRemovesIt()
        {
            RectTransform host = NewRect("Host");
            SkinParts skin = SkinParts.Create(NewSkin(new Vector2(200f, 40f)), host, "StatsHUD");

            Assert.IsNull(skin.GetImage("Nope"));
            LogAssert.Expect(LogType.Warning, new Regex("missing Nope"));
            Assert.IsFalse(skin.Accept());
            Assert.IsTrue(skin.Root == null);
        }

        [Test]
        public void APartWithTheWrongComponentCountsAsMissing()
        {
            RectTransform host = NewRect("Host");
            SkinParts skin = SkinParts.Create(NewSkin(new Vector2(200f, 40f)), host, "StatsHUD");

            Assert.IsNull(skin.GetImage("NotAnImage"));
            LogAssert.Expect(LogType.Warning, new Regex("NotAnImage"));
            Assert.IsFalse(skin.Accept());
        }

        [Test]
        public void TheDesignSizeIsUsedUnlessTheRootHasNone()
        {
            RectTransform host = NewRect("Host");
            var fallback = new Vector2(10f, 10f);

            SkinParts sized = SkinParts.Create(NewSkin(new Vector2(200f, 40f)), host, "StatsHUD");
            SkinParts stretched = SkinParts.Create(NewSkin(Vector2.zero), host, "StatsHUD");

            Assert.AreEqual(new Vector2(200f, 40f), sized.SizeOr(fallback));
            Assert.AreEqual(fallback, stretched.SizeOr(fallback));
        }

        [Test]
        public void AnOptionalPartMayBeMissing()
        {
            RectTransform host = NewRect("Host");
            SkinParts skin = SkinParts.Create(NewSkin(new Vector2(200f, 40f)), host, "AbilityHUD");

            Assert.IsNull(skin.GetImage("SurgeFill", optional: true));
            Assert.IsNotNull(skin.GetImage("HealthFill"));
            Assert.IsTrue(skin.Accept());
        }

        [Test]
        public void AnItemSkinIsLeftForTheElementToPlace()
        {
            RectTransform host = NewRect("Host");
            RectTransform prefab = NewSkin(new Vector2(56f, 56f));
            prefab.anchorMin = prefab.anchorMax = new Vector2(0.3f, 0.7f);

            SkinParts item = SkinParts.Create(prefab, host, "AbilityHUD", stretch: false);

            Assert.AreEqual(new Vector2(0.3f, 0.7f), item.Root.anchorMin);
            Assert.AreEqual(new Vector2(56f, 56f), item.SizeOr(Vector2.zero));
        }

        [Test]
        public void FillFromTheTopMovesTheNearAnchorOfAPlainImage()
        {
            Image overlay = NewImage("Overlay", NewRect("Host"));
            overlay.rectTransform.anchorMin = Vector2.zero;
            overlay.rectTransform.anchorMax = Vector2.one;

            UIFill.VerticalFromTop(overlay, 0.25f);

            Assert.AreEqual(0.75f, overlay.rectTransform.anchorMin.y, 0.0001f);
            Assert.AreEqual(1f, overlay.rectTransform.anchorMax.y);
        }

        [Test]
        public void FillMovesTheFarAnchorOfAPlainImage()
        {
            Image bar = NewImage("Bar", NewRect("Host"));
            UIFill.Horizontal(bar, 0.25f);
            UIFill.Vertical(bar, 0.5f);

            Assert.AreEqual(0.25f, bar.rectTransform.anchorMax.x, 0.0001f);
            Assert.AreEqual(0.5f, bar.rectTransform.anchorMax.y, 0.0001f);
        }

        [Test]
        public void FillSetsTheFillAmountOfAFilledImage()
        {
            Image bar = NewImage("Bar", NewRect("Host"));
            bar.type = Image.Type.Filled;
            UIFill.Horizontal(bar, 0.4f);

            Assert.AreEqual(0.4f, bar.fillAmount, 0.0001f);
            Assert.AreEqual(Vector2.zero, bar.rectTransform.anchorMax);
        }

        [Test]
        public void FillIsClampedToTheBar()
        {
            Image bar = NewImage("Bar", NewRect("Host"));
            UIFill.Horizontal(bar, 3f);
            Assert.AreEqual(1f, bar.rectTransform.anchorMax.x);
            UIFill.Horizontal(bar, -1f);
            Assert.AreEqual(0f, bar.rectTransform.anchorMax.x);
        }
    }
}
