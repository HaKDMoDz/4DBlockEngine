using System;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Noise;
using Microsoft.Xna.Framework;


namespace _4DMonoEngine.Core.Chunks.Generators.Biomes
{
    /// <summary>
    /// Antartic tundra generator.
    /// </summary>
    public class PolarGenerator : BiomeGeneratorBase
    {
        protected short m_ice;

        public PolarGenerator(SimplexNoise noise, BlockDictionary blockDictionary)
            : base(noise, blockDictionary)
        {
            m_ice = blockDictionary.GetBlockIdForName("ice");
        }

        public override void ApplyBiome(int groundLevel, int groundOffset, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW)
        {
            if(worldPositionY == groundLevel)
            {
                ChunkCache.Blocks[ChunkCache.BlockIndexByWorldPosition(worldPositionX, worldPositionY, worldPositionZ)].Type = m_ice;
            }
            else
            {
                ChunkCache.Blocks[ChunkCache.BlockIndexByWorldPosition(worldPositionX, worldPositionY, worldPositionZ)].Type = m_dirt;
            }
        }
    }
}