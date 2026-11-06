using System;
using UnityEngine;
using CGD.Audio;

namespace CGD.BehaviorTrees
{
    // Plays a sound bank where the agent stands — a bark ("Over there!"), a grunt, a taunt —
    // then succeeds. Put a Cooldown above it so a bark doesn't repeat every few frames.
    [Serializable]
    [BtNodeInfo("Actions/Play Sound", "Plays a sound bank at the agent (a bark, a taunt), then succeeds.")]
    public sealed class PlaySound : BtAction
    {
        [SerializeField] private SoundBank _sound;

        public override string Summary => _sound != null ? _sound.name : "(no sound)";

        protected override BtStatus OnTick(BtContext context)
        {
            if (_sound != null && context.Owner != null) _sound.Play(context.Owner.transform.position);
            return BtStatus.Success;
        }
    }
}
