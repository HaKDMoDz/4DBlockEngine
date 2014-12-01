using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using _4DMonoEngine.Core.Assets.DataObjects;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Assets
{
    internal class JsonWriter
    {
        private readonly Dictionary<Type, DataContractJsonSerializer> m_serializers;
        private readonly string m_directory;

        public JsonWriter(string directory)
        {
            m_serializers = new Dictionary<Type, DataContractJsonSerializer>();
            m_directory = directory;
        }

        public async void Write<T>(string filename, IEnumerable<T> records) where T : IDataContainer
        {
            var path = Path.Combine(m_directory, DataContainerPathRegistry.PathPrefix<T>(), filename + ".json");
            await Task.Run(() =>
            {
                using (var fileStream = new FileStream(path, FileMode.OpenOrCreate))
                {
                    var type = typeof (T);
                    if (!m_serializers.ContainsKey(type))
                    {
                        m_serializers.Add(type, new DataContractJsonSerializer(typeof (T[])));
                    }
                    var serializer = m_serializers[type];
                    serializer.WriteObject(fileStream, records);
                };
            });
        }

        public async void Write<T>(string fileName, T record) where T : IDataContainer
        {
            var path = Path.Combine(m_directory, DataContainerPathRegistry.PathPrefix<T>(), fileName + ".json");
            await Task.Run(() =>
            {
                using (var fileStream = new FileStream(path, FileMode.OpenOrCreate))
                {
                    var type = typeof (T);
                    if (!m_serializers.ContainsKey(type))
                    {
                        m_serializers.Add(type, new DataContractJsonSerializer(type));
                    }
                    var serializer = m_serializers[type];
                    serializer.WriteObject(fileStream, record);
                }
            });
        }
    }
}
