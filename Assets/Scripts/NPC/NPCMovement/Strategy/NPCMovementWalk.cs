using UnityEngine;
using UnityEngine.AI;
using NPC.NPCAnimations;   // enum + bus

class NPCMovementWalk : NPCMovementStrategy
{
    private readonly float _min;
    private readonly float _max;
    private Vector3 targetPos;
    private bool hasStarted = false;

    public NPCMovementWalk(NPCMovement npcMovement,
        float minWander,
        float maxWander)
        : base(npcMovement)
    {
        _min = minWander;
        _max = maxWander;

        Vector3 rnd = Random.insideUnitSphere * Random.Range(_min, _max);
        targetPos   = rnd + npcMovement.Manager.transform.position;
    }

    /* ---------- Lancement ---------- */
    public override void StartMovement()
    {
        if (hasStarted) return;
        hasStarted = true;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, _max, NavMesh.AllAreas))
        {
            MainAgent.SetDestination(hit.position);
            MainAgent.stoppingDistance = 0f;

            // Animation ON
            NPCAnimBus.Bool(npcMovement.Manager.gameObject,
                NPCAnimationsType.Walk,
                true);
        }
    }

    /* ---------- Fin du déplacement ---------- */
    public override bool IsDone
    {
        get
        {
            // On considère terminé quand l’agent n’a plus de chemin ou
            // qu’il est arrivé à destination.
            bool finished = !MainAgent.pathPending &&
                            MainAgent.remainingDistance <= MainAgent.stoppingDistance;

            if (finished && hasStarted)
            {
                // Animation OFF (une seule fois)
                NPCAnimBus.Bool(npcMovement.Manager.gameObject,
                    NPCAnimationsType.Walk,
                    false);
                hasStarted = false;          // évite de spammer
            }
            return finished;
        }
    }
}