using UnityEngine;

namespace CGD.Enemies
{
    // The rules for reading a target's body language from where an observer stands — pure, so
    // they can be tested. Fed one reading at a time (TargetReadings does, while it senses it):
    //   exposed   its back is turned to the observer, or it is reloading
    //   watching  seconds it has kept the observer squarely in view, unbroken
    //   flinch    it turned sharply within half a second, or the observer was told it panicked
    //             (shot at a decoy); stays an opening for a moment
    public sealed class BodyLanguage
    {
        public const float BackTurnedAngle = 110f;  // degrees between its facing and the observer
        public const float WatchingAngle   = 10f;   // degrees: looking this squarely is a stare
        public const float FlinchTurn      = 120f;  // degrees turned within FlinchSpan
        public const float FlinchSpan      = 0.5f;  // seconds
        public const float FlinchWindow    = 2f;    // seconds a flinch stays an opening

        private float   _lastRead = float.NegativeInfinity;
        private Vector3 _refFacing;
        private float   _refTime = float.NegativeInfinity;
        private float   _flinchUntil = float.NegativeInfinity;

        public bool  IsExposed { get; private set; }
        public float Watching  { get; private set; }

        public bool FlinchedAt(float now) => now < _flinchUntil;

        // `facing` is where the target faces, `toObserver` from the target to the observer (both
        // flattened here). The first reading after Forget starts the stare clock at zero.
        public void Read(Vector3 facing, Vector3 toObserver, bool reloading, float now)
        {
            facing.y = toObserver.y = 0f;
            float elapsed = float.IsNegativeInfinity(_lastRead) ? 0f : now - _lastRead;
            _lastRead = now;

            bool known = facing.sqrMagnitude > 0.0001f && toObserver.sqrMagnitude > 0.0001f;
            float angle = known ? Vector3.Angle(facing, toObserver) : 90f;

            IsExposed = angle >= BackTurnedAngle || reloading;
            Watching  = angle <= WatchingAngle ? Watching + elapsed : 0f;
            ReadTurn(facing, now);
        }

        // Out of its senses: it gives nothing away, and a stare in progress is broken.
        public void Lose()
        {
            IsExposed = false;
            Watching  = 0f;
            _lastRead = float.NegativeInfinity;
            _refTime  = float.NegativeInfinity;
        }

        public void NoteFlinch(float now) => _flinchUntil = now + FlinchWindow;

        public void Forget()
        {
            Lose();
            _flinchUntil = float.NegativeInfinity;
        }

        // Compares its facing against where it faced at least FlinchSpan ago.
        private void ReadTurn(Vector3 facing, float now)
        {
            if (facing.sqrMagnitude <= 0.0001f || now - _refTime < FlinchSpan) return;

            if (!float.IsNegativeInfinity(_refTime) && Vector3.Angle(_refFacing, facing) >= FlinchTurn) NoteFlinch(now);
            _refFacing = facing;
            _refTime   = now;
        }
    }
}
