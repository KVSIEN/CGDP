using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;

namespace CGD.Tests
{
    // How a faction's design turns into choices: which floor plans it favours and which dressing a
    // room type's generic one is swapped for.
    public class FactionDesignTests
    {
        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        [Test]
        public void ShapePreferencesScaleOnlyTheShapesTheyName()
        {
            var design = ScriptableObject.CreateInstance<FactionDesign>();
            var favoured = ScriptableObject.CreateInstance<RoomShape>();
            var shunned = ScriptableObject.CreateInstance<RoomShape>();
            var untouched = ScriptableObject.CreateInstance<RoomShape>();
            try
            {
                SetField(design, "_shapes", new List<ShapePreference>
                {
                    new() { Shape = favoured, Weight = 3f },
                    new() { Shape = shunned, Weight = 0f },
                });

                Assert.AreEqual(3f, design.ShapeWeight(favoured));
                Assert.AreEqual(0f, design.ShapeWeight(shunned));
                Assert.AreEqual(1f, design.ShapeWeight(untouched));
            }
            finally
            {
                Object.DestroyImmediate(design);
                Object.DestroyImmediate(favoured);
                Object.DestroyImmediate(shunned);
                Object.DestroyImmediate(untouched);
            }
        }

        [Test]
        public void ADressingIsSwappedForTheFactionsVersionOfIt()
        {
            var design = ScriptableObject.CreateInstance<FactionDesign>();
            var generic = ScriptableObject.CreateInstance<RoomDressing>();
            var themed = ScriptableObject.CreateInstance<RoomDressing>();
            var other = ScriptableObject.CreateInstance<RoomDressing>();
            try
            {
                SetField(design, "_swaps", new List<DressingSwap> { new() { Original = generic, Replacement = themed } });

                Assert.AreSame(themed, design.Dress(generic));
                Assert.AreSame(other, design.Dress(other), "a dressing with no swap stays as it is");
            }
            finally
            {
                Object.DestroyImmediate(design);
                Object.DestroyImmediate(generic);
                Object.DestroyImmediate(themed);
                Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void ASwapWithNoReplacementChangesNothing()
        {
            var design = ScriptableObject.CreateInstance<FactionDesign>();
            var generic = ScriptableObject.CreateInstance<RoomDressing>();
            try
            {
                SetField(design, "_swaps", new List<DressingSwap> { new() { Original = generic, Replacement = null } });

                Assert.AreSame(generic, design.Dress(generic));
            }
            finally
            {
                Object.DestroyImmediate(design);
                Object.DestroyImmediate(generic);
            }
        }
    }
}
