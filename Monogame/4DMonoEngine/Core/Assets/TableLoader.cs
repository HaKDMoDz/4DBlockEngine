using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using _4DMonoEngine.Core.Common.Interfaces;


namespace _4DMonoEngine.Core.Assets
{
    internal class TableLoader
    {
        //Using the non-generic version of the dictionary because of type coercion issues
        private readonly Hashtable m_loadedData; 
        private readonly string m_directory;

        public TableLoader(string directory)
        {
            m_directory = directory;
            m_loadedData = new Hashtable();
        }

        public async Task<Dictionary<string, T>> Load<T>(string filename) where T : IDataContainer 
        {
            if (m_loadedData.ContainsKey(filename))
            {
                return (Dictionary<string, T>) m_loadedData[filename];
            }
            Debug.Assert(File.Exists(m_directory + filename + ".csv"));
            var t = Task.Run(() => 
            {
                var fileStream = new FileStream(m_directory + filename + ".csv", FileMode.Open);
                var csv = new CsvReader(new StreamReader(fileStream));
                var records = csv.GetRecords<T>().ToDictionary(record => record.GetKey());
                m_loadedData.Add(filename, records);
                fileStream.Close();
            });
            await t;
            return (Dictionary<string, T>)m_loadedData[filename];
        }

        public void Unload()
        {
            m_loadedData.Clear();
        }
    }
}
