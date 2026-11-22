using NUnit.Framework;
using UnityEngine;
using CGD.Combat;

namespace CGD.Tests
{
    // A one-way barrier: shots at its front stop on it, shots fired from behind fly through.
    public class ShieldBarrierTests
    {
        private GameObject _barrier;
        private GameObject _target;

        // A barrier at z = 2 facing +z (the way its bearer faces), a target behind it at z = 5.
        [SetUp]
        public void SetUp()
        {
            _barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _barrier.transform.position   = new Vector3(0f, 0f, 2f);
            _barrier.transform.localScale = new Vector3(2f, 2f, 0.1f);
            _barrier.AddComponent<ShieldBarrier>();

            _target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _target.transform.position = new Vector3(0f, 0f, 5f);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_barrier);
            Object.DestroyImmediate(_target);
        }

        [Test]
        public void AShotFiredFromBehindPassesThrough()
        {
            Assert.IsTrue(ShotRay.Cast(Vector3.zero, Vector3.forward, out RaycastHit hit, 20f, ~0));
            Assert.AreEqual(_target, hit.collider.gameObject);
        }

        [Test]
        public void AShotAtItsFrontStopsOnIt()
        {
            // The target shelters behind the barrier at z = -4; fired at it from z = 10, the shot meets the front.
            _target.transform.position = new Vector3(0f, 0f, -4f);
            Physics.SyncTransforms();
            Assert.IsTrue(ShotRay.Cast(new Vector3(0f, 0f, 10f), Vector3.back, out RaycastHit hit, 20f, ~0));
            Assert.AreEqual(_barrier, hit.collider.gameObject);
        }

        [Test]
        public void WhatItStopsIsReported()
        {
            float stopped = 0f;
            var barrier = _barrier.GetComponent<ShieldBarrier>();
            barrier.Absorbed += amount => stopped += amount;
            Hitbox.ApplyHit(_barrier.GetComponent<Collider>(), new DamageInfo(30f), _barrier.transform.position);
            Assert.AreEqual(30f, stopped, 0.001f);
        }

        [Test]
        public void ADomeStopsAShotFromOutsideAndLetsOneFromInsideOut()
        {
            // The cube barrier is not wanted here: a dome at the origin shelters the target inside it.
            Object.DestroyImmediate(_barrier);
            _barrier = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _barrier.transform.localScale = Vector3.one * 8f;
            _barrier.AddComponent<ShieldBarrier>().MakeDome(4f);
            _target.transform.position = new Vector3(0f, 0f, 10f);
            Physics.SyncTransforms();

            // From outside, toward the dome: stops on it.
            Assert.IsTrue(ShotRay.Cast(new Vector3(0f, 0f, 20f), Vector3.back, out RaycastHit hit, 30f, ~0));
            Assert.AreEqual(_barrier, hit.collider.gameObject);

            // From its centre, out toward the target: flies on through.
            Assert.IsTrue(ShotRay.Cast(Vector3.zero, Vector3.forward, out hit, 30f, ~0));
            Assert.AreEqual(_target, hit.collider.gameObject);
        }
    }
}
