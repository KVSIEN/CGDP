using UnityEngine;

namespace CGD.Dialogue
{
    // Who is talking to whom, for choice actions and conditions (open *this* NPC's shop,
    // check *the player's* inventory).
    public class DialogueContext
    {
        public DialogueContext(GameObject speaker, GameObject listener, string speakerName)
        {
            Speaker     = speaker;
            Listener    = listener;
            SpeakerName = speakerName;
        }

        public GameObject Speaker     { get; }
        public GameObject Listener    { get; }
        public string     SpeakerName { get; }
    }
}
