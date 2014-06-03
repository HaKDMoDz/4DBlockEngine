using System;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Noise;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Chunks.Generators.Biomes
{
    public class TropicalGenerator : BiomeGeneratorBase
    {
        protected short m_grass;
        
        public TropicalGenerator(SimplexNoise noise, BlockDictionary blockDictionary)
            : base(noise, blockDictionary)
        {
            m_grass = blockDictionary.GetBlockIdForName("grass");
        }

        public override void ApplyBiome(int groundLevel, int groundOffset, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW)
        {
            if(worldPositionY == groundLevel)
            {
                ChunkCache.Blocks[ChunkCache.BlockIndexByWorldPosition(worldPositionX, worldPositionY, worldPositionZ)].Type = m_grass;
            }
            else
            {
                ChunkCache.Blocks[ChunkCache.BlockIndexByWorldPosition(worldPositionX, worldPositionY, worldPositionZ)].Type = m_dirt;
            }
        }
    }
}