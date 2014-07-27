﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Assets.Config;
using _4DMonoEngine.Core.Utils.Noise;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal struct RegionData
    {
        public float Humidity;
        public float Temperature;
        public float GeologicalActivity;
        public string Type;
    }

    internal abstract class WorldRegionGeneratorCollection<T> where T : WorldRegionData
    {
        protected readonly int SeaLevel; // 64;
        protected readonly int MountainHeight; // 64;
        protected readonly int BiomeSampleRescale; // 512;

        private readonly Dictionary<string, WorldRegionTerrainGenerator> m_generators;
        protected readonly SimplexNoise SimplexNoise;
        protected readonly SimplexNoise SimplexNoise2;
        protected readonly GetHeight GetHeightFunction;
        public delegate float GetHeight(float x, float z, float w);

        protected WorldRegionGeneratorCollection(ulong seed, GetHeight getHeightFunction, IEnumerable<string> regions, int biomeSampleRescale, int seaLevel, int mountainHeight)
        {
            BiomeSampleRescale = biomeSampleRescale;
            SeaLevel = seaLevel;
            MountainHeight = mountainHeight;

            SimplexNoise = new SimplexNoise(seed);
            SimplexNoise2 = new SimplexNoise(seed + 0x8fd3952e35bb901f); // the 2 generators just need to be offset by some arbitrary value
            m_generators = new Dictionary<string, WorldRegionTerrainGenerator>();
            InitializeAsync(seed + 0x3a98bfad, regions); //another arbitrary offset
            GetHeightFunction = getHeightFunction;
        }

        private async void InitializeAsync(ulong seed, IEnumerable<string> regions)
        {
            var simplexNoise = new SimplexNoise(seed);
            var noiseCache = new float[1000];
            for (var i = 0; i < noiseCache.Length; i++)
            {
                noiseCache[i] = (MathHelper.Clamp(simplexNoise.Perlin3Dfmb(i, 0, 0, 64, 0, 2) * 4, -1, 1) + 1) * 0.5f;
            }
            foreach (var region in regions)
            {
                var fileName = "Base";
                var recordName = region;
                if (region.Contains(","))
                {
                    var parts = region.Split(',');
                    fileName = parts[0];
                    recordName = parts[1];
                }
                var regionData = await MainEngine.GetEngineInstance().GetConfig<T>(fileName, recordName);
                m_generators.Add(regionData.GetKey(), GeneratorBuilder(noiseCache, regionData));
            }
        }

        protected abstract WorldRegionTerrainGenerator GeneratorBuilder(float[] noiseCache, WorldRegionData data);

        public WorldRegionTerrainGenerator GetRegionGenerator(float x, float y, float z)
        {
            var biome = GetRegionData(x, y, z);
            return m_generators[biome.Type];
        }

        protected abstract RegionData GetRegionData(float x, float y, float z);
        

        protected string GetRegionType(IDictionary parameters)
        {
            var valid = m_generators.Values.ToList();
            foreach (DictionaryEntry parameter in parameters)
            {
                var list = (valid.Where(b => b.Filter((string)parameter.Key, (float)parameter.Value))).ToList();
                if (list.Count > 0)
                {
                    valid = list;
                }
            }
            if (valid.Count > 1)
            {
                valid.Sort((left, right) => Math.Sign(left.ParamaterDistance(parameters) - right.ParamaterDistance(parameters)));
            }
            return valid[0].Name;
        }
    }
}
