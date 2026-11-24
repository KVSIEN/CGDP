using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Combat;

namespace CGD.Tests
{
    // A shield wall that stops every hit covers the bodies behind it from blasts and blows that come
    // at its front, not only from shots; from behind it, they land as usual.
    public class ShieldWallShelterTests
    {
        // Far from anything a scene might hold, so only these objects are queried.
        private static readonly Vector3 Origin = new(0f, 2000f, 0f);
        private const float Radius = 6f;

        private readonly List<GameObject> _made = new();
        private readonly AreaHits  _area   = new();
        private readonly ShapeHits _shapes = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _made) Object.DestroyImmediate(go);
            _made.Clear();
        }

        [Test]
        public void ABlastAtItsFront_SparesTheBodyBehind()
        {
            Wall(facing: Vector3.back, stopsEveryHit: true);
            Destructible behind = Body();
            Assert.That(Blast(), Has.No.Member(behind));
        }

        [Test]
        public void ABlastAtItsFront_StillHitsTheWall()
        {
            ShieldBarrier wall = Wall(facing: Vector3.back, stopsEveryHit: true);
            Body();
            Assert.That(Blast(), Does.Contain(wall));
        }

        [Test]
        public void ABlastFromBehindIt_HitsTheBody()
        {
            Wall(facing: Vector3.forward, stopsEveryHit: true);
            Destructible body = Body();
            Assert.That(Blast(), Does.Contain(body));
        }

        [Test]
        public void AWallThatOnlyStopsShots_SparesNoOne()
        {
            Wall(facing: Vector3.back, stopsEveryHit: false);
            Destructible body = Body();
            Assert.That(Blast(), Does.Contain(body));
        }

        [Test]
        public void ABlowAtItsFront_SparesTheBodyBehind()
        {
            Wall(facing: Vector3.back, stopsEveryHit: true);
            Destructible behind = Body();
            Physics.SyncTransforms();

            _shapes.Clear();
            _shapes.Sphere(Origin + new Vector3(0f, 0f, 2f), 3f, Origin, ~0, null);
            var struck = new List<IDamageable>();
            foreach (ShapeHit hit in _shapes.Hits) struck.Add(hit.Target);
            Assert.That(struck, Has.No.Member(behind));
        }

        private List<IDamageable> Blast()
        {
            Physics.SyncTransforms();
            _area.Gather(Origin, Radius, ~0);
            var targets = new List<IDamageable>();
            foreach (AreaHit hit in _area.Hits) targets.Add(hit.Target);
            return targets;
        }

        // A wall at z = 2 between the blast (at the origin) and the body (at z = 4). Its front is the
        // way it faces: facing back, toward the blast, the blast is at its front.
        private ShieldBarrier Wall(Vector3 facing, bool stopsEveryHit)
        {
            GameObject go = Make(PrimitiveType.Cube, new Vector3(0f, 0f, 2f), new Vector3(4f, 3f, 0.1f));
            go.layer = LayerMask.NameToLayer(ShieldBarrier.Layer);
            go.transform.rotation = Quaternion.LookRotation(facing);
            var barrier = go.AddComponent<ShieldBarrier>();
            if (stopsEveryHit) barrier.StopEveryHit();
            return barrier;
        }

        private Destructible Body() =>
            Make(PrimitiveType.Capsule, new Vector3(0f, 0f, 4f), Vector3.one).AddComponent<Destructible>();

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
