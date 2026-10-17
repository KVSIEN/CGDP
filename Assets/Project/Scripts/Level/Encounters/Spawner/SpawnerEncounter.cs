using UnityEngine;
using CGD.Feedback;

namespace CGD.Level
{
    // Spawner: a structure stands in the middle of the room and keeps sending enemies
    // while the player is near. The doors stay open, so the player can fight it out or
    // leave; destroying it pays out a reward.
    public class SpawnerEncounter : RoomEncounter
    {
        protected override void OnBegin()
        {
            EnemySpawner prefab = Context.Settings.SpawnerPrefab;
            if (prefab == null) return;

            EnemySpawner structure = Instantiate(prefab, Context.Center, Quaternion.identity, Context.Level);
            structure.Initialize(Context.Spawner, Context.Player, Context.Settings);
            structure.Destroyed += () =>
            {
                Notify("Spawner destroyed", NotificationStyle.Success);
                SpawnReward(structure.transform.position);
            };
        }

        protected override void OnPlayerEntered() =>
            Notify("A spawner is sending reinforcements — destroy it", NotificationStyle.Danger);
    }
}
