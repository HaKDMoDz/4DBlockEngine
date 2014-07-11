using System.Runtime.Serialization;

namespace _4DMonoEngine.Core.Config
{
    [DataContract]
    public class BlockData
    {
        [DataMember]
        public string Biome { get; set; }
        [DataMember]
        public uint BlockStructureId { get; set; }
        [DataMember]
        public float Opacity { get; set; }
        [DataMember]
        public float Durability { get; set; }
        [DataMember]
        public float Elasticity { get; set; }
        [DataMember]
        public float Adhesion { get; set; }
        [DataMember]
        public float Mass { get; set; }
    }
}