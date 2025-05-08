/* --------------- NPCMovementWalkToLocation ---------------- */
using UnityEngine;
using NPC.NPCAnimations;

class NPCMovementWalkToLocation : NPCMovementStrategy
{
    private readonly GameObject _targetLoc;
    private bool launched = false;

    public NPCMovementWalkToLocation(NPCMovement npcMovement, GameObject targetLocation)
        : base(npcMovement)
    {
        if (targetLocation == null) { Debug.LogError("Target Location null"); npcMovement.Enabled=false; return; }
        if (!targetLocation.CompareTag("Location")) { Debug.LogError("Target must be tagged Location"); npcMovement.Enabled=false; return; }

        _targetLoc = targetLocation;
    }

    public override void StartMovement()
    {
        if (launched) return;
        launched = true;

        MainAgent.SetDestination(_targetLoc.transform.position);
        MainAgent.stoppingDistance = 0f;

        NPCAnimBus.Bool(npcMovement.Manager.gameObject, NPCAnimationsType.Walk, true);
    }

    public override bool IsDone
    {
        get
        {
            bool finished = !MainAgent.pathPending &&
                            MainAgent.remainingDistance <= MainAgent.stoppingDistance;

            if (finished && launched)
            {
                NPCAnimBus.Bool(npcMovement.Manager.gameObject, NPCAnimationsType.Walk, false);
                launched = false;
            }
            return finished;
        }
    }
}