﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private readonly GraphicsDevice m_graphicsDevice;

        public AssetManager(ContentManager contentManager, GraphicsDevice graphicsDevice)
        {
            var executableDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase);
            Debug.Assert(!String.IsNullOrEmpty(executableDir));
            var absoluteDirPath = Path.Combine(executableDir, contentManager.RootDirectory).Substring(6);
            m_contentManager = contentManager;
            m_graphicsDevice = graphicsDevice;
            m_pathDictionary = new Dictionary<Type, string>
            {
               // {typeof (Model), "Models"}, 
                {typeof (Effect), Path.Combine(absoluteDirPath,"Effects")}, 
                {typeof (Texture2D), Path.Combine(absoluteDirPath,"Textures")}, 
                {typeof (SpriteFont), "Fonts"}
            };
            m_jsonLoader = new JsonLoader(Path.Combine(absoluteDirPath, "Config\\Json"));
            m_csvLoader = new TableLoader(Path.Combine(absoluteDirPath, "Config\\Tables"));
            //new Effect()
        }

        public T GetAsset<T>(string assetId)
        {
            Debug.Assert(!string.IsNullOrEmpty(assetId));
            Debug.Assert(m_pathDictionary.ContainsKey(typeof (T)));
            var path = Path.Combine(m_pathDictionary[typeof (T)], assetId);
            if (typeof (T) == typeof (Texture2D))
            {
                return (T)GetTexture(path);
            }
            if (typeof (T) == typeof (Effect))
            {
                return (T) GetEffect(path);
            }
            return m_contentManager.Load<T>(path);
        }

        private Object GetTexture(string path)
        {
            if (!Path.HasExtension(path))
            {
                path += ".png";
            }
            Debug.Assert(File.Exists(path), "The path: " + path + " does not exist.");
            var file = File.OpenRead(path);
            var texture = Texture2D.FromStream(m_graphicsDevice, file);
            file.Close();
            return texture;
        }

        private Object GetEffect(string path)
        {
            if (!Path.HasExtension(path))
            {
                path += ".mgfxo";
            }
            Debug.Assert(File.Exists(path), "The path: " + path + " does not exist.");
            var file = File.OpenRead(path);
            var reader = new BinaryReader(file);
            var effect = new Effect(m_graphicsDevice, reader.ReadBytes((int)reader.BaseStream.Length));
            reader.Close();
            return effect;
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