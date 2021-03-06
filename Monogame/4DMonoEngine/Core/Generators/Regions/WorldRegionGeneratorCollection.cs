﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Assets.DataObjects;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Noise;

namespace _4DMonoEngine.Core.Generators.Regions
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
        protected readonly int SeaLevel;
        protected readonly int MountainHeight;
        protected readonly int BiomeSampleRescale;

        private readonly Dictionary<string, WorldRegionTerrainGenerator> m_generators;
        protected readonly SimplexNoise2D SimplexNoiseGenerator1;
        protected readonly SimplexNoise2D SimplexNoiseGenerator2;
        protected readonly GetHeight GetHeightFunction;

        protected WorldRegionGeneratorCollection(ulong seed, GetHeight getHeightFunction, IEnumerable<string> regions, int biomeSampleRescale, int seaLevel, int mountainHeight)
        {
            BiomeSampleRescale = biomeSampleRescale;
            SeaLevel = seaLevel;
            MountainHeight = mountainHeight;

            SimplexNoiseGenerator1 = new SimplexNoise2D(seed);
            SimplexNoiseGenerator2 = new SimplexNoise2D(seed + 0x8fd3952e35bb901f); // the 2 generators just need to be offset by some arbitrary value
            m_generators = new Dictionary<string, WorldRegionTerrainGenerator>();
            InitializeAsync(seed + 0x3a98bfad, regions); //another arbitrary offset
            GetHeightFunction = getHeightFunction;
        }

        private async void InitializeAsync(ulong seed, IEnumerable<string> regions)
        {
            var simplexNoise = new SimplexNoise3D(seed);
            var noiseCache = new float[1000];
            for (var i = 0; i < noiseCache.Length; i++)
            {
                noiseCache[i] = (MathHelper.Clamp(simplexNoise.FractalBrownianMotion(i, 0, 0, 64, 0, 2) * 4, -1, 1) + 1) * 0.5f;
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
                var regionData = MainEngine.GetEngineInstance().GetConfig<T>(fileName, recordName);
                m_generators.Add((await regionData).GetKey(), GeneratorBuilder(noiseCache, (await regionData)));
            }
        }

        protected abstract WorldRegionTerrainGenerator GeneratorBuilder(float[] noiseCache, WorldRegionData data);

        public WorldRegionTerrainGenerator GetRegionGenerator(float x, float y)
        {
            var biome = GetRegionData(x, y);
            return m_generators[biome.Type];
        }

        protected abstract RegionData GetRegionData(float x, float y);
        

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
