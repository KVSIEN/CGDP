using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Combat;

namespace CGD.Tests
{
    // A blast with a cover mask spares only what is wholly behind scenery: a body round a wall's
    // edge, over a low wall or behind another body is still caught.
    public class AreaHitsCoverTests
    {
        // Far from anything a scene might hold, so only these objects are queried.
        private static readonly Vector3 Origin = new(0f, 1000f, 0f);
        private const float Radius = 6f;
        private static readonly LayerMask Cover = 1;

        private readonly List<GameObject> _made = new();
        private readonly AreaHits _area = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _made) Object.DestroyImmediate(go);
            _made.Clear();
        }

        [Test]
        public void InTheOpen_IsHit()
        {
            Destructible target = Body(new Vector3(0f, 0f, 4f));
            Assert.That(Gather(Cover), Does.Contain(target));
        }

        [Test]
        public void WhollyBehindAWall_IsSpared()
        {
            Wall(new Vector3(0f, 0f, 2f), new Vector3(6f, 4f, 0.3f));
            Body(new Vector3(0f, 0f, 4f));
            Assert.That(Gather(Cover), Is.Empty);
        }

        [Test]
        public void WithoutACoverMask_ReachesThroughWalls()
        {
            Wall(new Vector3(0f, 0f, 2f), new Vector3(6f, 4f, 0.3f));
            Destructible target = Body(new Vector3(0f, 0f, 4f));
            Assert.That(Gather(default), Does.Contain(target));
        }

        [Test]
        public void ShoulderRoundAWallsEdge_IsHit()
        {
            // The wall ends at x = 0; the body's middle is just behind it, its side just past it.
            Wall(new Vector3(-1.5f, 0f, 2f), new Vector3(3f, 4f, 0.3f));
            Destructible target = Body(new Vector3(-0.2f, 0f, 4f));
            Assert.That(Gather(Cover), Does.Contain(target));
        }

        [Test]
        public void HeadOverALowWall_IsHit()
        {
            Wall(new Vector3(0f, -0.4f, 2f), new Vector3(6f, 1.2f, 0.3f));
            Destructible target = Body(new Vector3(0f, 0f, 4f));
            Assert.That(Gather(Cover), Does.Contain(target));
        }

        [Test]
        public void AnotherBody_IsNoCover()
        {
            Destructible front = Body(new Vector3(0f, 0f, 2f));
            Destructible back  = Body(new Vector3(0f, 0f, 4f));
            Assert.That(Gather(Cover), Is.EquivalentTo(new IDamageable[] { front, back }));
        }

        [Test]
        public void LooseBodyBehindAWall_IsNotReached()
        {
            Wall(new Vector3(0f, 0f, 2f), new Vector3(6f, 4f, 0.3f));
            GameObject crate = Make(PrimitiveType.Cube, new Vector3(0f, 0f, 4f), Vector3.one * 0.5f);
            crate.AddComponent<Rigidbody>();
            Physics.SyncTransforms();

            _area.Gather(Origin, Radius, ~0, Cover);
            Assert.That(_area.Reaches(crate.GetComponent<Collider>()), Is.False);
        }

        private List<IDamageable> Gather(LayerMask cover)
        {
            Physics.SyncTransforms();
            _area.Gather(Origin, Radius, ~0, cover);
            var targets = new List<IDamageable>();
            foreach (AreaHit hit in _area.Hits) targets.Add(hit.Target);
            return targets;
        }

        private Destructible Body(Vector3 offset) =>
            Make(PrimitiveType.Capsule, offset, Vector3.one).AddComponent<Destructible>();

        private void Wall(Vector3 offset, Vector3 size) => Make(PrimitiveType.Cube, offset, size);

        private GameObject Make(PrimitiveType type, Vector3 offset, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.transform.position   = Origin + offset;
            go.transform.localScale = scale;
            _made.Add(go);
            return go;
        }
    }
}
