using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    public class ActionContext
    {
        public Vector3 Origin;
        public Vector3 Forward;
        public Vector3 Up;
        public Transform SourceRoot;

        public Vector3 TargetPoint;
        public bool HasTarget;

        public DamageSource Source;
        public LayerMask HitMask;

        // Set when a timeline delivers reflected damage: every damage event's authored
        // Damage is multiplied by this (so Damage 1 = all of it) and marked as reflected.
        public float DamageScale = 1f;
        public bool  Reflected;

        public readonly HashSet<IDamageable> SharedHits = new();
        public HashSet<IDamageable>[] EventHits;
        public int CurrentEventIndex;

        public bool DebugDraw;
        public float DebugDuration;

        public bool HitAnything;

        // Optional: the wielder's weapon socket, for events anchored to an animated weapon.
        public Transform Socket;
        public PoseTrail AimTrail;
        public PoseTrail SocketTrail;

        // The timeline frame being ticked, for events that animate over their frames.
        public int Frame;

        // Applies DamageScale and Reflected to a hit an event built from its own fields.
        public DamageInfo Prepare(DamageInfo info)
        {
            if (DamageScale != 1f) info = info.WithDamageScale(DamageScale);
            return Reflected ? info.AsReflected() : info;
        }

        // Records where the aim and socket are this tick; the runner calls it once per tick.
        public void TrackPoses()
        {
            AimTrail.Update(new Pose(Origin, Quaternion.LookRotation(Forward, Up)));
            if (Socket != null) SocketTrail.Update(new Pose(Socket.position, Socket.rotation));
        }

        public void Clear(int eventCount)
        {
            AimTrail.Reset();
            SocketTrail.Reset();
            SharedHits.Clear();
            HitAnything = false;

            if (EventHits == null || EventHits.Length < eventCount)
                EventHits = new HashSet<IDamageable>[eventCount];

            for (int i = 0; i < eventCount; i++)
            {
                if (EventHits[i] == null)
                    EventHits[i] = new HashSet<IDamageable>();
                else
                    EventHits[i].Clear();
            }
        }
    }
}
