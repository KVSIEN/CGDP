using NUnit.Framework;
using CGD.Enemies;

namespace CGD.Tests
{
    public class BarkRulesTests
    {
        private const float AnyGap = 1f, SameKindGap = 5f, SpeakerGap = 2f;

        private static BarkRules NewRules() => new(AnyGap, SameKindGap, SpeakerGap);
        private static float Fresh => float.NegativeInfinity;

        [Test]
        public void TheFirstBarkIsAllowed()
        {
            float speaker = Fresh;
            Assert.IsTrue(NewRules().TryClaim(BarkKind.Flanking, 10f, ref speaker));
            Assert.AreEqual(10f, speaker);
        }

        [Test]
        public void AnotherSpeakerWaitsForTheAnyBarkGap()
        {
            var rules = NewRules();
            float a = Fresh, b = Fresh;
            rules.TryClaim(BarkKind.Flanking, 10f, ref a);
            Assert.IsFalse(rules.TryClaim(BarkKind.Hunting, 10.5f, ref b));
            Assert.IsTrue(rules.TryClaim(BarkKind.Hunting, 11.1f, ref b));
        }

        [Test]
        public void UrgentBarksSkipTheAnyBarkGap()
        {
            var rules = NewRules();
            float a = Fresh, b = Fresh;
            rules.TryClaim(BarkKind.Flanking, 10f, ref a);
            Assert.IsTrue(rules.TryClaim(BarkKind.Spotted, 10.1f, ref b));
        }

        [Test]
        public void TheSameKindWaitsLongerEvenFromSomeoneElse()
        {
            var rules = NewRules();
            float a = Fresh, b = Fresh;
            rules.TryClaim(BarkKind.Grenade, 10f, ref a);
            Assert.IsFalse(rules.TryClaim(BarkKind.Grenade, 14f, ref b));
            Assert.IsTrue(rules.TryClaim(BarkKind.Grenade, 15.1f, ref b));
        }

        [Test]
        public void ASpeakerRestsBetweenBarks()
        {
            var rules = NewRules();
            float a = Fresh;
            rules.TryClaim(BarkKind.Flanking, 10f, ref a);
            Assert.IsFalse(rules.TryClaim(BarkKind.Spotted, 11f, ref a));
            Assert.IsTrue(rules.TryClaim(BarkKind.Spotted, 12.1f, ref a));
        }

        [Test]
        public void NoneIsNeverSaidAndResetForgets()
        {
            var rules = NewRules();
            float a = Fresh, b = Fresh;
            Assert.IsFalse(rules.TryClaim(BarkKind.None, 10f, ref a));
            rules.TryClaim(BarkKind.Grenade, 10f, ref a);
            rules.Reset();
            Assert.IsTrue(rules.TryClaim(BarkKind.Grenade, 10.5f, ref b));
        }
    }
}
