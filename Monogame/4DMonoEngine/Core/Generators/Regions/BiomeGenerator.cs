using System;
using _4DMonoEngine.Core.Assets.DataObjects;
using _4DMonoEngine.Core.Blocks;

namespace _4DMonoEngine.Core.Generators.Regions
{
    internal class BiomeGenerator : WorldRegionTerrainGenerator
    {
        public float FoliageDensity { get; private set; }
        public BiomeGenerator(float[] noiseBuffer, BiomeData biomeData)
            : base(noiseBuffer, biomeData.Name, biomeData.Layers, biomeData.Parameters)
        {
            FoliageDensity = biomeData.FoliageDensity;
        }

        public override Block Apply(int upperBound, int worldPositionX, int worldPositionY, int worldPositionZ)
        {
            var accumulator = 0;
            var layer = Layers[Layers.Count - 1];
            foreach (var worldRegionLayer in Layers)
            {
                int step;
                if (worldRegionLayer.Thickness <= 1)
                {
                    step = 1;
                }
                else
                {
                    step = (int)Math.Ceiling(worldRegionLayer.Thickness*
                                        GetNoise(worldPositionX, worldRegionLayer.Id, worldPositionZ,
                                            worldRegionLayer.NoiseOffset, worldRegionLayer.NoiseScale));
                }

                accumulator += step;
                if (accumulator < (upperBound - worldPositionY))
                {
                    continue;
                }
                layer = worldRegionLayer;
                break;
            }
            return new Block(BlockDictionary.Instance.GetBlockIdForName(layer.BlockName));
        }
    }
}