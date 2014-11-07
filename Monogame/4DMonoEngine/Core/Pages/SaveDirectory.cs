using System.Runtime.Serialization;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Pages
{
    [DataContract]
    public class SaveDirectory : IDataContainer
    {
        [DataMember]
        public string[] Pages { get; set; }


        public string GetKey()
        {
            return "PageTable";
        }
    }
}
