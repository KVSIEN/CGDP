using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CGD.Algorithms;
using CGD.Core;

namespace CGD.Tests
{
    // Easing runs 0 to 1, curves meet their points, springs settle, Poisson points keep their
    // distance, L-systems rewrite and the turtle draws a line per F.
    public class CurveSamplingGrowthTests
    {
        [Test]
        public void EveryEasingRunsFromZeroToOne()
        {
            var curves = new Func<float, float>[]
            {
                Easing.Linear, Easing.SmoothStep, Easing.SmootherStep, Easing.InQuad, Easing.OutQuad, Easing.InOutQuad,
                Easing.InCubic, Easing.OutCubic, Easing.InOutCubic, Easing.InBack, Easing.OutBack, Easing.OutBounce, Easing.OutElastic,
            };
            foreach (Func<float, float> curve in curves)
            {
                Assert.That(curve(0f), Is.EqualTo(0f).Within(1e-4f), curve.Method.Name);
                Assert.That(curve(1f), Is.EqualTo(1f).Within(1e-4f), curve.Method.Name);
            }
        }

        [Test]
        public void CurvesMeetTheirPoints()
        {
            Vector3 start = Vector3.zero, end = new(4f, 0f, 0f);
            Assert.That(Vector3.Distance(BezierCurve.Cubic(start, new Vector3(1f, 3f, 0f), new Vector3(3f, 3f, 0f), end, 1f), end), Is.LessThan(1e-5f));
            Assert.That(BezierCurve.Quadratic(start, new Vector3(2f, 4f, 0f), end, 0.5f).y, Is.EqualTo(2f).Within(1e-5f));

            var points = new List<Vector3> { new(0f, 0f, 0f), new(2f, 1f, 0f), new(4f, 0f, 0f), new(5f, 3f, 0f) };
            for (int i = 0; i < points.Count; i++)
                Assert.That(Vector3.Distance(CatmullRomSpline.Evaluate(points, i / 3f), points[i]), Is.LessThan(1e-4f));
        }

        [Test]
        public void ACriticallyDampedSpringSettlesWithoutOvershootAndABouncyOneOvershoots()
        {
            var critical = new SpringDamper(frequency: 3f, dampingRatio: 1f);
            var bouncy = new SpringDamper(frequency: 3f, dampingRatio: 0.3f);
            float criticalPeak = 0f, bouncyPeak = 0f;
            for (int frame = 0; frame < 240; frame++)
            {
                criticalPeak = Mathf.Max(criticalPeak, critical.Update(1f, 1f / 60f));
                bouncyPeak = Mathf.Max(bouncyPeak, bouncy.Update(1f, 1f / 60f));
            }
            Assert.That(criticalPeak, Is.LessThanOrEqualTo(1.0001f));
            Assert.That(critical.Value, Is.EqualTo(1f).Within(0.01f));
            Assert.That(bouncyPeak, Is.GreaterThan(1.2f));
            Assert.That(bouncy.Value, Is.EqualTo(1f).Within(0.02f));
        }

        [Test]
        public void PoissonPointsKeepTheirDistance()
        {
            List<Vector2> points = PoissonDiskSampler.Sample(30f, 20f, 2f, new RandomStream(new Seed(1)));
            Assert.That(points.Count, Is.GreaterThan(80));
            for (int i = 0; i < points.Count; i++)
                for (int j = i + 1; j < points.Count; j++)
                    Assert.That(Vector2.Distance(points[i], points[j]), Is.GreaterThanOrEqualTo(2f - 1e-4f));
        }

        [Test]
        public void AnLSystemGrowsAndTheTurtleDrawsALinePerStep()
        {
            LSystem plant = new LSystem("X").AddRule('X', "F+[[X]-X]-F[-FX]+X").AddRule('F', "FF");
            Assert.That(plant.Generate(1), Is.EqualTo("F+[[X]-X]-F[-FX]+X"));

            string grown = plant.Generate(3);
            List<(Vector2 from, Vector2 to)> lines = Turtle2D.Draw(grown, 1f, 25f);
            Assert.That(lines.Count, Is.EqualTo(grown.Count(symbol => symbol == 'F')));
            Assert.That(Turtle2D.Draw("F+F+F+F", 1f, 90f, 0f)[3].to.magnitude, Is.LessThan(1e-4f), "a square closes");
        }
    }
}
