using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Combat;
using CGD.Factions;

namespace CGD.Tests
{
    // Every faction is hostile to every other; the relations only say who goes looking for
    // which fight, and whether calm rivals leave each other be.
    public class FactionRelationsTests
    {
        private FactionRelations _relations;

        [SetUp]
        public void Create()
        {
            _relations = ScriptableObject.CreateInstance<FactionRelations>();
            var serialized = new SerializedObject(_relations);
            SerializedProperty rows = serialized.FindProperty("_relations");
            rows.arraySize = 2;
            SetRow(rows.GetArrayElementAtIndex(0), Team.Bio, Team.Tech, FactionAttitude.Hunt);
            SetRow(rows.GetArrayElementAtIndex(1), Team.Bio, Team.Void, FactionAttitude.Flee);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void Destroy() => Object.DestroyImmediate(_relations);

        private static void SetRow(SerializedProperty row, Team from, Team toward, FactionAttitude attitude)
        {
            row.FindPropertyRelative("From").enumValueIndex     = (int)from;
            row.FindPropertyRelative("Toward").enumValueIndex   = (int)toward;
            row.FindPropertyRelative("Attitude").enumValueIndex = (int)attitude;
        }

        [Test]
        public void Rows_AreOneWay()
        {
            Assert.That(_relations.AttitudeOf(Team.Bio, Team.Tech), Is.EqualTo(FactionAttitude.Hunt));
            Assert.That(_relations.AttitudeOf(Team.Bio, Team.Void), Is.EqualTo(FactionAttitude.Flee));
            Assert.That(_relations.AttitudeOf(Team.Tech, Team.Bio), Is.EqualTo(FactionAttitude.Fight));
        }

        [Test]
        public void UnlistedPairs_UseTheDefault()
        {
            Assert.That(_relations.AttitudeOf(Team.Void, Team.Player), Is.EqualTo(FactionAttitude.Fight));
        }

        [Test]
        public void Truce_CoversRivalsButNeverThePlayer()
        {
            Assert.That(_relations.KeepsTruce(Team.Bio, Team.Tech),   Is.True);
            Assert.That(_relations.KeepsTruce(Team.Bio, Team.Player), Is.False);
            Assert.That(_relations.KeepsTruce(Team.Bio, Team.Bio),    Is.False);
        }

        [Test]
        public void Truce_NeverCoversPrey()
        {
            Assert.That(_relations.KeepsTruce(Team.Bio, Team.Prey), Is.False);
            Assert.That(_relations.KeepsTruce(Team.Prey, Team.Bio), Is.False);
        }

        [Test]
        public void Truce_CanBeSwitchedOff()
        {
            var serialized = new SerializedObject(_relations);
            serialized.FindProperty("_truceWhileCalm").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(_relations.KeepsTruce(Team.Bio, Team.Tech), Is.False);
        }

        [Test]
        public void AvoidedAndFearedHostiles_AreNeverChosenUnprovoked()
        {
            Assert.That(_relations.TargetWeight(FactionAttitude.Hunt),  Is.GreaterThan(1f));
            Assert.That(_relations.TargetWeight(FactionAttitude.Fight), Is.EqualTo(1f));
            Assert.That(_relations.TargetWeight(FactionAttitude.Avoid), Is.Zero);
            Assert.That(_relations.TargetWeight(FactionAttitude.Flee),  Is.Zero);
        }
    }
}
