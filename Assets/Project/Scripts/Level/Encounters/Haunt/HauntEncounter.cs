using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Enemies;
using CGD.Feedback;
using CGD.Loot;

namespace CGD.Level
{
    // Haunt: a Void entity stalks the room, and it can't be killed. Bring it down and it is only
    // banished for a while, then comes back somewhere away from the player. What holds it here
    // are the anchors standing round the room: break every one and it comes undone for good,
    // and the room pays out. The doors stay open, so the player can fight it off, slip past, or
    // end it. The entity drops nothing itself.
    public class HauntEncounter : RoomEncounter
    {
        private const int    ReturnSpots = 3;
        private const string EntityName  = "Void Entity";

        private readonly List<Vector3> _returnSpots = new();
        private HealthManager   _entity;
        private ObjectiveStatus _status;
        private int   _anchors;
        private int   _anchorsLeft;
        private float _returnsAt = -1f;
        private bool  _unbound;
        private bool  _shown;

        private EncounterSettings Settings => Context.Settings;

        protected override void OnBegin()
        {
            if (Settings.HauntEntityPrefab == null || Settings.HauntAnchorPrefab == null) return;

            for (int i = 0; i < Settings.HauntAnchors; i++) PlaceAnchor(Context.TakeSpot());
            for (int i = 0; i < ReturnSpots; i++) _returnSpots.Add(Context.TakeSpot());

            _status = new ObjectiveStatus("Break the anchors");
            Manifest(_returnSpots[0]);
            UpdateStatus();
        }

        protected override void OnPlayerEntered()
        {
            if (_status != null && !_unbound)
                Notify("Something here can't be killed — break what binds it", NotificationStyle.Danger);
        }

        protected override void Tick()
        {
            if (_status == null || _unbound) return;

            if (_returnsAt >= 0f && Time.time >= _returnsAt)
            {
                _returnsAt = -1f;
                Manifest(FarthestFromPlayer());
                Notify("It's back", NotificationStyle.Danger);
            }
            if (_returnsAt >= 0f) _status.TimeRemaining = _returnsAt - Time.time;
            ShowWhileInside();
        }

        private void OnDestroy() => ObjectiveFeed.Clear(_status);

        private void PlaceAnchor(Vector3 spot)
        {
            HealthManager anchor = Instantiate(Settings.HauntAnchorPrefab, spot, Quaternion.Euler(0f, Context.Random.Range(0, 360), 0f), Context.Level);
            anchor.OnDeath += BreakAnchor;
            _anchors++;
            _anchorsLeft++;
        }

        private void Manifest(Vector3 spot)
        {
            _entity = Context.Spawner.SpawnAt(Settings.HauntEntityPrefab, spot);
            if (_entity == null) return;

            if (_entity is EnemyHealth enemy) enemy.SetDisplayName(EntityName);
            if (_entity.TryGetComponent(out LootDropper dropper)) dropper.ScaleDrops(0f);
            _entity.OnDeath += Banish;
            _status.TimeRemaining = -1f;
            _status.Health        = _entity;
        }

        // Brought down: gone for now, back later.
        private void Banish()
        {
            Release(_entity);
            _entity = null;
            if (_unbound) return;

            _returnsAt = Time.time + Settings.HauntBanishSeconds;
            _status.Health = null;
            Notify("The entity is banished — for now", NotificationStyle.Warning);
        }

        private void BreakAnchor()
        {
            if (_unbound) return;

            _anchorsLeft--;
            UpdateStatus();
            if (_anchorsLeft > 0)
            {
                Notify($"An anchor breaks — {_anchorsLeft} left", NotificationStyle.Info);
                return;
            }
            Unbind();
        }

        // The last anchor is gone: the entity comes undone for good.
        private void Unbind()
        {
            _unbound   = true;
            _returnsAt = -1f;
            if (_entity != null)
            {
                HealthManager entity = _entity;
                Release(entity);
                _entity = null;
                entity.TakeDamage(new DamageInfo((entity.MaxHealth + entity.MaxShield + entity.MaxPlating) * 10f + 1000f, type: DamageType.True));
            }

            ObjectiveFeed.Clear(_status);
            _shown = false;
            Notify("The entity comes undone", NotificationStyle.Success);
            SpawnReward(Context.Center);
        }

        // Pooled: the next use of this enemy must not still be the entity.
        private void Release(HealthManager entity)
        {
            if (entity == null) return;
            entity.OnDeath -= Banish;
            if (entity is EnemyHealth enemy) enemy.SetDisplayName(null);
            _status.Health = null;
        }

        private void UpdateStatus() => _status.Detail = $"Anchors {_anchors - _anchorsLeft}/{_anchors}";

        // The panel is about this room: shown while the player is in it.
        private void ShowWhileInside()
        {
            bool inside = PlayerInside;
            if (inside == _shown) return;

            _shown = inside;
            if (inside) ObjectiveFeed.Show(_status);
            else        ObjectiveFeed.Clear(_status);
        }

        private Vector3 FarthestFromPlayer()
        {
            if (Context.Player == null) return _returnSpots[0];

            Vector3 player = Context.Player.position;
            Vector3 best   = _returnSpots[0];
            foreach (Vector3 spot in _returnSpots)
                if ((spot - player).sqrMagnitude > (best - player).sqrMagnitude) best = spot;
            return best;
        }
    }
}
