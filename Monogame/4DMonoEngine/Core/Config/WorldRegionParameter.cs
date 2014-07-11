using System.Runtime.Serialization;

namespace _4DMonoEngine.Core.Config
{
    [DataContract(Name = "Parameter")]
    internal class WorldRegionParameter
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public float Min { get; set; }
        [DataMember]
        public float Max { get; set; }
    }
}