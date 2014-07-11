using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Noise;
using _4DMonoEngine.Core.Config;
using _4DMonoEngine.Core.Utils;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal class BiomeGeneratorCollection : WorldRegionGeneratorCollection<BiomeData>
    {
        private readonly int m_sealevel; // 64;
        private readonly int m_mountainHeight; // 64;

        public BiomeGeneratorCollection(ulong seed, GetHeight getHeightFunction, IEnumerable<string> biomes)
            : base(seed, getHeightFunction, biomes)
        {
            m_sealevel = MainEngine.GetEngineInstance().GeneralSettings.SeaLevel;
            m_mountainHeight = (MainEngine.GetEngineInstance().GeneralSettings.MountainHeight - m_sealevel);
        }

        protected override WorldRegionTerrainGenerator GeneratorBuilder(SimplexNoise noise, WorldRegionData data)
        {
            return new BiomeGenerator(noise, (BiomeData)data);
        }

        protected override RegionData InternalGetRegionData(float x, float y, float z, Vector3 centroid)
        {
            var region = new RegionData();
            var centroidHeight = GetHeightFunction(centroid.X, centroid.Y, centroid.Z);
            var heightRatio = MathHelper.Clamp((centroidHeight - m_sealevel) / m_mountainHeight, 0, 1);
            region.Temperature = (MathHelper.Clamp(SimplexNoise.Perlin3Dfmb(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale, 0, 3) * 5, -1, 1) + 1) / 2;
            //Adjust temperature with elevation based on atmospheric pressure (http://tinyurl.com/macaquk)
            region.Temperature *= (float)Math.Pow(1 - 0.3158078f * heightRatio, 5.25588f);
            //Humidity is biased with a curve based on temperature (http://tinyurl.com/qfc3kf7)
            region.Humidity = ((MathHelper.Clamp(SimplexNoise2.Perlin3Dfmb(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale, 0, 3) * 5, -1, 1) + 1) / 2) * MathUtilities.Bias(region.Temperature, 0.7f);
            //Geological Activity is biased slightly by elevation
            region.GeologicalActivity = ((MathHelper.Clamp(SimplexNoise.Perlin3Dfmb(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2);
            var parameters = new OrderedDictionary
            {
                {"rarity", (MathHelper.Clamp(SimplexNoise2.Perlin3Dfmb(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2},
                {"temperature", region.Temperature},
                {"humidity", region.Humidity},
                {"elevation", heightRatio},
                {"geologicalActivity", region.GeologicalActivity}
            };
            region.Type = GetRegionType(parameters);
            return region;
        }
    }
}
