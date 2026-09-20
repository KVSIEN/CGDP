using UnityEngine;

namespace CGD.Weapons
{
    // Owns spread state for one weapon: the current bloom above the base cone and when
    // the last shot fired. AddBloom pumps it on each shot; Tick recovers it when the
    // trigger has been quiet long enough. EffectiveConeDeg returns what fire behaviors
    // and the crosshair should use — base cone + bloom scaled by ADS and current heat.
    public class SpreadProcessor
    {
        private WeaponData _data;

        private float _current;
        private float _lastShotTime = float.NegativeInfinity;

        public float Current => _current;

        public void Configure(WeaponData data)
        {
            _data = data;
            Reset();
        }

        public void Reset()
        {
            _current      = 0f;
            _lastShotTime = float.NegativeInfinity;
        }

        // Bloom may overshoot MaxSpread by one shot's worth; Tick settles it back before
        // the next shot, so the crosshair still pulses with each shot at max while the
        // spread bullets actually use (read before this is called) never exceeds MaxSpread.
        public void AddBloom()
        {
            _current      = Mathf.Min(_current, _data.MaxSpread) + _data.SpreadPerShot;
            _lastShotTime = Time.time;
        }

        // Bloom recovers once the gun has been quiet for RecoilRecoveryDelay. Automatic
        // weapons fire faster than that, so bloom still builds while spraying; slow weapons
        // (shotguns, snipers) start closing soon after each shot instead of staying fully
        // bloomed until the next round is ready.
        public void Tick(float dt, bool cooldownReady, float settleFraction)
        {
            bool recovering = cooldownReady || Time.time - _lastShotTime >= _data.RecoilRecoveryDelay;
            if (!recovering)
            {
                // Still firing: only settle the at-cap overshoot.
                if (_current > _data.MaxSpread)
                    _current -= (_current - _data.MaxSpread) * settleFraction;
                return;
            }

            if (_current > 0f)
                _current = Mathf.Max(_current - _data.SpreadRecovery * dt, 0f);
        }

        // Cone half-angle used for both the actual shot and the crosshair. Heat comes
        // from RecoilProcessor and controls how much the ADS bloom is scaled up.
        public float EffectiveConeDeg(float adsT, float heat)
        {
            float baseDeg    = Mathf.Lerp(_data.HipSpreadDeg, _data.AdsSpreadDeg, adsT);
            float bloomScale = Mathf.Lerp(1f, _data.GetAdsSpreadMultiplier(heat), adsT);
            return baseDeg + _current * bloomScale;
        }
    }
}
