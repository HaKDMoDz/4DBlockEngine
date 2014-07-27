using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Assets.Config;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Utils;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal abstract class WorldRegionTerrainGenerator
    {
        private const int MaxCacheSize = 10000;
        private const int Xoffset = 8;
        private const int Yoffset = 7;
        private const int Zoffset = 5;
        private const int Woffset = 3;
        private readonly Dictionary<string, WorldRegionParameter> m_parameters;
        private readonly float[] m_noiseBuffer;
        private readonly Queue<DepthCacheEntry> m_cacheExipiraQueue;
        private readonly Dictionary<Vector3, DepthCacheEntry> m_depthCache; 
        protected readonly List<WorldRegionLayer> Layers;

        public string Name { get; private set; }

        protected WorldRegionTerrainGenerator(float[] noiseBuffer, string name, IEnumerable<WorldRegionLayer> layers, IEnumerable<WorldRegionParameter> parameters)
        {
            m_noiseBuffer = noiseBuffer;
            m_cacheExipiraQueue = new Queue<DepthCacheEntry>();
            m_depthCache = new Dictionary<Vector3, DepthCacheEntry>();
            m_parameters = new Dictionary<string, WorldRegionParameter>();
            Layers = new List<WorldRegionLayer>(layers);
            Layers.Sort((left, right) => left.Id - right.Id);
            Name = name;
            foreach (var parameter in parameters)
            {
                m_parameters.Add(parameter.Name, parameter);
            }
        }

        protected float GetNoise(int x, int y, int z, int w, int offset, float scale)
        {
            var xProbe = MathUtilities.Modulo((int)(x / scale) + offset + Xoffset, m_noiseBuffer.Length);
            var yProbe = MathUtilities.Modulo((int)(y / scale) + offset + Yoffset, m_noiseBuffer.Length);
            var zProbe = MathUtilities.Modulo((int)(z / scale) + offset + Zoffset, m_noiseBuffer.Length);
            var wProbe = MathUtilities.Modulo((int)(w / scale) + offset + Woffset, m_noiseBuffer.Length);

            var xVal = m_noiseBuffer[xProbe];
            var yVal = m_noiseBuffer[yProbe];
            var zVal = m_noiseBuffer[zProbe];
            var wVal = m_noiseBuffer[wProbe];

            return (xVal + yVal + zVal + wVal)*.25f;
        }

        public bool Filter(string parameter, float value)
        {
            return !m_parameters.ContainsKey(parameter) || value >= m_parameters[parameter].Min && value < m_parameters[parameter].Max;
        }

        public float ParamaterDistance(IDictionary parameterList)
        {
            float distance = 0;
            //The LINQ expression produces a stupidly long line of code that I think is less readable
// ReSharper disable once LoopCanBeConvertedToQuery
            foreach (DictionaryEntry dictionaryEntry in parameterList)
            {
                var key = (string)dictionaryEntry.Key;
                if (!m_parameters.ContainsKey(key))
                {
                    distance += 1;
                    continue;
                }
                var d = (float)dictionaryEntry.Value - (m_parameters[key].Min + m_parameters[key].Max) / 2;
                distance += d * d;
            }
            return distance;
        }

        protected DepthCacheEntry GetDepthCache(int x, int z, int w)
        {
            var query = new Vector3(x, z, w);
            if (m_depthCache.ContainsKey(query))
            {
                return m_depthCache[query];
            }
            DepthCacheEntry cache;
            if (m_cacheExipiraQueue.Count > MaxCacheSize)
            {
                cache = m_cacheExipiraQueue.Dequeue();
                m_depthCache.Remove(cache.Key);
                cache.Key = query;
                cache.Depth = 0;
                cache.LayerIndex = 0;
            }
            else
            {
                cache = new DepthCacheEntry(query);
            }
            m_depthCache[query] = cache;
            m_cacheExipiraQueue.Enqueue(cache);
            return cache;
        }

        protected class DepthCacheEntry
        {
            public int LayerIndex;
            public float Depth;
            public Vector3 Key;
            public DepthCacheEntry(Vector3 key)
            {
                Key = key;
                LayerIndex = 0;
                Depth = 0;
            }
        }

        public abstract Block Apply(int upperBound, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW);
    }
}
