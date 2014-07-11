﻿using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Noise;
using _4DMonoEngine.Core.Config;

namespace _4DMonoEngine.Core.Chunks.Generators.Regions
{
    internal class BiomeGenerator : WorldRegionTerrainGenerator
    {
        public float FoliageDensity { get; private set; }
        public BiomeGenerator(SimplexNoise noise, Biome biome)
            : base(noise, biome.Name, biome.Layers, biome.Parameters)
        {
            FoliageDensity = biome.FoliageDensity;
        }

        public override Block Apply(int upperBound, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW)
        {
            var accumulator = 0.0f;
            var layer = Layers[Layers.Count - 1];
            foreach (var worldRegionLayer in Layers)
            {
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