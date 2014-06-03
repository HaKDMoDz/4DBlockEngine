using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Noise;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Chunks.Generators.Biomes
{
    public abstract class BiomeGeneratorBase
    {
        protected SimplexNoise m_noise;
        protected BlockDictionary m_blockDictionary;
        protected short m_dirt;
        protected short m_stone;
        protected BiomeGeneratorBase(SimplexNoise noise)
        {
            m_noise = noise;
            m_blockDictionary = BlockDictionary.GetInstance();
            m_dirt = blockDictionary.GetBlockIdForName("dirt");
            m_stone = blockDictionary.GetBlockIdForName("stone");
        }

        public void Apply(int groundLevel, int groundOffset, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW)
        {
            if (worldPositionY > groundLevel - groundOffset)
            {
                ApplyBiome(groundLevel, groundOffset, worldPositionX, worldPositionY, worldPositionZ, worldPositionW);
            }
            else
            {
                ApplyUnderground(groundLevel - groundOffset, worldPositionX, worldPositionY, worldPositionZ, worldPositionW);
            }
        }

        public abstract void ApplyBiome(int groundLevel, int groundOffset, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW);

        public void ApplyUnderground(int groundOffset, int worldPositionX, int worldPositionY, int worldPositionZ, int worldPositionW)
        {
            var density = (MathHelper.Clamp(m_noise.Perlin4DFBM(worldPositionX, worldPositionY, worldPositionZ, worldPositionW, 16, 0, 1) * 3, -1, 1) + 1) * 0.5f;
            //TODO : rock layers and resource generation
            if (density > 0)
            {
                ChunkCache.Blocks[ChunkCache.BlockIndexByWorldPosition(worldPositionX, worldPositionY, worldPositionZ)].Type = m_stone;
            }
            else
            {
                ChunkCache.Blocks[ChunkCache.BlockIndexByWorldPosition(worldPositionX, worldPositionY, worldPositionZ)].Type = m_dirt;
            }
        }
    }
}