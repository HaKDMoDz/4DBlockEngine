using System.Runtime.Serialization;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Assets.DataObjects
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
