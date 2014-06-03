using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Chunks.Generators.Biomes;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common;
using _4DMonoEngine.Core.Common.Noise;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Chunks.Generators
{
    public struct BiomeData
    {
        public float Humidity;
        public float Temperature;
        public BiomeType Type;
    }

    public enum BiomeType
    {
        Desert,
        Polar,
        Taigia,
        ForestSeasonal,
        ForestEvergreen,
        Plains,
        Savanah,
        Swamp,
        Tropical,
        Rainforest
    }

    public class BiomeGeneratorCollection
    {
        public static int BiomeMinHeight = 64;
        public static int MountainHeight = 64;
        public static int CentroidSampleScale = 16;
        public static int BiomeSampleRescale = 256;

        protected Dictionary<BiomeType, BiomeGeneratorBase> m_generators;
        protected CellNoise m_biomeCellNoise;
        protected SimplexNoise m_biomeSimplexNoise;
        protected Dictionary<ulong, BiomeData> m_biomes;
        protected GetHeight m_getHeightFunction;
        public delegate float GetHeight(float x, float z, float w);

        public BiomeGeneratorCollection(ulong seed, BlockDictionary blockDictionary, GetHeight getHeightFunction)
        {
            m_biomeCellNoise = new CellNoise(seed);
            m_biomeSimplexNoise = new SimplexNoise(seed);
            m_generators = new Dictionary<BiomeType, BiomeGeneratorBase>
            {
                {BiomeType.Polar, new PolarGenerator(m_biomeSimplexNoise, blockDictionary)},
                {BiomeType.Taigia, new TaigiaGenerator(m_biomeSimplexNoise, blockDictionary)},
                {BiomeType.ForestEvergreen, new EvergreenGenerator(m_biomeSimplexNoise, blockDictionary)},
                {BiomeType.Plains, new PlainsGenerator(m_biomeSimplexNoise, blockDictionary)},
                {BiomeType.ForestSeasonal, new SeasonalGenerator(m_biomeSimplexNoise, blockDictionary)},
                {BiomeType.Swamp, new SwampGenerator(m_biomeSimplexNoise, blockDictionary)},
                {BiomeType.Desert, new DesertGenerator(m_biomeSimplexNoise, blockDictionary)},
                {BiomeType.Savanah, new SavanahGenerator(m_biomeSimplexNoise, blockDictionary)},
                {BiomeType.Tropical, new TropicalGenerator(m_biomeSimplexNoise, blockDictionary)},
                {BiomeType.Rainforest, new RainForestGenerator(m_biomeSimplexNoise, blockDictionary)}
            };
            m_getHeightFunction = getHeightFunction;
            m_biomes = new Dictionary<ulong, BiomeData>();
        }

        public BiomeGeneratorBase GetBiomeGenerator(float x, float y, float z)
        {
            var data = m_biomeCellNoise.Voroni(x / CentroidSampleScale, y / CentroidSampleScale, z / CentroidSampleScale);
            BiomeData biome;
            if (m_biomes.ContainsKey(data.id))
            {
                biome = m_biomes[data.id];
            }
            else
            {
                biome = new BiomeData();
                var centroid = new Vector3(x, y, z) + data.delta * CentroidSampleScale;
                var centroidHeight = m_getHeightFunction(centroid.X, centroid.Y, centroid.Z);
                biome.Temperature = (MathHelper.Clamp(m_biomeSimplexNoise.Perlin3DFMB(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale * 2, 0, 3) * 5, -1, 1) + 1) / 2;
                //Adjust temperature with elevation based on atmospheric pressure (http://tinyurl.com/macaquk)
                biome.Temperature *= (float)Math.Pow(1 - 0.3158078f * Math.Max(centroidHeight - BiomeMinHeight, 0) / MountainHeight, 5.25588f);
                //Humidity is biased with a curve based on temperature (http://tinyurl.com/qfc3kf7)
                biome.Humidity = ((MathHelper.Clamp(m_biomeSimplexNoise.Perlin3DFMB(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale, 0, 3) * 5, -1, 1) + 1) / 2) * MathUtilities.Bias(biome.Temperature, 0.7f);
                biome.Type = GetBiomeType(biome.Humidity, biome.Temperature);
                m_biomes[data.id] = biome;
            }
            return m_generators[biome.Type];
        }

        public BiomeType GetBiomeType(float rainfall, float temperature)
        {
            BiomeType type;
            if(temperature < 0.25)
            {
                type = BiomeType.Polar;
            }
            else if (temperature < 0.5)
            {
                if (rainfall < 0.25)
                {
                    type = BiomeType.Taigia;
                }
                else
                {
                    type = BiomeType.ForestEvergreen;
                }
            }
            else if (temperature < 0.75)
            {
                if (rainfall < 0.25)
                {
                    type = BiomeType.Plains;
                }
                else if (rainfall < 0.5)
                {
                    type = BiomeType.ForestSeasonal;
                }
                else
                {
                    type = BiomeType.Swamp;
                }
            }
            else
            {
                if (rainfall < 0.25)
                {
                    type = BiomeType.Desert;
                }
                else if (rainfall < 0.5)
                {
                    type = BiomeType.Savanah;
                }
                else if (rainfall < 0.75)
                {
                    type = BiomeType.Tropical;
                }
                else
                {
                    type = BiomeType.Rainforest;
                }
            }
            return type;
        }
    }
}
