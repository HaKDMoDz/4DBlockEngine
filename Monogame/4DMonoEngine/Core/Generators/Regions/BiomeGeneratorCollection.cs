using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Assets.DataObjects;
using _4DMonoEngine.Core.Utils;

namespace _4DMonoEngine.Core.Generators.Regions
{
    internal class BiomeGeneratorCollection : WorldRegionGeneratorCollection<BiomeData>
    {

        public BiomeGeneratorCollection(ulong seed, GetHeight getHeightFunction, IEnumerable<string> biomes, int biomeSampleRescale, int seaLevel, int mountainHeight)
            : base(seed, getHeightFunction, biomes, biomeSampleRescale, seaLevel, mountainHeight)
        {}

        protected override WorldRegionTerrainGenerator GeneratorBuilder(float[] noiseCache, WorldRegionData data)
        {
            return new BiomeGenerator(noiseCache, (BiomeData)data);
        }

        protected override RegionData GetRegionData(float x, float y)
        {
            var region = new RegionData();
            var centroidHeight = GetHeightFunction(x, y);
            var heightRatio = MathHelper.Clamp((centroidHeight - SeaLevel) / MountainHeight, 0, 1);
            region.Temperature = (MathHelper.Clamp(SimplexNoiseGenerator1.FractalBrownianMotion(x, y, BiomeSampleRescale, 0, 3) * 5, -1, 1) + 1) / 2;
            //Adjust temperature with elevation based on atmospheric pressure (http://tinyurl.com/macaquk)
            region.Temperature *= (float)Math.Pow(1 - 0.3158078f * heightRatio, 5.25588f);
            //Humidity is biased with a curve based on temperature (http://tinyurl.com/qfc3kf7)
            region.Humidity = ((MathHelper.Clamp(SimplexNoiseGenerator2.FractalBrownianMotion(x, y, BiomeSampleRescale, 0, 3) * 5, -1, 1) + 1) / 2) * MathUtilities.Bias(region.Temperature, 0.7f);
            //Geological Activity is biased slightly by elevation
            region.GeologicalActivity = ((MathHelper.Clamp(SimplexNoiseGenerator1.FractalBrownianMotion(x, y, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2);
            var parameters = new OrderedDictionary
            {
                {"Rarity", (MathHelper.Clamp(SimplexNoiseGenerator2.FractalBrownianMotion(x, y, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2},
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
