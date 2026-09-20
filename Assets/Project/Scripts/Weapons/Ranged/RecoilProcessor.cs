using UnityEngine;

namespace CGD.Weapons
{
    // One shot's worth of recoil, computed from the current weapon and state.
    // WeaponController turns this into calls on PlayerCamera and WeaponVisuals.
    public struct RecoilShot
    {
        // Camera-side kick — these are the values passed to PlayerCamera.AddRecoil.
        public float VertKick;
        public float HorizKick;
        // Weapon-model kick before the hip/ADS camera share is applied — the values passed
        // to WeaponVisuals.AddKick so the gun animates the full kick regardless of ADS.
        public float GunVert;
        public float GunHoriz;
        // Interpolated recovery fraction between hip and ADS, so ADS returns further to origin.
        public float RecoveryFraction;
    }

    // Owns all recoil state for one weapon: the accumulated climb, the heat buildup, and
    // the horizontal drift-sign that flips at the cap in Alternating mode. Given adsT it
    // returns the numbers WeaponController needs; given a Tick it drains heat and eases
    // the at-cap vertical overshoot back before the next round fires.
    public class RecoilProcessor
    {
        private WeaponData _data;

        private float _accVert;
        private float _accHoriz;
        private float _heat;
        private float _driftSign = 1f;
        private bool  _wasFiring;

        // 0..1; consumed by SpreadProcessor for ADS bloom scaling and passed back into
        // Fire() indirectly through the WeaponData multipliers each shot.
        public float Heat => _heat;

        public void Configure(WeaponData data)
        {
            _data = data;
            Reset();
        }

        public void Reset()
        {
            _accVert   = 0f;
            _accHoriz  = 0f;
            _heat      = 0f;
            _driftSign = 1f;
            _wasFiring = false;
        }

        // Called on each shot. Advances heat, computes this shot's kick along both axes,
        // and honors HorizontalDriftMode (Alternating flips at the cap; OneWay pins there).
        public RecoilShot Fire(float adsT)
        {
            float vertMult  = Mathf.Lerp(_data.HipRecoilVerticalMultiplier,   _data.AdsRecoilMultiplier, adsT);
            float horizMult = Mathf.Lerp(_data.HipRecoilHorizontalMultiplier, _data.AdsRecoilMultiplier, adsT);

            // Heat is read BEFORE this shot adds to it, so the first shot always kicks at 1×.
            float kickHeat   = Mathf.Lerp(1f, _data.MaxHeatRecoilMultiplier,    _heat);
            float jitterHeat = Mathf.Lerp(1f, _data.RecoilHeatJitterMultiplier, _heat);
            _heat = Mathf.Min(_heat + _data.RecoilHeatPerShot, 1f);

            // Vertical: constant full kick plus jitter. Kick still applies at the cap;
            // Tick() eases the overshoot back before the next shot fires.
            float gunVert  = _data.RecoilScale.y * (1f + BlendedJitter(_data.RecoilJitter.y) * jitterHeat) * kickHeat;
            float vertKick = gunVert * vertMult;
            _accVert += vertKick;

            // Horizontal: bias + jitter, direction gated by drift mode.
            float horizCap = _data.MaxAccumulatedHorizontalRecoil;
            float gunHoriz = HorizontalKick(kickHeat, jitterHeat);
            if (_data.HorizontalDriftMode == HorizontalDriftMode.Alternating &&
                Mathf.Abs(_accHoriz + gunHoriz * horizMult) > horizCap)
            {
                _driftSign = -_driftSign;
                gunHoriz   = HorizontalKick(kickHeat, jitterHeat);
            }
            float horizKick = Mathf.Clamp(gunHoriz * horizMult,
                                          -horizCap - _accHoriz, horizCap - _accHoriz);
            _accHoriz += horizKick;

            return new RecoilShot
            {
                VertKick         = vertKick,
                HorizKick        = horizKick,
                GunVert          = gunVert,
                GunHoriz         = gunHoriz,
                RecoveryFraction = Mathf.Lerp(_data.RecoilRecoveryFraction, _data.AdsRecoilRecoveryFraction, adsT),
            };
        }

        // Called every frame. Returns the amount of vertical settle the camera should apply
        // (0 if none). settleFraction is the caller-computed dt/cooldownRemaining ratio, so
        // the at-cap overshoot eases back over exactly one fire interval.
        public float Tick(float dt, bool isFiring, float settleFraction)
        {
            if (!isFiring && _wasFiring)
            {
                // Gun just went idle — reset the caps instantly so the next burst starts
                // fresh. Heat cools gradually (below) so quick follow-up bursts stay hot.
                _accVert   = 0f;
                _accHoriz  = 0f;
                _driftSign = 1f;
            }
            _wasFiring = isFiring;

            float settle = 0f;
            float vertOvershoot = _accVert - _data.MaxAccumulatedRecoil;
            if (isFiring && vertOvershoot > 0f)
            {
                settle    = vertOvershoot * settleFraction;
                _accVert -= settle;
            }

            if (!isFiring && _heat > 0f)
                _heat = Mathf.Max(_heat - _data.RecoilHeatCooldown * dt, 0f);

            return settle;
        }

        private float HorizontalKick(float kickHeat, float jitterHeat)
        {
            float jitter = BlendedJitter(_data.RecoilJitter.x) * jitterHeat;
            return _data.RecoilScale.x * (_data.RecoilHorizontalBias * _driftSign + jitter) * kickHeat;
        }

        // Averaging two uniform samples gives a triangular, center-weighted spread
        // over the same [-magnitude, magnitude] range instead of a flat distribution.
        private static float BlendedJitter(float magnitude)
        {
            float a = Random.Range(-magnitude, magnitude);
            float b = Random.Range(-magnitude, magnitude);
            return (a + b) * 0.5f;
        }
    }
}
