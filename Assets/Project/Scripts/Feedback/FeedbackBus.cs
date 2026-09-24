using System;
using UnityEngine;

namespace CGD.Feedback
{
    // The one place gameplay asks for player feedback. Callers name what happened (a
    // preset, or a plain message) and the scene's FeedbackPlayer decides how it shows
    // up — feed, flash, vibration, shake, sound. Same broadcast style as Noise, so
    // pickups and quests don't need a reference to the HUD. Listeners must unsubscribe
    // in OnDisable.
    public static class FeedbackBus
    {
        public static event Action<FeedbackRequest> Requested;

        public static void Play(FeedbackPreset preset, string detail = null, float intensity = 1f)
        {
            if (preset == null) return;

            string message = preset.HasMessage ? preset.FormatMessage(detail) : null;
            Requested?.Invoke(new FeedbackRequest(preset, message, preset.Style, Mathf.Max(0f, intensity)));
        }

        // A message on its own, for things that don't warrant an asset ("+30 Rounds").
        public static void Notify(string message, NotificationStyle style = NotificationStyle.Info)
        {
            if (!string.IsNullOrEmpty(message))
                Requested?.Invoke(new FeedbackRequest(null, message, style, 1f));
        }
    }
}
