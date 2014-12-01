using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Assets.DataObjects;
using _4DMonoEngine.Core.Utils;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal class ProvinceGeneratorCollection : WorldRegionGeneratorCollection<ProvinceData>
    {

        public ProvinceGeneratorCollection(ulong seed, GetHeight getHeightFunction, IEnumerable<string> provinces, int biomeSampleRescale, int seaLevel, int mountainHeight)
            : base(seed, getHeightFunction, provinces, biomeSampleRescale, seaLevel, mountainHeight)
        {}

        protected override WorldRegionTerrainGenerator GeneratorBuilder(float[] noiseCache, WorldRegionData data)
        {
            return new ProvinceGenerator(noiseCache, (ProvinceData)data);
        }

        protected override RegionData GetRegionData(float x, float y)
        {
            var region = new RegionData();
            var centroidHeight = GetHeightFunction(x, y);
            var heightRatio = MathHelper.Clamp((centroidHeight - SeaLevel) / MountainHeight, 0, 1);
            region.GeologicalActivity = ((MathHelper.Clamp(SimplexNoiseGenerator1.FractalBrownianMotion(x, y, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2);
            var parameters = new OrderedDictionary
            {
                {"Rarity", (MathHelper.Clamp(SimplexNoiseGenerator2.FractalBrownianMotion(x, y, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2},
                {"Elevation", heightRatio},
                {"GeologicalActivity", region.GeologicalActivity}
            };
            region.Type = GetRegionType(parameters);
            return region;
        }
    }
}
