using UnityEngine;
using UnityEngine.AI;

namespace NPC.NPCMovement.Strategy
{
    public abstract class MovementStrategy : IMovementStrategy
    {
        protected readonly GameObject NPC;
        protected NavMeshAgent MainAgent => NPC.GetComponentInChildren<NavMeshAgent>();

        protected MovementStrategy(GameObject NPC)
        {
            this.NPC = NPC;
        }

        public abstract void StartMovement();
        public abstract bool IsDone { get; }
    }
}