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

            Assert.AreEqual(0, plain.Idle.Beats.Length);
            Assert.AreEqual(0f, plain.Idle.WaypointStopChance);
            Assert.AreEqual(0f, plain.Idle.HaltsPerMinute);
            Assert.AreEqual(1f, plain.Idle.PatrolSpeedScale);
            Assert.AreEqual(0f, plain.Idle.PaceDrift);
            Assert.AreEqual(TellKind.None, plain.Tells.NoticedKind);
            Assert.AreEqual(TellKind.None, plain.Tells.CommittedKind);
            Assert.AreEqual(TellKind.None, plain.Tells.StandDownKind);
            Assert.AreEqual(0f, plain.Tells.Hesitation.Max);
            Assert.IsFalse(plain.Escalation.ReportsFallenKin);
            Assert.IsFalse(plain.Escalation.CallsForHelpWhenShaken);
            Object.DestroyImmediate(plain);
        }

        [Test]
        public void PlainDoctrineLooksNoFurtherAheadAndStaysPutForGrenades()
        {
            var plain = ScriptableObject.CreateInstance<FactionDoctrine>();

            Assert.AreEqual(0f, plain.Foresight.InterceptShare);
            Assert.AreEqual(0f, plain.Foresight.ZoneLeadShare);
            Assert.AreEqual(0f, plain.Foresight.CornerPause.Max);
            Assert.AreEqual(1f, plain.Foresight.StraightPace);
            Assert.AreEqual(0f, plain.Foresight.ListenStopsPerMinute);
            Assert.IsFalse(plain.Reflexes.AvoidsGrenades);
            Object.DestroyImmediate(plain);
        }

        [Test]
        public void PlainDoctrineLeavesAFightEndingAsItWas()
        {
            var plain = ScriptableObject.CreateInstance<FactionDoctrine>();
            AftermathDoctrine aftermath = plain.Aftermath;

            Assert.AreEqual(45f, aftermath.GiveUpCaution(45f), 0.0001f);
            Assert.AreEqual(0f, aftermath.VictoryCaution(45f), 0.0001f);
            Assert.AreEqual(1f, aftermath.VictoryPauseScale);
            Assert.AreEqual(GazePattern.Hold, aftermath.VictoryGaze);
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

        private static bool Uses(IdleBeat[] beats, GazePattern gaze)
        {
            foreach (IdleBeat beat in beats)
                if (beat.Gaze == gaze) return true;
            return false;
        }

        private static bool Walks(IdleBeat[] beats)
        {
            foreach (IdleBeat beat in beats)
                if (beat.WalkRadius > 0f) return true;
            return false;
        }

        private static float LongestBeat(IdleBeat[] beats)
        {
            float longest = 0f;
            foreach (IdleBeat beat in beats) longest = Mathf.Max(longest, beat.Duration.Max);
            return longest;
        }

        [Test]
        public void EveryFactionHasAnIdleRoutine()
        {
            foreach (string faction in new[] { "Bio", "Tech", "Void" })
            {
                IdleDoctrine idle = Doctrine(faction).Idle;
                Assert.Greater(idle.Beats.Length, 1, faction);
                foreach (IdleBeat beat in idle.Beats)
                {
                    Assert.Greater(beat.Duration.Min, 0f, faction + " " + beat.Gaze);
                    Assert.GreaterOrEqual(beat.Duration.Max, beat.Duration.Min, faction);
                    Assert.Greater(beat.Weight, 0f, faction);
                }
            }
        }

        [Test]
        public void TechIdlesByTheBookAndTheOthersByMood()
        {
            IdleDoctrine tech = Doctrine("Tech").Idle;

            Assert.AreEqual(IdleOrder.Cycle, tech.Order);
            Assert.AreEqual(IdleOrder.Weighted, Doctrine("Bio").Idle.Order);
            Assert.AreEqual(IdleOrder.Weighted, Doctrine("Void").Idle.Order);

            // Tech stops at every waypoint, never halts at random, never wanders, walks like a machine.
            Assert.AreEqual(1f, tech.WaypointStopChance);
            Assert.AreEqual(0f, tech.HaltsPerMinute);
            Assert.AreEqual(0f, tech.PaceDrift);
            Assert.IsFalse(Walks(tech.Beats));
            Assert.IsTrue(Uses(tech.Beats, GazePattern.Sweep));
        }

        [Test]
        public void BioIsRestlessAndWandersFromItsPost()
        {
            IdleDoctrine bio = Doctrine("Bio").Idle;

            Assert.IsTrue(Walks(bio.Beats));
            Assert.IsTrue(Uses(bio.Beats, GazePattern.Scatter));
            Assert.Greater(bio.PaceDrift, 0f);
            Assert.Greater(bio.HaltsPerMinute, Doctrine("Void").Idle.HaltsPerMinute);
            // Restless when on edge: its beats come quicker.
            Assert.Less(bio.EdgeBeatScale, 1f);
        }

        [Test]
        public void VoidStaresTurnsItsBackAndIsTheSlowestWalker()
        {
            IdleDoctrine voidIdle = Doctrine("Void").Idle;

            Assert.IsTrue(Uses(voidIdle.Beats, GazePattern.Fixate));
            Assert.IsTrue(Uses(voidIdle.Beats, GazePattern.Away));
            Assert.Less(voidIdle.PatrolSpeedScale, Doctrine("Tech").Idle.PatrolSpeedScale);
            Assert.Less(voidIdle.PatrolSpeedScale, Doctrine("Bio").Idle.PatrolSpeedScale);
            Assert.Greater(LongestBeat(voidIdle.Beats), LongestBeat(Doctrine("Tech").Idle.Beats));
            // Patient when on edge, not jumpy.
            Assert.Greater(voidIdle.EdgeBeatScale, 1f);
        }

        [Test]
        public void FactionsStayOnEdgeAfterAWonFightForDifferentTimes()
        {
            float tech = Doctrine("Tech").Aftermath.VictoryCaution(45f);
            float bio  = Doctrine("Bio").Aftermath.VictoryCaution(45f);
            float voidCaution = Doctrine("Void").Aftermath.VictoryCaution(45f);

            Assert.Greater(bio, 0f);
            Assert.Greater(tech, voidCaution);
            Assert.Greater(voidCaution, bio);
            // A won fight leaves less edge than a search that found nothing.
            foreach (string faction in new[] { "Bio", "Tech", "Void" })
                Assert.Less(Doctrine(faction).Aftermath.VictoryCaution(45f), Doctrine(faction).Aftermath.GiveUpCaution(45f), faction);
        }

        [Test]
        public void FactionsStandOverTheirKillInTheirOwnWay()
        {
            Assert.AreEqual(GazePattern.Sweep,   Doctrine("Tech").Aftermath.VictoryGaze);
            Assert.AreEqual(GazePattern.Scatter, Doctrine("Bio").Aftermath.VictoryGaze);
            Assert.AreEqual(GazePattern.Hold,    Doctrine("Void").Aftermath.VictoryGaze);
            Assert.Greater(Doctrine("Void").Aftermath.VictoryPauseScale, Doctrine("Bio").Aftermath.VictoryPauseScale);
            Assert.Greater(Doctrine("Bio").Aftermath.VictoryPauseScale, 1f);
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

            Assert.AreEqual(TellKind.AllClear, tech.StandDownKind);
            Assert.AreEqual(TellKind.Settle,   bio.StandDownKind);
            Assert.AreEqual(TellKind.Fade,     voidTells.StandDownKind);
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

        // --- Thinking ahead and reflexes ------------------------------------------------

        [Test]
        public void BioRunsToCutATargetOffAndTechBarelyBothers()
        {
            Assert.Greater(Doctrine("Bio").Foresight.InterceptShare, Doctrine("Void").Foresight.InterceptShare);
            Assert.Greater(Doctrine("Void").Foresight.InterceptShare, Doctrine("Tech").Foresight.InterceptShare);
            Assert.Greater(Doctrine("Bio").Foresight.MaxLead, Doctrine("Tech").Foresight.MaxLead);
        }

        [Test]
        public void EveryFactionAimsAheadOfAMovingTarget()
        {
            foreach (string faction in new[] { "Bio", "Tech", "Void" })
                Assert.Greater(Doctrine(faction).Foresight.ZoneLeadShare, 0f, faction);
            // Void expects you to keep running; Bio is the least exact.
            Assert.Greater(Doctrine("Void").Foresight.ZoneLeadShare, Doctrine("Tech").Foresight.ZoneLeadShare);
            Assert.Less(Doctrine("Bio").Foresight.ZoneLeadShare, Doctrine("Tech").Foresight.ZoneLeadShare);
        }

        [Test]
        public void FactionsReadTheRouteToWhatTheyHaveNotSeenDifferently()
        {
            ForesightDoctrine tech = Doctrine("Tech").Foresight;
            ForesightDoctrine bio  = Doctrine("Bio").Foresight;
            ForesightDoctrine voidFore = Doctrine("Void").Foresight;

            // Tech holds at corners; only Bio rushes the long stretches; only Void stops to listen.
            Assert.Greater(tech.CornerPause.Min, 0f);
            Assert.AreEqual(0f, bio.CornerPause.Max);
            Assert.AreEqual(0f, voidFore.CornerPause.Max);
            Assert.Greater(bio.StraightPace, 1f);
            Assert.AreEqual(1f, tech.StraightPace);
            Assert.Greater(voidFore.ListenStopsPerMinute, 0f);
            Assert.AreEqual(0f, tech.ListenStopsPerMinute);
            Assert.AreEqual(0f, bio.ListenStopsPerMinute);
        }

        [Test]
        public void EveryFactionGetsOutOfAGrenadesWayInItsOwnManner()
        {
            ReflexDoctrine tech = Doctrine("Tech").Reflexes;
            ReflexDoctrine bio  = Doctrine("Bio").Reflexes;
            ReflexDoctrine voidReflex = Doctrine("Void").Reflexes;

            foreach (ReflexDoctrine reflex in new[] { tech, bio, voidReflex })
                Assert.IsTrue(reflex.AvoidsGrenades);

            // Bio is off before it thinks, fastest and furthest; Void is the slowest to notice.
            Assert.Less(bio.ReactionDelay.Max, tech.ReactionDelay.Min);
            Assert.Greater(voidReflex.ReactionDelay.Min, tech.ReactionDelay.Max);
            Assert.Greater(bio.SpeedScale, tech.SpeedScale);
            Assert.Greater(bio.SafetyMargin, tech.SafetyMargin);

            // Tech looks for cover; Void accepts a graze and keeps its eyes on the grenade.
            Assert.IsTrue(tech.SeeksCover);
            Assert.IsFalse(bio.SeeksCover);
            Assert.Less(voidReflex.SafetyMargin, 0f);
            Assert.IsTrue(voidReflex.WatchesIt);
            Assert.AreEqual(BarkKind.Warning, tech.Bark);
        }

        // --- Encounters, overseers and temperament ---------------------------------------

        [Test]
        public void PlainDoctrineNeitherCoordinatesNorOversees()
        {
            var plain = ScriptableObject.CreateInstance<FactionDoctrine>();

            Assert.IsFalse(plain.Encounter.Coordinates);
            Assert.AreEqual(EncounterPosture.Steady, plain.Encounter.OnLeadLost);
            Assert.AreEqual(0f, plain.Encounter.SteppedUpSeconds);
            Assert.IsFalse(plain.Overseer.Oversees);
            Assert.AreEqual(1f, plain.Temperament.Aggression);
            Assert.AreEqual(1f, plain.Temperament.Caution);
            Assert.AreEqual(1f, plain.Temperament.Cunning);
            Object.DestroyImmediate(plain);
        }

        [Test]
        public void EveryFactionCoordinatesStepsUpAndRegroupsWhenItsLeadFalls()
        {
            foreach (string faction in new[] { "Bio", "Tech", "Void" })
            {
                EncounterDoctrine encounter = Doctrine(faction).Encounter;
                Assert.IsTrue(encounter.Coordinates, faction);
                Assert.AreEqual(EncounterPosture.Regroup, encounter.OnLeadLost, faction);
                Assert.Greater(encounter.RegroupSeconds, 0f, faction);
                Assert.Greater(encounter.SteppedUpSeconds, 0f, faction);
                Assert.Greater(encounter.SuccessorNerve, 0f, faction);
            }
            Assert.AreEqual(SuccessionRule.Rank,     Doctrine("Tech").Encounter.Succession);
            Assert.AreEqual(SuccessionRule.Strength, Doctrine("Bio").Encounter.Succession);
            Assert.AreEqual(SuccessionRule.Depth,    Doctrine("Void").Encounter.Succession);
        }

        private static bool Has(RoleSlot[] slots, SquadRole role)
        {
            foreach (RoleSlot slot in slots)
                if (slot.Role == role) return true;
            return false;
        }

        [Test]
        public void TechHandsOutFearStyleSlotsAndPinsDownWhileItReforms()
        {
            EncounterDoctrine tech = Doctrine("Tech").Encounter;
            RoleSlot[] steady  = tech.RolesFor(EncounterPosture.Steady);
            RoleSlot[] regroup = tech.RolesFor(EncounterPosture.Regroup);

            Assert.IsTrue(Has(steady, SquadRole.Assault));
            Assert.IsTrue(Has(steady, SquadRole.Suppress));
            Assert.IsTrue(Has(steady, SquadRole.Flank));
            Assert.IsTrue(steady[0].LeadFirst, "the lead takes point");
            // Regrouping: the new lead still bounds forward, every other gun pins the target down.
            Assert.IsTrue(regroup[0].LeadFirst);
            Assert.IsTrue(Has(regroup, SquadRole.Suppress));
            Assert.IsFalse(Has(regroup, SquadRole.Flank));
        }

        [Test]
        public void BioAndVoidLetTheirLeadPressOnWhileTheRestCircle()
        {
            foreach (string faction in new[] { "Bio", "Void" })
            {
                EncounterDoctrine encounter = Doctrine(faction).Encounter;
                RoleSlot[] steady = encounter.RolesFor(EncounterPosture.Steady);
                Assert.IsTrue(steady[0].LeadFirst, faction);
                Assert.AreEqual(SquadRole.Assault, steady[0].Role, faction);
                Assert.AreEqual(SquadRole.Flank, encounter.FallbackFor(EncounterPosture.Steady), faction);
            }
            // A Bio pack that lost its alpha circles round its new one; a coven that lost its
            // deepest falls back on each member's own judgement (and its caution).
            Assert.AreEqual(SquadRole.Flank, Doctrine("Bio").Encounter.FallbackFor(EncounterPosture.Regroup));
            Assert.AreEqual(0, Doctrine("Void").Encounter.RolesFor(EncounterPosture.Regroup).Length);
            Assert.AreEqual(SquadRole.None, Doctrine("Void").Encounter.FallbackFor(EncounterPosture.Regroup));
        }

        [Test]
        public void OverseersPaceEachFactionDifferently()
        {
            OverseerDoctrine tech = Doctrine("Tech").Overseer;
            OverseerDoctrine bio  = Doctrine("Bio").Overseer;
            OverseerDoctrine voidOverseer = Doctrine("Void").Overseer;

            foreach (OverseerDoctrine overseer in new[] { tech, bio, voidOverseer })
                Assert.IsTrue(overseer.Oversees);

            // Void breathes in and out the most (backs off soonest), Tech never lets up.
            Assert.IsTrue(voidOverseer.BacksOff);
            Assert.IsTrue(bio.BacksOff);
            Assert.IsFalse(tech.BacksOff);
            Assert.Less(voidOverseer.BackOffAt, bio.BackOffAt);
            // Void hints soonest; Tech's network gives the sharpest hints, to two members at once.
            Assert.Less(voidOverseer.HintAfterQuiet, tech.HintAfterQuiet);
            Assert.Less(tech.HintAccuracy(0), bio.HintAccuracy(0));
            Assert.Less(bio.HintAccuracy(0), voidOverseer.HintAccuracy(0));
            Assert.Greater(tech.HintMembers, voidOverseer.HintMembers);
            // Escalating sharpens every faction's hints.
            foreach (OverseerDoctrine overseer in new[] { tech, bio, voidOverseer })
                Assert.Less(overseer.HintAccuracy(3), overseer.HintAccuracy(0));
        }

        [Test]
        public void TemperamentsFitTheFactions()
        {
            TemperamentDoctrine tech = Doctrine("Tech").Temperament;
            TemperamentDoctrine bio  = Doctrine("Bio").Temperament;
            TemperamentDoctrine voidTemperament = Doctrine("Void").Temperament;

            Assert.Greater(bio.Aggression, tech.Aggression);
            Assert.Greater(tech.Caution, bio.Caution);
            Assert.Greater(voidTemperament.Cunning, tech.Cunning);
            Assert.Greater(voidTemperament.Cunning, bio.Cunning);
        }
    }
}
