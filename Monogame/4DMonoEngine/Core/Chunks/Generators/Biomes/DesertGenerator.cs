using System;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Noise;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Chunks.Generators.Biomes
{
    public class DesertGenerator : BiomeGeneratorBase
    {
        protected short m_sand;

        public DesertGenerator(SimplexNoise noise, BlockDictionary blockDictionary)
            : base(noise, blockDictionary)
        {
            m_sand = blockDictionary.GetBlockIdForName("sand");
        }

        public override void ApplyBiome(int groundLevel, int groundOffset, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW)
        {
            ChunkCache.Blocks[ChunkCache.BlockIndexByWorldPosition(worldPositionX, worldPositionY, worldPositionZ)].Type = m_sand;
        }
    }
}
