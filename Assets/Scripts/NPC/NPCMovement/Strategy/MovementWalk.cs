using UnityEngine;
using UnityEngine.AI;

namespace NPC.NPCMovement.Strategy
{
    class MovementWalk : MovementStrategy
    {
        private readonly float _min;
        private readonly float _max;
        private Vector3 targetPos;
        private bool hasStarted = false;

        public MovementWalk(GameObject NPC,
            float minWander,
            float maxWander)
            : base(NPC)
        {
            _min = minWander;
            _max = maxWander;

            Vector3 rnd = Random.insideUnitSphere * Random.Range(_min, _max);
            targetPos   = rnd + NPC.transform.position;
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
                    hasStarted = false;          // évite de spammer
                }
                return finished;
            }
        }
    }
}