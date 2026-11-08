using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.DevTools;

namespace CGD.Tests
{
    public class SpectatorTests
    {
        private const float Settle = 10f;   // seconds: long enough for any smoothing to arrive

        private static readonly Vector3    HomePosition = new(0f, 50f, -30f);
        private static readonly Quaternion HomeRotation = Quaternion.Euler(62f, 0f, 0f);

        private static SpectatorCameraController Make() => new(HomePosition, HomeRotation, panLimit: 30f);

        private static void Run(SpectatorCameraController camera, SpectatorMotion motion, Vector3? follow = null, float seconds = Settle) =>
            camera.Tick(motion, follow, moveSpeed: 10f, smoothing: 0f, deltaTime: seconds);

        // --- Bindings -------------------------------------------------------------------

        [Test]
        public void DefaultsBindEveryActionExactlyOnce()
        {
            List<SpectatorBinding> bindings = SpectatorInputSettings.DefaultBindings();
            foreach (SpectatorAction action in Enum.GetValues(typeof(SpectatorAction)))
                Assert.AreEqual(1, bindings.FindAll(b => b.Action == action).Count, action.ToString());
        }

        [Test]
        public void NoTwoDefaultsShareAControl()
        {
            var seen = new HashSet<string>();
            foreach (SpectatorBinding binding in SpectatorInputSettings.DefaultBindings())
            {
                if (!string.IsNullOrEmpty(binding.PrimaryPath))   Assert.IsTrue(seen.Add(binding.PrimaryPath),   binding.PrimaryPath);
                if (!string.IsNullOrEmpty(binding.SecondaryPath)) Assert.IsTrue(seen.Add(binding.SecondaryPath), binding.SecondaryPath);
            }
        }

        [Test]
        public void SettingsStaysOnEscape()
        {
            Assert.IsTrue(SpectatorInputSettings.IsFixed(SpectatorAction.Settings));
            SpectatorBinding settings = SpectatorInputSettings.DefaultBindings().Find(b => b.Action == SpectatorAction.Settings);
            Assert.AreEqual("<Keyboard>/escape", settings.PrimaryPath);
        }

        [Test]
        public void EnsureCompleteFillsInMissingActions()
        {
            var asset = ScriptableObject.CreateInstance<SpectatorInputSettings>();
            asset.Bindings = new List<SpectatorBinding>();
            asset.EnsureComplete();
            Assert.AreEqual(Enum.GetValues(typeof(SpectatorAction)).Length, asset.Bindings.Count);
            UnityEngine.Object.DestroyImmediate(asset);
        }

        // --- Camera ---------------------------------------------------------------------

        [Test]
        public void StartsOnThePlacedOverview()
        {
            SpectatorCameraController camera = Make();
            Assert.AreEqual(SpectatorCameraMode.Overview, camera.Mode);
            Assert.That(Vector3.Distance(HomePosition, camera.Position), Is.LessThan(0.01f));
        }

        [Test]
        public void OverviewPansButNeverFarFromTheArena()
        {
            SpectatorCameraController camera = Make();
            Run(camera, new SpectatorMotion(new Vector3(1f, 0f, 0f), Vector2.zero, 0, fast: true), seconds: 100f);

            Assert.AreEqual(SpectatorCameraMode.Overview, camera.Mode);
            Assert.That(camera.Position.x, Is.EqualTo(30f).Within(0.1f));
            Assert.That(camera.Position.y, Is.EqualTo(HomePosition.y).Within(0.1f));
        }

        [Test]
        public void OverviewZoomMovesAlongTheView()
        {
            SpectatorCameraController camera = Make();
            Run(camera, new SpectatorMotion(Vector3.zero, Vector2.zero, zoom: 3, fast: false));
            Assert.Less(camera.Position.y, HomePosition.y);
            Assert.That(Quaternion.Angle(HomeRotation, camera.Rotation), Is.LessThan(0.1f));
        }

        [Test]
        public void LookingFromTheOverviewFliesFree()
        {
            SpectatorCameraController camera = Make();
            Run(camera, new SpectatorMotion(Vector3.zero, new Vector2(10f, 0f), 0, false), seconds: 0.016f);
            Assert.AreEqual(SpectatorCameraMode.Free, camera.Mode);
        }

        [Test]
        public void FollowOrbitsAndLooksAtTheAgent()
        {
            SpectatorCameraController camera = Make();
            Vector3 agent = new(5f, 1.2f, 3f);
            camera.Follow();
            Run(camera, default, agent);

            Assert.AreEqual(SpectatorCameraMode.Follow, camera.Mode);
            Assert.That(Vector3.Distance(camera.Position, agent), Is.EqualTo(12f).Within(0.05f));
            Vector3 toAgent = (agent - camera.Position).normalized;
            Assert.That(Vector3.Angle(camera.Rotation * Vector3.forward, toAgent), Is.LessThan(0.5f));
            Assert.Greater(camera.Position.y, agent.y);   // looks down on it
        }

        [Test]
        public void FollowZoomStaysInRange()
        {
            SpectatorCameraController camera = Make();
            Vector3 agent = Vector3.up;
            camera.Follow();
            Run(camera, new SpectatorMotion(Vector3.zero, Vector2.zero, zoom: 100, fast: false), agent);
            Assert.That(Vector3.Distance(camera.Position, agent), Is.EqualTo(3f).Within(0.05f));
        }

        [Test]
        public void MovingWhileFollowingFliesFreeFromThere()
        {
            SpectatorCameraController camera = Make();
            camera.Follow();
            Run(camera, default, Vector3.up);
            Vector3 before = camera.Position;

            Run(camera, new SpectatorMotion(Vector3.forward, Vector2.zero, 0, false), Vector3.up, seconds: 0.016f);
            Assert.AreEqual(SpectatorCameraMode.Free, camera.Mode);
            Assert.That(Vector3.Distance(before, camera.Position), Is.LessThan(1f));   // no jump
        }

        [Test]
        public void FreeFlightGoesWhereItFaces()
        {
            SpectatorCameraController camera = Make();
            camera.FlyFree();
            Vector3 start = camera.Position;
            Vector3 forward = camera.Rotation * Vector3.forward;

            Run(camera, new SpectatorMotion(Vector3.forward, Vector2.zero, 0, false), seconds: 1f);
            Assert.That(Vector3.Angle(forward, camera.Position - start), Is.LessThan(1f));
        }

        [Test]
        public void OrbitAndAngleMaths()
        {
            SpectatorCameraController.Orbit(Vector3.zero, yaw: 0f, pitch: 0f, distance: 10f, out Vector3 position, out _);
            Assert.That(Vector3.Distance(new Vector3(0f, 0f, -10f), position), Is.LessThan(0.001f));

            Assert.AreEqual(-10f,  SpectatorCameraController.SignedAngle(350f), 1e-4f);
            Assert.AreEqual(-170f, SpectatorCameraController.SignedAngle(190f), 1e-4f);
            Assert.AreEqual(62f,   SpectatorCameraController.SignedAngle(62f),  1e-4f);
        }
    }
}
