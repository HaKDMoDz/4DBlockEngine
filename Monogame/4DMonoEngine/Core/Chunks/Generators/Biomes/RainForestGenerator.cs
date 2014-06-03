using System;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Noise;

namespace _4DMonoEngine.Core.Chunks.Generators.Biomes
{
    public class RainForestGenerator : BiomeGeneratorBase
    {
        public RainForestGenerator(SimplexNoise noise, BlockDictionary blockDictionary)
            : base(noise, blockDictionary)
        { }

        public override void ApplyBiome(int groundLevel, int groundOffset, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW)
        {
            ChunkCache.Blocks[ChunkCache.BlockIndexByWorldPosition(worldPositionX, worldPositionY, worldPositionZ)].Type = m_dirt;
        }
    }
}