using NUnit.Framework;
using CGD.Enemies;

namespace CGD.Tests
{
    public class SquadRolesTests
    {
        private static SquadRole[] Assign(bool[] ranged, float[] distance, int suppressors, int assaulters)
        {
            var roles = new SquadRole[ranged.Length];
            SquadRoles.Assign(ranged, distance, suppressors, assaulters, roles);
            return roles;
        }

        [Test]
        public void NearestRangedSuppressesTheRestFlank()
        {
            var roles = Assign(new[] { true, true, true }, new[] { 20f, 8f, 14f }, suppressors: 1, assaulters: 0);
            CollectionAssert.AreEqual(new[] { SquadRole.Flank, SquadRole.Suppress, SquadRole.Flank }, roles);
        }

        [Test]
        public void NearestMeleeAssaultTheRestFlank()
        {
            var roles = Assign(new[] { false, false, false }, new[] { 5f, 15f, 3f }, suppressors: 0, assaulters: 2);
            CollectionAssert.AreEqual(new[] { SquadRole.Assault, SquadRole.Flank, SquadRole.Assault }, roles);
        }

        [Test]
        public void RangedAndMeleeAreCountedSeparately()
        {
            var roles = Assign(new[] { true, false, true, false }, new[] { 10f, 4f, 30f, 6f }, suppressors: 1, assaulters: 1);
            CollectionAssert.AreEqual(new[] { SquadRole.Suppress, SquadRole.Assault, SquadRole.Flank, SquadRole.Flank }, roles);
        }

        [Test]
        public void EveryoneFlanksWhenNoSlotsAreSet()
        {
            var roles = Assign(new[] { true, false }, new[] { 1f, 2f }, suppressors: 0, assaulters: 0);
            CollectionAssert.AreEqual(new[] { SquadRole.Flank, SquadRole.Flank }, roles);
        }

        [Test]
        public void HandlesAnEmptySquad()
        {
            Assert.IsEmpty(Assign(new bool[0], new float[0], 1, 2));
        }
    }
}
