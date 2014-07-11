using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace _4DMonoEngine.Core.Config
{
    internal static class DataContainerPathRegistry
    {
        private static readonly Dictionary<Type, String> PathDictionary = new Dictionary<Type, string>()
        {
                {typeof (Biome), "Biomes/"}, 
                {typeof (Province), "Province/"}, 
                {typeof (General), ""}, 
        };
        public static string PathPrefix<T>()
        {
            Debug.Assert(PathDictionary.ContainsKey(typeof(T)));
            return PathDictionary[typeof (T)];
        }
    }
}
