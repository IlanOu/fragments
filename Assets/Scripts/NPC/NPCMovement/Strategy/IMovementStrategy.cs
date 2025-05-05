namespace NPC.NPCMovement.Strategy
{
    public interface IMovementStrategy
    {
        void StartMovement();
        bool IsDone { get; }
    }
}