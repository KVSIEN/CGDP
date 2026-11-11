using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // The faction doctrines: that an empty one changes nothing, that the morale rules do what
    // each faction's character calls for, and that the authored assets are wired to every
    // enemy of their faction and differ in the ways they are meant to.
    public class FactionDoctrineTests
    {
        private const string DataFolder = "Assets/Project/Data/Enemies/";

        private static FactionDoctrine Doctrine(string faction) =>
            AssetDatabase.LoadAssetAtPath<FactionDoctrine>(DataFolder + "Doctrine/" + faction + "Doctrine.asset");

        private static EnemyData Enemy(string name) => AssetDatabase.LoadAssetAtPath<EnemyData>(DataFolder + name + ".asset");

        // --- A plain doctrine ---------------------------------------------------------

        [Test]
        public void PlainDoctrineLeavesMoraleNumbersAlone()
        {
            var plain = ScriptableObject.CreateInstance<FactionDoctrine>();
            MoraleDoctrine morale = plain.Morale;

            Assert.AreEqual(0.3f, morale.AllyDownLoss(0.3f, 3, 1), 0.0001f);
            Assert.AreEqual(0.35f, morale.HurtLoss(0.35f), 0.0001f);
            Assert.AreEqual(0.04f, morale.RecoveryPerSecond(0.04f, kinNear: 5, inFight: true, targetWeak: true, hostilesSensed: 6, shieldBroken: true), 0.0001f);
            Assert.AreEqual(RetreatDestination.Cover, morale.RetreatTo);
            Assert.AreEqual(1f, morale.RetreatSpeedScale);
            Object.DestroyImmediate(plain);
        }

        [Test]
        public void PlainDoctrineIdlesAndGivesNothingAway()
        {
            var plain = ScriptableObject.CreateInstance<FactionDoctrine>();

            Assert.AreEqual(GazePattern.Scatter, plain.Idle.Gaze);
            Assert.AreEqual(0f, plain.Idle.WaypointDwell.Max);
            Assert.AreEqual(0f, plain.Idle.HaltsPerMinute);
            Assert.AreEqual(0f, plain.Idle.RoamRadius);
            Assert.AreEqual(0f, plain.Idle.MutterEvery.Max);
            Assert.AreEqual(TellKind.None, plain.Tells.NoticedKind);
            Assert.AreEqual(TellKind.None, plain.Tells.CommittedKind);
            Assert.AreEqual(0f, plain.Tells.Hesitation.Max);
            Assert.IsFalse(plain.Escalation.ReportsFallenKin);
            Assert.IsFalse(plain.Escalation.CallsForHelpWhenShaken);
            Object.DestroyImmediate(plain);
        }

        // --- Morale rules -------------------------------------------------------------

        [Test]
        public void SeeingAHigherRankFallShakesEveryFactionMore()
        {
            foreach (string faction in new[] { "Bio", "Tech", "Void" })
            {
                MoraleDoctrine morale = Doctrine(faction).Morale;
                float peer   = morale.AllyDownLoss(0.3f, 1, 1);
                float leader = morale.AllyDownLoss(0.3f, 3, 1);
                float prey   = morale.AllyDownLoss(0.3f, 1, 3);

                Assert.Greater(leader, peer, faction);
                Assert.Less(prey, peer, faction);
                Assert.Greater(prey, 0f, faction);
            }
        }

        [Test]
        public void BioFeelsAnAlphaFallMoreThanTechFeelsAnOfficer()
        {
            float bio  = Doctrine("Bio").Morale.AllyDownLoss(0.3f, 3, 1) / Doctrine("Bio").Morale.AllyDownLoss(0.3f, 1, 1);
            float tech = Doctrine("Tech").Morale.AllyDownLoss(0.3f, 3, 1) / Doctrine("Tech").Morale.AllyDownLoss(0.3f, 1, 1);
            Assert.Greater(bio, tech);
        }

        [Test]
        public void VoidCaresLeastWhenAnAllyFalls()
        {
            float voidLoss = Doctrine("Void").Morale.AllyDownLoss(0.3f, 1, 1);
            Assert.Less(voidLoss, Doctrine("Tech").Morale.AllyDownLoss(0.3f, 1, 1));
            Assert.Less(voidLoss, Doctrine("Bio").Morale.AllyDownLoss(0.3f, 1, 1));
        }

        [Test]
        public void BioRecoversNerveAgainstAWeakTargetAndTechDoesNot()
        {
            MoraleDoctrine bio  = Doctrine("Bio").Morale;
            MoraleDoctrine tech = Doctrine("Tech").Morale;

            float bioStrong = bio.RecoveryPerSecond(0.04f, 0, true, targetWeak: false, 1, false);
            float bioWeak   = bio.RecoveryPerSecond(0.04f, 0, true, targetWeak: true, 1, false);
            Assert.Greater(bioWeak, bioStrong);
            Assert.AreEqual(tech.RecoveryPerSecond(0.04f, 0, true, false, 1, false),
                            tech.RecoveryPerSecond(0.04f, 0, true, true, 1, false), 0.0001f);
        }

        [Test]
        public void TechKnowsTheOddsAndBioDoesNot()
        {
            MoraleDoctrine tech = Doctrine("Tech").Morale;
            MoraleDoctrine bio  = Doctrine("Bio").Morale;

            Assert.Less(tech.RecoveryPerSecond(0.04f, 0, true, false, hostilesSensed: 4, false),
                        tech.RecoveryPerSecond(0.04f, 0, true, false, hostilesSensed: 1, false));
            Assert.AreEqual(bio.RecoveryPerSecond(0.04f, 0, true, false, 4, false),
                            bio.RecoveryPerSecond(0.04f, 0, true, false, 1, false), 0.0001f);
        }

        [Test]
        public void KinInNumbersSteadyAHerdUpToAPoint()
        {
            MoraleDoctrine bio = Doctrine("Bio").Morale;

            float alone = bio.RecoveryPerSecond(0.04f, 0, false, false, 0, false);
            float three = bio.RecoveryPerSecond(0.04f, 3, false, false, 0, false);
            float ten   = bio.RecoveryPerSecond(0.04f, 10, false, false, 0, false);
            Assert.Greater(three, alone);
            Assert.AreEqual(three, ten, 0.0001f);
        }

        [Test]
        public void ABrokenShieldWearsVoidDownOnlyInAFight()
        {
            MoraleDoctrine voidMorale = Doctrine("Void").Morale;

            Assert.Less(voidMorale.RecoveryPerSecond(0.04f, 0, true, false, 1, shieldBroken: true),
                        voidMorale.RecoveryPerSecond(0.04f, 0, true, false, 1, shieldBroken: false));
            Assert.AreEqual(voidMorale.RecoveryPerSecond(0.04f, 0, false, false, 0, true),
                            voidMorale.RecoveryPerSecond(0.04f, 0, false, false, 0, false), 0.0001f);
        }

        [Test]
        public void VoidShakesOffAShockFasterThanTheOthers()
        {
            float voidRate = Doctrine("Void").Morale.RecoveryPerSecond(0.04f, 0, false, false, 0, false);
            Assert.Greater(voidRate, Doctrine("Tech").Morale.RecoveryPerSecond(0.04f, 0, false, false, 0, false));
            Assert.Greater(voidRate, Doctrine("Bio").Morale.RecoveryPerSecond(0.04f, 0, false, false, 0, false));
        }

        // --- The authored doctrines ---------------------------------------------------

        [Test]
        public void EveryEnemyOfAFactionFollowsItsDoctrine()
        {
            var factions = new (string Doctrine, string[] Enemies)[]
            {
                ("Bio",  new[] { "ScavengerEnemyData", "BioTier2EnemyData", "BioTier3EnemyData", "AITest/BioTestEnemyData" }),
                ("Tech", new[] { "MaintenanceBotEnemyData", "TechTier2EnemyData", "TechTier3EnemyData", "AITest/TechTestEnemyData" }),
                ("Void", new[] { "HuskEnemyData", "VoidTier2EnemyData", "VoidTier3EnemyData", "AITest/VoidTestEnemyData" }),
            };

            foreach (var faction in factions)
            {
                Assert.IsNotNull(Doctrine(faction.Doctrine), faction.Doctrine);
                foreach (string enemy in faction.Enemies)
                    Assert.AreEqual(Doctrine(faction.Doctrine), Enemy(enemy).Doctrine, enemy);
            }
        }

        [Test]
        public void FactionsIdleInDifferentWays()
        {
            Assert.AreEqual(GazePattern.Sweep,   Doctrine("Tech").Idle.Gaze);
            Assert.AreEqual(GazePattern.Scatter, Doctrine("Bio").Idle.Gaze);
            Assert.AreEqual(GazePattern.Fixate,  Doctrine("Void").Idle.Gaze);

            // Tech walks like a machine, the others do not.
            Assert.AreEqual(0f, Doctrine("Tech").Idle.PaceDrift);
            Assert.Greater(Doctrine("Bio").Idle.PaceDrift, 0f);
            Assert.Greater(Doctrine("Void").Idle.PaceDrift, 0f);

            // Only Bio wanders from its post; Tech stands its ground.
            Assert.Greater(Doctrine("Bio").Idle.RoamRadius, 0f);
            Assert.AreEqual(0f, Doctrine("Tech").Idle.RoamRadius);
            Assert.AreEqual(0f, Doctrine("Void").Idle.RoamRadius);

            // Void is the slowest walker and the longest starer.
            Assert.Less(Doctrine("Void").Idle.PatrolSpeedScale, Doctrine("Tech").Idle.PatrolSpeedScale);
            Assert.Greater(Doctrine("Void").Idle.LookIntervalScale, Doctrine("Tech").Idle.LookIntervalScale);
            Assert.Greater(Doctrine("Void").Idle.HaltDuration.Min, Doctrine("Bio").Idle.HaltDuration.Min);
        }

        [Test]
        public void FactionsGiveDifferentSignsAndHoldTheFirstBlowForDifferentTimes()
        {
            var tech = Doctrine("Tech").Tells;
            var bio  = Doctrine("Bio").Tells;
            var voidTells = Doctrine("Void").Tells;

            Assert.AreEqual(TellKind.Scan,   tech.NoticedKind);
            Assert.AreEqual(TellKind.Sniff,  bio.NoticedKind);
            Assert.AreEqual(TellKind.Stare,  voidTells.NoticedKind);
            Assert.AreEqual(TellKind.LockOn, tech.CommittedKind);
            Assert.AreEqual(TellKind.Roar,   bio.CommittedKind);

            // Bio pounces at once; Tech takes a beat to lock on; Void stares longest.
            Assert.AreEqual(0f, bio.Hesitation.Max);
            Assert.Greater(tech.Hesitation.Min, 0f);
            Assert.Greater(voidTells.Hesitation.Min, tech.Hesitation.Max);

            Assert.IsTrue(voidTells.NoticeSnapsToLead);
            Assert.IsFalse(tech.NoticeSnapsToLead);
        }

        [Test]
        public void FactionsFallBackAndRaiseTheAlarmInTheirOwnWay()
        {
            Assert.AreEqual(RetreatDestination.Kin,   Doctrine("Tech").Morale.RetreatTo);
            Assert.AreEqual(RetreatDestination.Den,   Doctrine("Bio").Morale.RetreatTo);
            Assert.AreEqual(RetreatDestination.Cover, Doctrine("Void").Morale.RetreatTo);
            Assert.Less(Doctrine("Void").Morale.RetreatSpeedScale, 1f);

            Assert.IsTrue(Doctrine("Tech").Escalation.ReportsFallenKin);
            Assert.IsTrue(Doctrine("Tech").Escalation.CallsForHelpWhenShaken);
            Assert.IsFalse(Doctrine("Bio").Escalation.ReportsFallenKin);
            Assert.Greater(Doctrine("Bio").Escalation.CalloutRadiusScale, 1f);
            Assert.Less(Doctrine("Void").Escalation.CalloutRadiusScale, 1f);
        }

        [Test]
        public void BioBreaksLaterThanTheOthers()
        {
            Assert.Less(Doctrine("Bio").Morale.RetreatBelowScale, Doctrine("Tech").Morale.RetreatBelowScale);
            Assert.Less(Doctrine("Bio").Morale.RetreatBelowScale, Doctrine("Void").Morale.RetreatBelowScale);
        }
    }
}
