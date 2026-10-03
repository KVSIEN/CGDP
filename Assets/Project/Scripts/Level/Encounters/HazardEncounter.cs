using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Feedback;
using CGD.Interaction;

namespace CGD.Level
{
    // Hazard: a leak (fire, toxin, coolant, live wiring — one of the settings' status
    // effects) afflicts everyone on the room's floor, player and enemies alike, every tick
    // until the vent controls somewhere in the room are switched on. The floor glows in the
    // effect's colour while it runs.
    public class HazardEncounter : RoomEncounter
    {
        // Room geometry and props share the box with the characters, so leave plenty of room.
        private static readonly Collider[] Overlaps = new Collider[256];
        private const float ReachHeight = 2.5f;

        private readonly HashSet<StatusEffectController> _hit = new();
        private StatusEffect _effect;
        private GameObject   _overlay;
        private Material     _overlayMaterial;
        private Vector3      _boxCenter;
        private Vector3      _boxHalfExtents;
        private float        _nextTick;
        private bool         _active;

        protected override void OnBegin()
        {
            StatusEffect[] effects = Context.Settings.HazardEffects;
            if (effects.Length == 0) return;
            _effect = Context.Random.Pick(effects);
            if (_effect == null) return;

            _active = true;
            ComputeBox();
            _overlay = BuildOverlay();

            if (Context.Settings.TerminalPrefab == null) return;
            GameObject terminal = Instantiate(Context.Settings.TerminalPrefab, Context.TakeSpot(), Quaternion.identity, Context.Level);
            if (!terminal.TryGetComponent(out LockTerminal console)) return;
            console.SetLabel("Shut the vents");
            console.SwitchedOn += _ => Stop();
        }

        protected override void OnPlayerEntered()
        {
            if (_active) Notify($"{_effect.DisplayName} leak — find the vent controls", NotificationStyle.Warning);
        }

        protected override void Tick()
        {
            if (!_active || Time.time < _nextTick) return;
            _nextTick = Time.time + Context.Settings.HazardTick;

            int count = Physics.OverlapBoxNonAlloc(_boxCenter, _boxHalfExtents, Overlaps, Context.Level.rotation, ~0, QueryTriggerInteraction.Ignore);
            _hit.Clear();
            var hit = new DamageInfo(Context.Settings.HazardMagnitude);
            for (int i = 0; i < count; i++)
            {
                StatusEffectController target = Overlaps[i].GetComponentInParent<StatusEffectController>();
                if (target == null || !_hit.Add(target) || !Context.Floor.Contains(target.transform.position)) continue;
                target.Apply(_effect, hit);
            }
        }

        private void Stop()
        {
            if (!_active) return;
            _active = false;
            DestroyOverlay();
            Notify("Vents sealed — the leak stops", NotificationStyle.Success);
        }

        private void OnDestroy() => DestroyOverlay();

        private void DestroyOverlay()
        {
            if (_overlay != null) Destroy(_overlay);
            if (_overlayMaterial != null) Destroy(_overlayMaterial);
        }

        // A box over the room's floor tiles, in world space (the level may be rotated).
        private void ComputeBox()
        {
            RectInt bounds = Context.Room.Footprint.Bounds;
            float tile = Context.TileSize;
            Vector3 localCenter = new(bounds.center.x * tile, ReachHeight * 0.5f, bounds.center.y * tile);
            _boxCenter      = Context.Level.TransformPoint(localCenter);
            _boxHalfExtents = new Vector3(bounds.width * tile * 0.5f, ReachHeight * 0.5f, bounds.height * tile * 0.5f);
        }

        private GameObject BuildOverlay()
        {
            if (Context.Settings.HazardMaterial == null) return null;

            var builder = new BoxMeshBuilder();
            float tile = Context.TileSize;
            foreach (Vector2Int t in Context.Room.Footprint.Tiles)
                builder.AddBox(new Vector3((t.x + 0.5f) * tile, 0.03f, (t.y + 0.5f) * tile), new Vector3(tile, 0.02f, tile));

            var overlay = new GameObject("HazardOverlay", typeof(MeshFilter), typeof(MeshRenderer));
            overlay.transform.SetParent(Context.Level, false);
            overlay.GetComponent<MeshFilter>().sharedMesh = builder.ToMesh("HazardOverlay");

            var material = _overlayMaterial = new Material(Context.Settings.HazardMaterial);
            Color color = _effect.Color;
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 1.5f);
            overlay.GetComponent<MeshRenderer>().sharedMaterial = material;
            return overlay;
        }
    }
}
