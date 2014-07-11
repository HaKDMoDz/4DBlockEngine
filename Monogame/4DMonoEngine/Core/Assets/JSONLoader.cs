using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using _4DMonoEngine.Core.Interfaces;
using _4DMonoEngine.Core.Config;

namespace _4DMonoEngine.Core.Assets
{
    internal class JsonLoader
    {
        private readonly Dictionary<Type, DataContractJsonSerializer> m_serializers;
        private readonly Dictionary<String, IDataContainer> m_loadedData; 
        private readonly string m_directory;

        public JsonLoader(string directory)
        {
            m_serializers = new Dictionary<Type, DataContractJsonSerializer>();
            m_directory = directory;
            m_loadedData = new Dictionary<string, IDataContainer>();
        }

        public async Task<T> Load<T>(string filename, string recordName)
        {
            Debug.Assert(typeof(T).IsAssignableFrom(typeof(IDataContainer)));
            if (m_loadedData.ContainsKey(recordName))
            {
                return (T) m_loadedData[filename];
            }
            Debug.Assert(File.Exists(m_directory + filename + ".json"));
            var t = Task.Run(() =>
            {
                var fileStream = new FileStream(m_directory + DataContainerPathRegistry.PathPrefix<T>() + filename + ".json", FileMode.Open);
                var type = typeof (T);
                if (!m_serializers.ContainsKey(type))
                {
                    m_serializers.Add(type, new DataContractJsonSerializer(typeof (T[])));
                }
                var serializer = m_serializers[type];
                var response = (IDataContainer[])serializer.ReadObject(fileStream);
                foreach (var dataContainer in response)
                {
                    m_loadedData[dataContainer.GetKey()] = dataContainer;
                }
                fileStream.Close();
            });
            await t;
            return (T)m_loadedData[filename];
        }


        public void Unload()
        {
            m_loadedData.Clear();
            m_serializers.Clear();
        }

    }
}
