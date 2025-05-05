using UnityEngine;
using NPC.NPCAnimations;

class MovementWalkToLocation : MovementStrategy
{
    private readonly GameObject _targetLoc;
    private bool launched = false;

    public MovementWalkToLocation(GameObject NPC, GameObject targetLocation)
        : base(NPC)
    {
        /*if (targetLocation == null)
        {
            Debug.LogError("Target Location null"); 
            NPC.enabled=false; 
            return;
        }

        if (!targetLocation.CompareTag("Location"))
        {
            Debug.LogError("Target must be tagged Location"); 
            NPC.enabled=false; 
            return;
        }*/

        _targetLoc = targetLocation;
    }

    public override void StartMovement()
    {
        if (launched) return;
        launched = true;

        MainAgent.SetDestination(_targetLoc.transform.position);
        MainAgent.stoppingDistance = 0f;

        NPCAnimBus.Bool(NPC, NPCAnimationsType.Walk, true);
    }

    public override bool IsDone
    {
        get
        {
            bool finished = !MainAgent.pathPending &&
                            MainAgent.remainingDistance <= MainAgent.stoppingDistance;

            if (finished && launched)
            {
                NPCAnimBus.Bool(NPC, NPCAnimationsType.Walk, false);
                launched = false;
            }
            return finished;
        }
    }
}