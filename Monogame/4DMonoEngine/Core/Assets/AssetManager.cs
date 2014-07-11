using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Interfaces;

namespace _4DMonoEngine.Core.Assets
{
    internal class AssetManager
    {
        private readonly ContentManager m_contentManager;
        private readonly Dictionary<Type, string> m_pathDictionary;
        private readonly JsonLoader m_jsonLoader;
        private readonly TableLoader m_csvLoader;

        public AssetManager(ContentManager contentManager)
        {
            m_contentManager = contentManager;
            m_pathDictionary = new Dictionary<Type, string>
            {
                {typeof (Model), "Models/"}, 
                {typeof (Effect), "Effects/"}, 
                {typeof (Texture2D), "Textures/"}, 
                {typeof (SpriteFont), "Fonts/"}
            };
            m_jsonLoader = new JsonLoader(contentManager.RootDirectory + "/Config/Json/");
            m_csvLoader = new TableLoader(contentManager.RootDirectory + "/Config/Tables/");
        }

        public T GetAsset<T>(string assetId)
        {
            Debug.Assert(!string.IsNullOrEmpty(assetId));
            Debug.Assert(m_pathDictionary.ContainsKey(typeof (T)));
            var path = m_pathDictionary[typeof (T)];
            return m_contentManager.Load<T>(path + assetId);
        }

        public async Task<Dictionary<string, T>> GetTable<T>(string tableName) where T : IDataContainer
        {
            Debug.Assert(!string.IsNullOrEmpty(tableName));
            return await m_csvLoader.Load<T>(tableName);
        }

        public async Task<T> GetConfig<T>(string fileName, string recordId) where T : IDataContainer
        {
            Debug.Assert(!string.IsNullOrEmpty(fileName));
            Debug.Assert(!string.IsNullOrEmpty(recordId));
            return await m_jsonLoader.Load<T>(fileName, recordId);
        }

        public void Unload()
        {
            m_contentManager.Unload();
            m_csvLoader.Unload();
            m_jsonLoader.Unload();
        }
    }
}