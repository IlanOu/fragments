using UnityEngine;

namespace NPC.NPCMovement.Strategy
{
    class TalkStrategy : MovementStrategy
    {
        private readonly AudioClip clip;
        private AudioSource source;
        private bool launched;
        private bool finished;

        public TalkStrategy(GameObject NPC, AudioClip clip)
            : base(NPC) => this.clip = clip;

        public override void StartMovement()
        {
            if (launched) return;
            launched = true;
        
            // Lecture audio
            source = NPC.GetComponent<AudioSource>();
            if (source == null) source = NPC.AddComponent<AudioSource>();

            if (clip != null) source.PlayOneShot(clip);
            else              finished = true;                   // pas de son → fin immédiate
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
                    finished = true;
                }
                return finished;
            }
        }
    }
}
