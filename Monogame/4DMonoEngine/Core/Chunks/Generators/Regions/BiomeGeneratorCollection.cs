using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Assets.Config;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Noise;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal class BiomeGeneratorCollection : WorldRegionGeneratorCollection<BiomeData>
    {

        public BiomeGeneratorCollection(ulong seed, GetHeight getHeightFunction, IEnumerable<string> biomes, int centeiodSampleScale, int biomeSampleRescale, int seaLevel, int mountainHeight)
            : base(seed, getHeightFunction, biomes, centeiodSampleScale, biomeSampleRescale, seaLevel, mountainHeight)
        {}

        protected override WorldRegionTerrainGenerator GeneratorBuilder(SimplexNoise noise, WorldRegionData data)
        {
            return new BiomeGenerator(noise, (BiomeData)data);
        }

        protected override RegionData InternalGetRegionData(float x, float y, float z, Vector3 centroid)
        {
            var region = new RegionData();
            var centroidHeight = GetHeightFunction(centroid.X, centroid.Y, centroid.Z);
            var heightRatio = MathHelper.Clamp((centroidHeight - m_seaLevel) / m_mountainHeight, 0, 1);
            region.Temperature = (MathHelper.Clamp(SimplexNoise.Perlin3Dfmb(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale, 0, 3) * 5, -1, 1) + 1) / 2;
            //Adjust temperature with elevation based on atmospheric pressure (http://tinyurl.com/macaquk)
            region.Temperature *= (float)Math.Pow(1 - 0.3158078f * heightRatio, 5.25588f);
            //Humidity is biased with a curve based on temperature (http://tinyurl.com/qfc3kf7)
            region.Humidity = ((MathHelper.Clamp(SimplexNoise2.Perlin3Dfmb(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale, 0, 3) * 5, -1, 1) + 1) / 2) * MathUtilities.Bias(region.Temperature, 0.7f);
            //Geological Activity is biased slightly by elevation
            region.GeologicalActivity = ((MathHelper.Clamp(SimplexNoise.Perlin3Dfmb(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2);
            var parameters = new OrderedDictionary
            {
                {"Rarity", (MathHelper.Clamp(SimplexNoise2.Perlin3Dfmb(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2},
                {"Temperature", region.Temperature},
                {"Humidity", region.Humidity},
                {"Elevation", heightRatio},
                {"GeologicalActivity", region.GeologicalActivity}
            };
            region.Type = GetRegionType(parameters);
            return region;
        }
    }
}
