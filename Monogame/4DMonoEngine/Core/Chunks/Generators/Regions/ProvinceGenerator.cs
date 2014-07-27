using System;
using _4DMonoEngine.Core.Assets.Config;
using _4DMonoEngine.Core.Blocks;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal class ProvinceGenerator : WorldRegionTerrainGenerator
    {
        public ProvinceGenerator(float[] noiseBuffer, WorldRegionData province)
            : base(noiseBuffer, province.Name, province.Layers, province.Parameters)
        {}

        public override Block Apply(int upperBound, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW)
        {
            DepthCacheEntry cache;
            lock (this)
            {
                cache = GetDepthCache(worldPositionX, worldPositionZ, worldPositionW);
            }
            float accumulator;
            int index;
            WorldRegionLayer layer;
            if (cache.Depth > worldPositionY)
            {
                accumulator = cache.Depth;
                index = cache.LayerIndex;
                layer = Layers[index];
            }
            else
            {
                accumulator = 0.0f;
                index = 0;
                layer = Layers[index];   
            }
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
            cache.Depth = accumulator;
            cache.LayerIndex = index % Layers.Count;
            return new Block(BlockDictionary.GetInstance().GetBlockIdForName(layer.BlockName));
        }
    }
}