using System;
using _4DMonoEngine.Core.Assets.Config;
using _4DMonoEngine.Core.Blocks;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal class ProvinceGenerator : WorldRegionTerrainGenerator
    {
        public ProvinceGenerator(float[] noiseBuffer, ProvinceData province)
            : base(noiseBuffer, province.Name, province.Layers, province.Parameters)
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
                accumulator += (int)Math.Ceiling(worldRegionLayer.Thickness *
                                    GetNoise(worldPositionX, worldRegionLayer.Id, worldPositionZ, worldPositionW,
                                        worldRegionLayer.NoiseOffset, worldRegionLayer.NoiseScale));
                if (!(accumulator > (upperBound - worldPositionY)))
                {
                    continue;
                }
                layer = worldRegionLayer;
                break;
            }
            return new Block(BlockDictionary.GetInstance().GetBlockIdForName(layer.BlockName));
        }
    }
}