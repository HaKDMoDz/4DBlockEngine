using System.Runtime.Serialization;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Assets.Config
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
        [DataMember]
        public byte EmissivitySun { get; set; }
        [DataMember]
        public byte EmissivityRed { get; set; }
        [DataMember]
        public byte EmissivityGreen { get; set; }
        [DataMember]
        public byte EmissivityBlue { get; set; }

        public string GetKey()
        {
            return Name;
        }
    }
}