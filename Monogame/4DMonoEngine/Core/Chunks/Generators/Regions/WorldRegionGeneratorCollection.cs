using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Noise;
using _4DMonoEngine.Core.Config;

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
        private readonly int m_centroidSampleScale; // 16;
        protected readonly int BiomeSampleRescale; // 512;

        private readonly Dictionary<string, WorldRegionTerrainGenerator> m_generators;
        private readonly CellNoise m_cellNoise;
        protected readonly SimplexNoise SimplexNoise;
        protected readonly SimplexNoise SimplexNoise2;
        private readonly Dictionary<ulong, RegionData> m_biomes;
        protected readonly GetHeight GetHeightFunction;
        public delegate float GetHeight(float x, float z, float w);

        protected WorldRegionGeneratorCollection(ulong seed, GetHeight getHeightFunction, IEnumerable<string> regions)
        {
            m_centroidSampleScale = MainEngine.GetEngineInstance().GeneralSettings.BiomeCentroidSampleScale;
            BiomeSampleRescale = MainEngine.GetEngineInstance().GeneralSettings.BiomeSampleRescale;

            m_cellNoise = new CellNoise(seed);
            SimplexNoise = new SimplexNoise(seed);
            SimplexNoise2 = new SimplexNoise(seed + 0x8fd3952e35bb901f); // the 2 generators just need to be offset by some arbitrary value
            m_generators = new Dictionary<string, WorldRegionTerrainGenerator>();

            foreach (var biome in regions)
            {
                var biomeData = MainEngine.GetEngineInstance().GetAsset<T>(biome);
                m_generators.Add(biome, GeneratorBuilder(SimplexNoise, biomeData));
            }

            GetHeightFunction = getHeightFunction;
            m_biomes = new Dictionary<ulong, RegionData>();
        }

        public RegionData GetRegionData(float x, float y, float z)
        {
            var data = m_cellNoise.Voroni(x / m_centroidSampleScale, y / m_centroidSampleScale, z / m_centroidSampleScale);
            if (!m_biomes.ContainsKey(data.Id))
            {
                var centroid = new Vector3(x, y, z) + data.Delta * m_centroidSampleScale;
                m_biomes[data.Id] = InternalGetRegionData(x, y, z, centroid);
            }
            return m_biomes[data.Id];
        }

        protected abstract WorldRegionTerrainGenerator GeneratorBuilder(SimplexNoise noise, WorldRegionData data);

        protected abstract RegionData InternalGetRegionData(float x, float y, float z, Vector3 centroid);

        public WorldRegionTerrainGenerator GetRegionGenerator(float x, float y, float z)
        {
            var biome = GetRegionData(x, y, z);
            return m_generators[biome.Type];
        }

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
