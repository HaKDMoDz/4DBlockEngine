namespace _4DMonoEngine.Core.Common.Enums
{
    public interface IAssetProvider
    {
        T Get<T>(string assetId);
    }
}
