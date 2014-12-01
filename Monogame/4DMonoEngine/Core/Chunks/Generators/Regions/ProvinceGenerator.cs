using System;
using _4DMonoEngine.Core.Assets.DataObjects;
using _4DMonoEngine.Core.Blocks;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal class ProvinceGenerator : WorldRegionTerrainGenerator
    {
        private object m_lockObject;

        public ProvinceGenerator(float[] noiseBuffer, WorldRegionData province)
            : base(noiseBuffer, province.Name, province.Layers, province.Parameters)
        {
            m_lockObject = new object();
        }

        public override Block Apply(int upperBound, int worldPositionX, int worldPositionY, int worldPositionZ)
        {
            DepthCacheEntry cache;
            lock (m_lockObject)
            {
                cache = GetDepthCache(worldPositionX, worldPositionZ);
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
                                    GetNoise(worldPositionX, worldRegionLayer.Id, worldPositionZ,
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
            return new Block(BlockDictionary.Instance.GetBlockIdForName(layer.BlockName));
        }
    }
}