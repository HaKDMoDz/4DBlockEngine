using System.Collections.Generic;
using System.Runtime.Serialization;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Assets.DataObjects
{
    [DataContract]
    public class SaveDirectory : IDataContainer
    {
        public SaveDirectory()
        {
            Pages = new List<string>();
        }

        [DataMember]
        public List<string> Pages { get; set; }

        public string GetKey()
        {
            return "PageTable";
        }
    }
}
