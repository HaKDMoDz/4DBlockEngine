using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Assets.Config;
using _4DMonoEngine.Core.Utils.Noise;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal class ProvinceGeneratorCollection : WorldRegionGeneratorCollection<ProvinceData>
    {

        public ProvinceGeneratorCollection(ulong seed, GetHeight getHeightFunction, IEnumerable<string> provinces,int centeiodSampleScale, int biomeSampleRescale, int seaLevel, int mountainHeight)
            : base(seed, getHeightFunction, provinces, centeiodSampleScale, biomeSampleRescale, seaLevel, mountainHeight)
        {}

        protected override WorldRegionTerrainGenerator GeneratorBuilder(SimplexNoise noise, WorldRegionData data)
        {
            return new ProvinceGenerator(noise, (ProvinceData)data);
        }

        protected override RegionData InternalGetRegionData(float x, float y, float z, Vector3 centroid)
        {
            var region = new RegionData();
            var centroidHeight = GetHeightFunction(centroid.X, centroid.Y, centroid.Z);
            var heightRatio = MathHelper.Clamp((centroidHeight - m_seaLevel) / m_mountainHeight, 0, 1);
            region.GeologicalActivity = ((MathHelper.Clamp(SimplexNoise.Perlin3Dfmb(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2);
            var parameters = new OrderedDictionary
            {
                {"Rarity", (MathHelper.Clamp(SimplexNoise2.Perlin3Dfmb(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2},
                {"Elevation", heightRatio},
                {"GeologicalActivity", region.GeologicalActivity}
            };
            region.Type = GetRegionType(parameters);
            return region;
        }
    }
}
