namespace _4DMonoEngine.Core.Interfaces
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
