using System.Runtime.Serialization;

namespace _4DMonoEngine.Core.Assets.Config
{
    [DataContract]
    [KnownType(typeof(WorldRegionParameter))]
    [KnownType(typeof(WorldRegionLayer))]
    internal class ProvinceData : WorldRegionData
    {

    }
}
