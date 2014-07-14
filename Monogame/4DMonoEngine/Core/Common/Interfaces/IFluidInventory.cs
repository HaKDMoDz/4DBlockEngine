namespace _4DMonoEngine.Core.Common.Interfaces
{
    interface IFluidInventory
    {
        float Volume { get; }
        float Capacity { get; }
        float TryAddFluid(float amount);
        float TryRemoveFluid(float amount);
        float Empty();
    }
}
