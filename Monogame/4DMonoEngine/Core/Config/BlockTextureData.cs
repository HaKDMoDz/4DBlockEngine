using System.Runtime.Serialization;
using _4DMonoEngine.Core.Enums;
using _4DMonoEngine.Core.Interfaces;

namespace _4DMonoEngine.Core.Config
{
    [DataContract]
    public class BlockTextureData : IDataContainer
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public int[] FaceTextureIds { get; set; }
        [DataMember]
        public float TileU { get; set; }
        [DataMember]
        public float TileV { get; set; }

       

        public string GetKey()
        {
            return Name;
        }
    }
}