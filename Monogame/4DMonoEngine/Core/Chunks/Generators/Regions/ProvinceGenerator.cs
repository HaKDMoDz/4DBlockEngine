using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Noise;
using _4DMonoEngine.Core.Config;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal class ProvinceGenerator : WorldRegionTerrainGenerator
    {
        public ProvinceGenerator(SimplexNoise noise, Province biome) 
            : base( noise, biome.Name, biome.Layers, biome.Parameters)
        {}

        public override Block Apply(int upperBound, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW)
        {
            var accumulator = 0.0f;
            var layer = Layers[Layers.Count - 1];
            var index = 0;
            //const max depth because eventually this loop is dumb
            while (index < 100)
            {
                var worldRegionLayer = Layers[index++ % Layers.Count];
                accumulator += (worldRegionLayer.Thickness) * Noise.Perlin4Dfbm(worldPositionX, worldRegionLayer.Id, worldPositionZ, worldPositionW,worldRegionLayer.NoiseScale,worldRegionLayer.NoiseOffset, 2);
                if (accumulator > (upperBound - worldPositionY))
                {
                    layer = worldRegionLayer;
                    break;
                }
            }
            return new Block(BlockDictionary.GetInstance().GetBlockIdForName(layer.BlockName), 0);
        }
    }
}