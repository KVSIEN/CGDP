using NUnit.Framework;
using CGD.Director;

namespace CGD.Tests
{
    public class MusicLayerTests
    {
        [Test]
        public void FadesInAcrossItsIntensityRange()
        {
            var layer = new MusicLayer(0.2f, 0.6f, volume: 0.5f);

            Assert.AreEqual(0f,    layer.TargetVolume(PacingPhase.BuildUp, 0.1f));
            Assert.AreEqual(0.25f, layer.TargetVolume(PacingPhase.BuildUp, 0.4f), 0.0001f);
            Assert.AreEqual(0.5f,  layer.TargetVolume(PacingPhase.BuildUp, 0.9f), 0.0001f);
        }

        [Test]
        public void ABedWithNoRangeIsAlwaysOn() =>
            Assert.AreEqual(1f, new MusicLayer(0f, 0f).TargetVolume(PacingPhase.Relax, 0f));

        [Test]
        public void PeakLayersOnlyPlayAtThePeak()
        {
            var layer = new MusicLayer(0f, 0f, peakOnly: true);

            Assert.AreEqual(0f, layer.TargetVolume(PacingPhase.BuildUp, 1f));
            Assert.AreEqual(1f, layer.TargetVolume(PacingPhase.SustainPeak, 1f));
        }

        [Test]
        public void CalmSilencesFightLayers()
        {
            var layer = new MusicLayer(0.3f, 0.7f, silentInCalm: true);

            Assert.AreEqual(0f, layer.TargetVolume(PacingPhase.Relax, 1f));
            Assert.AreEqual(1f, layer.TargetVolume(PacingPhase.PeakFade, 1f));
        }
    }
}
