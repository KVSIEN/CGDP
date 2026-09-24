using UnityEngine;

namespace CGD.Audio
{
    // Sound banks are optional almost everywhere. `bank.TryPlay()` looks null-safe but skips
    // Unity's null check, so a destroyed or missing asset would still be called; this
    // goes through it.
    public static class SoundBankExtensions
    {
        public static void TryPlay(this SoundBank bank, Vector3 position)
        {
            if (bank != null) bank.Play(position);
        }
    }
}
