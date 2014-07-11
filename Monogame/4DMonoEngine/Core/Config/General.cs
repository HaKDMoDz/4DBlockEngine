using System.Runtime.Serialization;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Config
{
    [DataContract]
    public class General : IDataContainer
    {
        [DataMember]
        public string[] Biomes { get; set; }
        [DataMember]
        public string[] Provinces { get; set; }
        [DataMember]
        public int SeaLevel { get; set; }
        [DataMember]
        public int MountainHeight { get; set; }
        [DataMember]
        public int DetailScale { get; set; }
        [DataMember]
        public int SinkHoleDepth { get; set; }
        [DataMember]
        public int BiomeCentroidSampleScale { get; set; }
        [DataMember]
        public int BiomeSampleRescale { get; set; }
        [DataMember]
        public int BiomeThickness { get; set; }

        public string GetKey()
        {
            return "General";
        }
    }
}
