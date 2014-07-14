using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace _4DMonoEngine.Core.Assets.Config
{
    internal static class DataContainerPathRegistry
    {
        private static readonly Dictionary<Type, String> PathDictionary = new Dictionary<Type, string>()
        {
                {typeof (BiomeData), "Biomes"}, 
                {typeof (ProvinceData), "Provinces"}, 
                {typeof (BlockData), "Blocks"},
                {typeof (BlockTextureData), "Blocks"},
                {typeof (General), ""}, 
        };
        public static string PathPrefix<T>()
        {
            Debug.Assert(PathDictionary.ContainsKey(typeof(T)));
            return PathDictionary[typeof (T)];
        }
    }
}
