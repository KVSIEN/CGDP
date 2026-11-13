using NUnit.Framework;
using CGD.Core;
using CGD.Enemies;

namespace CGD.Tests
{
    // When a Void horror's body blinks out while it glitches (HorrorPresence).
    public class GlitchScheduleTests
    {
        private const float Tick = 0.01f;

        private static GlitchSchedule Schedule() => new(new FloatRange(0.5f, 0.5f), new FloatRange(0.05f, 0.05f));

        [Test]
        public void NotGlitchingItNeverBlinks()
        {
            GlitchSchedule glitch = Schedule();
            for (int i = 0; i < 500; i++)
            {
                glitch.Tick(Tick, glitching: false);
                Assert.IsFalse(glitch.IsHidden);
            }
        }

        [Test]
        public void GlitchingItBlinksBrieflyEveryInterval()
        {
            GlitchSchedule glitch = Schedule();
            int blinks = 0, hiddenTicks = 0;
            for (int i = 0; i < 200; i++)   // 2 s
            {
                glitch.Tick(Tick, glitching: true);
                if (glitch.JustBlinked) blinks++;
                if (glitch.IsHidden) hiddenTicks++;
            }

            // One every half second, counted from when the last blink ended.
            Assert.That(blinks, Is.InRange(3, 4));
            Assert.LessOrEqual(hiddenTicks, blinks * 6, "each blink is only a few hundredths of a second");
        }

        [Test]
        public void ABurstBlinksInQuickSuccessionEvenWhenNotGlitching()
        {
            GlitchSchedule glitch = Schedule();
            glitch.Burst(3);
            int blinks = 0;
            for (int i = 0; i < 40; i++)   // 0.4 s: well before the next ordinary blink
            {
                glitch.Tick(Tick, glitching: false);
                if (glitch.JustBlinked) blinks++;
            }

            Assert.AreEqual(3, blinks);
        }

        [Test]
        public void ResetShowsItAgain()
        {
            GlitchSchedule glitch = Schedule();
            glitch.Burst(1);
            glitch.Tick(Tick, glitching: false);
            Assume.That(glitch.IsHidden);

            glitch.Reset();

            Assert.IsFalse(glitch.IsHidden);
        }
    }
}
