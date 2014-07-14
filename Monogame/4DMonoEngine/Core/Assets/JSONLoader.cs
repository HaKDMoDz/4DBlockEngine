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

        public async Task<T> Load<T>(string filename, string recordName) where T : IDataContainer
        {
            //records that have the same filename as they do record name are single record files
            if (filename == recordName)
            {
                return await LoadSingle<T>(recordName);
            }
            return await LoadArray<T>(filename, recordName);
        }

        private async Task<T> LoadArray<T>(string filename, string recordName) where T : IDataContainer
        {
            if (m_loadedData.ContainsKey(recordName))
            {
                return (T)m_loadedData[recordName];
            }
            var path = Path.Combine(m_directory, DataContainerPathRegistry.PathPrefix<T>(), filename + ".json");
            Debug.Assert(File.Exists(path), "The path: " + path + " does not exist.");
            var t = Task.Run(() =>
            {
                var fileStream = new FileStream(path, FileMode.Open);
                var type = typeof(T);
                if (!m_serializers.ContainsKey(type))
                {
                    m_serializers.Add(type, new DataContractJsonSerializer(typeof(T[])));
                }
                var serializer = m_serializers[type];
                var response = (IDataContainer[])serializer.ReadObject(fileStream);
                foreach (var dataContainer in response)
                {
                    m_loadedData[dataContainer.GetKey()] = dataContainer;
                }
                fileStream.Close();
                return (T)m_loadedData[recordName];
            });
            return await t;
        }

        private async Task<T> LoadSingle<T>( string recordName) where T : IDataContainer
        {
            if (m_loadedData.ContainsKey(recordName))
            {
                return (T)m_loadedData[recordName];
            }
            var path = Path.Combine(m_directory, DataContainerPathRegistry.PathPrefix<T>(), recordName + ".json");
            Debug.Assert(File.Exists(path), "The path: " + path + " does not exist.");
            var t = Task.Run(() =>
            {
                var fileStream = new FileStream(path, FileMode.Open);
                var type = typeof(T);
                if (!m_serializers.ContainsKey(type))
                {
                    m_serializers.Add(type, new DataContractJsonSerializer(type));
                }
                var serializer = m_serializers[type];
                var response = (IDataContainer)serializer.ReadObject(fileStream);
                m_loadedData[response.GetKey()] = response;
                fileStream.Close();
                return (T) response;
            });
            return await t;
        }



        public void Unload()
        {
            m_loadedData.Clear();
            m_serializers.Clear();
        }

    }
}
