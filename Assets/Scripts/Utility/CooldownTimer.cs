using System;
using UnityEngine;

// Shared ready/cooldown model: Start() begins a cooldown of a given duration,
// Tick() advances it, IsReady/Ratio expose progress for gameplay and HUD code.
[Serializable]
public struct CooldownTimer
{
    [SerializeField] private float _duration;
    [SerializeField] private float _remaining;

    public bool IsReady => _remaining <= 0f;

    // 0 = just started, 1 = ready.
    public float Ratio => _duration <= 0f ? 1f : 1f - Mathf.Clamp01(_remaining / _duration);

    public void Start(float duration)
    {
        _duration  = duration;
        _remaining = duration;
    }

    public void Tick(float deltaTime)
    {
        if (_remaining > 0f)
            _remaining -= deltaTime;
    }
}
