using System.Runtime.Serialization;
using _4DMonoEngine.Core.Interfaces;

namespace _4DMonoEngine.Core.Config
{
    [DataContract]
    public class BlockData : IDataContainer
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public ushort BlockId { get; set; }
        [DataMember]
        public string[] TextureNames { get; set; }
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

        public string GetKey()
        {
            return Name;
        }
    }
}