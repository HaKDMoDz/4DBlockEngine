using System.Runtime.Serialization;

namespace _4DMonoEngine.Core.Config
{
    [DataContract]
    [KnownType(typeof(WorldRegionParameter))]
    [KnownType(typeof(WorldRegionLayer))]
    internal class BiomeData : WorldRegionData
    {
       // [DataMember]
       // public string[] ValidProvinces { get; set; }
        [DataMember]
        public float FoliageDensity { get; set; }
    }
}
