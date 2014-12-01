using System.Runtime.Serialization;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Assets.DataObjects
{
    [DataContract]
    public class BlockTextureData : IDataContainer
    {
        [DataMember]
        public string Name { get; set; }
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