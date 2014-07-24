using System;
using System.Collections;
using System.Collections.Generic;
using _4DMonoEngine.Core.Assets.Config;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Noise;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal abstract class WorldRegionTerrainGenerator
    {
        private const int Xoffset = 8;
        private const int Yoffset = 7;
        private const int Zoffset = 5;
        private const int Woffset = 3;
        private readonly Dictionary<string, WorldRegionParameter> m_parameters;
        private readonly float[] m_noiseBuffer; 
        protected readonly List<WorldRegionLayer> Layers;

        public string Name { get; private set; }

        protected WorldRegionTerrainGenerator(float[] noiseBuffer, string name, IEnumerable<WorldRegionLayer> layers, IEnumerable<WorldRegionParameter> parameters)
        {
            m_noiseBuffer = noiseBuffer;
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

        public abstract Block Apply(int upperBound, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW);
    }
}
