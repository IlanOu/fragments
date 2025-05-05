using UnityEngine;
using NPC.NPCAnimations;

class MovementYell : MovementStrategy
{
    private readonly AudioClip clip;
    private AudioSource source;
    private bool launched;
    private bool finished;

    public MovementYell(GameObject NPC, AudioClip clip)
        : base(NPC) => this.clip = clip;

    public override void StartMovement()
    {
        if (launched) return;
        launched = true;

        // Animation ON
        NPCAnimBus.Bool(NPC,
            NPCAnimationsType.Yell, true);

        // Lecture audio
        source = NPC.GetComponent<AudioSource>();
        if (source == null) source = NPC.AddComponent<AudioSource>();

        if (clip != null) source.PlayOneShot(clip);
        else              finished = true;
    }

    public override bool IsDone
    {
        get
        {
            if (finished) return true;
            if (!launched) return false;

            bool audioDone = (source == null) || !source.isPlaying;
            if (audioDone)
            {
                // Animation OFF
                NPCAnimBus.Bool(NPC,
                    NPCAnimationsType.Yell, false);
                finished = true;
            }
            return finished;
        }
    }
}
