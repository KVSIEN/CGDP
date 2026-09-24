using System;
using UnityEngine;
using CGD.Audio;

namespace CGD.Combat
{
    [Serializable]
    public class SoundEvent : IActionEvent
    {
        [SerializeField] private int _startFrame;
        [SerializeField] private SoundBank _sound;

        public int StartFrame => _startFrame;
        public int EndFrame => -1;

        public void OnEnter(ActionContext ctx)
        {
            _sound.TryPlay(ctx.Origin);
        }

        public void OnTick(ActionContext ctx) { }
        public void OnExit(ActionContext ctx) { }
    }
}
