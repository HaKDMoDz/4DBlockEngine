using System.Runtime.Serialization;

namespace _4DMonoEngine.Core.Config
{
    [DataContract]
    [KnownType(typeof(WorldRegionParameter))]
    [KnownType(typeof(WorldRegionLayer))]
    internal class Province : WorldRegionData
    {

    }
}
