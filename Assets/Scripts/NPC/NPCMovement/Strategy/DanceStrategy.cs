/* ------------------ NPCMovementDance ------------------ */

using UnityEngine;

namespace NPC.NPCMovement.Strategy
{
    class DanceStrategy : MovementStrategy
    {
        private readonly float danceDuration;
        private float timer = 0f;
        private bool launched = false;

        public DanceStrategy(GameObject NPC, float duration = 3f)
            : base(NPC)
        {
            danceDuration = duration;
        }

        public override void StartMovement()
        {
            if (launched) return;
            launched = true;
            timer = 0f;
        }

        public override bool IsDone
        {
            get
            {
                if (!launched) return false;

                timer += Time.deltaTime;

                if (timer >= danceDuration)
                {
                    launched = false;

                    return true;
                }
                return false;
            }
        }
    }
}