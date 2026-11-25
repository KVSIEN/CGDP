using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // An enemy crouching behind low cover: its parts squash down toward its feet over a quarter
    // of a second, and stand straight back up.
    public class EnemyPostureTests
    {
        [Test]
        public void CrouchingSquashesTheBodyTowardItsFeetAndStandingRestoresIt()
        {
            var root = new GameObject("Enemy");
            try
            {
                var body = new GameObject("Body", typeof(BoxCollider));
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.75f, 0.1f);
                body.transform.localScale    = new Vector3(0.9f, 1.5f, 0.9f);
                var marker = new GameObject("Marker");
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition = Vector3.up * 2f;

                var posture = new EnemyPosture(root.transform);
                posture.Tick(crouch: true, 0.1f);
                Assert.That(posture.IsCrouching, Is.True);
                Assert.That(posture.HeightShare, Is.GreaterThan(EnemyPosture.CrouchedShare), "not all at once");

                posture.Tick(crouch: true, 1f);
                Assert.That(posture.HeightShare, Is.EqualTo(EnemyPosture.CrouchedShare).Within(1e-5f));
                Assert.That(body.transform.localPosition.y, Is.EqualTo(0.75f * EnemyPosture.CrouchedShare).Within(1e-5f));
                Assert.That(body.transform.localScale.y, Is.EqualTo(1.5f * EnemyPosture.CrouchedShare).Within(1e-5f));
                Assert.That(body.transform.localScale.x, Is.EqualTo(0.9f).Within(1e-5f), "only its height");
                Assert.That(marker.transform.localPosition.y, Is.EqualTo(2f), "a child with no mesh or collider is left alone");

                posture.Stand();
                Assert.That(posture.IsCrouching, Is.False);
                Assert.That(body.transform.localScale.y, Is.EqualTo(1.5f).Within(1e-5f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
