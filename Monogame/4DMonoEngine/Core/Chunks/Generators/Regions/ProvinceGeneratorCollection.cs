using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Assets.Config;
using _4DMonoEngine.Core.Utils.Noise;

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

        protected override RegionData GetRegionData(float x, float y, float z)
        {
            var region = new RegionData();
            var centroidHeight = GetHeightFunction(x, y, z);
            var heightRatio = MathHelper.Clamp((centroidHeight - SeaLevel) / MountainHeight, 0, 1);
            region.GeologicalActivity = ((MathHelper.Clamp(SimplexNoise.Perlin3Dfmb(x, y, z, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2);
            var parameters = new OrderedDictionary
            {
                {"Rarity", (MathHelper.Clamp(SimplexNoise2.Perlin3Dfmb(x, y, z, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2},
                {"Elevation", heightRatio},
                {"GeologicalActivity", region.GeologicalActivity}
            };
            region.Type = GetRegionType(parameters);
            return region;
        }
    }
}
