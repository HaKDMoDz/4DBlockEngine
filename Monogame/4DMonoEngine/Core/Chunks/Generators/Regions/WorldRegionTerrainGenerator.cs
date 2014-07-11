﻿using System.Collections;
using System.Collections.Generic;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Noise;
using _4DMonoEngine.Core.Config;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal abstract class WorldRegionTerrainGenerator
    {
        private readonly Dictionary<string, WorldRegionParameter> m_parameters;
        protected readonly SimplexNoise Noise;
        protected readonly List<WorldRegionLayer> Layers;

        public string Name { get; private set; }

        protected WorldRegionTerrainGenerator(SimplexNoise noise, string name, IEnumerable<WorldRegionLayer> layers, IEnumerable<WorldRegionParameter> parameters)
        {
            Noise = noise;
            m_parameters = new Dictionary<string, WorldRegionParameter>();
            Layers = new List<WorldRegionLayer>(layers);
            Layers.Sort((left, right) => left.Id - right.Id);
            Name = name;
            foreach (var parameter in parameters)
            {
                m_parameters.Add(parameter.Name, parameter);
            }
        }

        public bool Filter(string parameter, float value)
        {
            return m_parameters.ContainsKey(parameter) && value >= m_parameters[parameter].Min && value < m_parameters[parameter].Max;
        }

        public float ParamaterDistance(IDictionary parameterList)
        {
            float distance = 0;
            foreach (DictionaryEntry dictionaryEntry in parameterList)
            {
                var key = (string)dictionaryEntry.Key;
                if (m_parameters.ContainsKey(key))
                {
                    var d = (float)dictionaryEntry.Value - (m_parameters[key].Min + m_parameters[key].Max) / 2;
                    distance += d * d;
                }
                else
                {
                    distance += 1;
                }
            }
            return distance;
        }

        public abstract Block Apply(int upperBound, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW);
    }
}