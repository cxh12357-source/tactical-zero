namespace TacticalZero
{
    public interface IInteractable
    {
        string Label { get; }

        bool CanInteract(PlayerHealth player, out string reason);
        bool Interact(PlayerHealth player);
    }
}
