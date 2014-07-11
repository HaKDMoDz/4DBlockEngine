using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Noise;
using _4DMonoEngine.Core.Config;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal class ProvinceGeneratorCollection : WorldRegionGeneratorCollection<Province>
    {
        private readonly int m_sealevel; // 64;
        private readonly int m_mountainHeight; // 64;

        public ProvinceGeneratorCollection(ulong seed, GetHeight getHeightFunction, IEnumerable<string> provinces)
            : base(seed, getHeightFunction, provinces)
        {
            m_sealevel = MainEngine.GetEngineInstance().GeneralSettings.SeaLevel;
            m_mountainHeight = (MainEngine.GetEngineInstance().GeneralSettings.MountainHeight - m_sealevel);
        }

        protected override WorldRegionTerrainGenerator GeneratorBuilder(SimplexNoise noise, WorldRegionData data)
        {
            return new ProvinceGenerator(noise, (Province)data);
        }

        protected override RegionData InternalGetRegionData(float x, float y, float z, Vector3 centroid)
        {
            var region = new RegionData();
            var centroidHeight = GetHeightFunction(centroid.X, centroid.Y, centroid.Z);
            var heightRatio = MathHelper.Clamp((centroidHeight - m_sealevel) / m_mountainHeight, 0, 1);
            region.GeologicalActivity = ((MathHelper.Clamp(SimplexNoise.Perlin3Dfmb(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2);
            var parameters = new OrderedDictionary
            {
                {"rarity", (MathHelper.Clamp(SimplexNoise2.Perlin3Dfmb(centroid.X, centroid.Y, centroid.Z, BiomeSampleRescale * 8, 0, 5) * 5, -1, 1) + 1) / 2},
                {"elevation", heightRatio},
                {"geologicalActivity", region.GeologicalActivity}
            };
            region.Type = GetRegionType(parameters);
            return region;
        }
    }
}
