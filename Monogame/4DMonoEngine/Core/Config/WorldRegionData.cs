using System.Runtime.Serialization;
using _4DMonoEngine.Core.Interfaces;

namespace _4DMonoEngine.Core.Config
{
    [DataContract]
    [KnownType(typeof(WorldRegionParameter))]
    [KnownType(typeof(WorldRegionLayer))]
    internal class WorldRegionData : IDataContainer
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public WorldRegionParameter[] Parameters { get; set; }
        [DataMember]
        public WorldRegionLayer[] Layers { get; set; }

        public string GetKey()
        {
            return Name;
        }
    }
}
