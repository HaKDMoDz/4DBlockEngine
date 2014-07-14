using System.Runtime.Serialization;

namespace _4DMonoEngine.Core.Config
{
    [DataContract(Name = "Layer")]
    internal class WorldRegionLayer
    {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string BlockName { get; set; }
        [DataMember]
        public int Thickness { get; set; }
        [DataMember]
        public float NoiseOffset { get; set; }
        [DataMember]
        public float NoiseScale { get; set; }
    }
}