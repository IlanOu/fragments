using UnityEngine;

namespace NPC.NPCMovement.Strategy
{
    class MovementWalkToLocation : MovementStrategy
    {
        private readonly GameObject _targetLoc;
        private bool launched = false;

        public MovementWalkToLocation(GameObject NPC, GameObject targetLocation)
            : base(NPC)
        {
            _targetLoc = targetLocation;
        }

        public override void StartMovement()
        {
            if (launched) return;
            launched = true;

            MainAgent.SetDestination(_targetLoc.transform.position);
            MainAgent.stoppingDistance = 0f;
        }

        public override bool IsDone
        {
            get
            {
                bool finished = !MainAgent.pathPending &&
                                MainAgent.remainingDistance <= MainAgent.stoppingDistance;

                if (finished && launched)
                {
                    launched = false;
                }
                return finished;
            }
        }
    }
}