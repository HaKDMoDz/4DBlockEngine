namespace _4DMonoEngine.Core.Interfaces
{
    public interface IEquipable : IItem
    {
        IAction Use(int actionIndex);
        int InteractionDistance { get; }
    }
}