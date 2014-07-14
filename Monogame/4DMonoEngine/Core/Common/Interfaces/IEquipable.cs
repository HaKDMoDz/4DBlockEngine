namespace _4DMonoEngine.Core.Common.Interfaces
{
    public interface IEquipable : IItem
    {
        IAction Use(int actionIndex);
        int InteractionDistance { get; }
    }
}