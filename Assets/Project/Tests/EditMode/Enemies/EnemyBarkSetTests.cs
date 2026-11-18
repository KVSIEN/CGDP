using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // Sounds are optional: a bark line without a SoundBank is still a line, said silently, so
    // barks work before any sound exists. Kinds with no line at all are never said.
    public class EnemyBarkSetTests
    {
        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        private static BarkLine Line(BarkKind kind, float chance)
        {
            var line = new BarkLine();
            Set(line, "_kind", kind);
            Set(line, "_chance", chance);
            return line;
        }

        [Test]
        public void ALineWithoutASoundIsStillSaid()
        {
            var set = ScriptableObject.CreateInstance<EnemyBarkSet>();
            try
            {
                Set(set, "_lines", new[] { Line(BarkKind.Spotted, 0.6f) });

                Assert.IsTrue(set.TryGet(BarkKind.Spotted, out BarkLine line));
                Assert.IsNull(line.Sound, "no sound was given");
                Assert.AreEqual(0.6f, line.Chance, 1e-5f);
            }
            finally { UnityEngine.Object.DestroyImmediate(set); }
        }

        [Test]
        public void AKindWithNoLineIsNeverSaid()
        {
            var some = ScriptableObject.CreateInstance<EnemyBarkSet>();
            var none = ScriptableObject.CreateInstance<EnemyBarkSet>();
            try
            {
                Set(some, "_lines", new[] { Line(BarkKind.Spotted, 1f) });
                Set(none, "_lines", Array.Empty<BarkLine>());

                Assert.IsFalse(some.TryGet(BarkKind.Flanking, out _));
                Assert.IsFalse(none.TryGet(BarkKind.Spotted, out _));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(some);
                UnityEngine.Object.DestroyImmediate(none);
            }
        }
    }
}
