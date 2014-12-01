using System.Runtime.Serialization;

namespace _4DMonoEngine.Core.Assets.DataObjects
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
        public int NoiseOffset { get; set; }
        [DataMember]
        public float NoiseScale { get; set; }
    }
}