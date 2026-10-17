using System.Collections.Generic;
using CGD.Combat;
using CGD.Feedback;

namespace CGD.Level
{
    // Lockdown: stepping in seals every doorway; the shutters open once every enemy in the
    // room is dead, and a reward cache drops. Enemies that wandered out before the seal are
    // shut outside and don't count; an empty room calls in a wave instead.
    public class LockdownEncounter : RoomEncounter
    {
        protected override void OnPlayerEntered()
        {
            var group = new EnemyGroup();
            group.AddRange(EnemiesInside(Context));
            if (group.IsCleared) group.AddRange(SpawnWave());
            if (group.IsCleared) return;

            Context.Seal.Seal();
            Notify("Lockdown — clear the room", NotificationStyle.Danger);
            group.Cleared += () =>
            {
                Context.Seal.Release();
                Notify("Lockdown lifted", NotificationStyle.Success);
                SpawnReward(Context.Center);
            };
        }

        public static List<HealthManager> EnemiesInside(EncounterContext context)
        {
            var inside = new List<HealthManager>();
            foreach (HealthManager enemy in context.RoomEnemies)
                if (enemy != null && !enemy.IsDead && context.Floor.Contains(enemy.transform.position))
                    inside.Add(enemy);
            return inside;
        }
    }
}
