public interface IMovementStrategy
{
    void StartMovement();
    bool IsDone { get; }
}