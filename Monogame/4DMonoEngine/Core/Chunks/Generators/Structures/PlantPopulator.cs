using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Noise;

namespace _4DMonoEngine.Core.Chunks.Generators.Structures
{
    //TODO : refactor this into a proper structure system
    public class PlantPopulator
    {
        private readonly Block[] m_blocks;
        private readonly MappingFunction m_mappingFunction;
        private ushort m_treeId;
        private readonly CellNoise2D m_cellNoise;
        private readonly SimplexNoise2D m_densityFunction;
        private float m_sampleScale;
        private float m_minSampleScale;

        public PlantPopulator(ulong seed, Block[] blocks, MappingFunction mappingFunction)
        {
            m_blocks = blocks;
            m_mappingFunction = mappingFunction;
            m_treeId = BlockDictionary.GetInstance().GetBlockIdForName("Sand");
            m_densityFunction = new SimplexNoise2D(seed);
            m_cellNoise = new CellNoise2D(seed);
            m_sampleScale = 64;
            m_minSampleScale = 4;
        }

         public CellNoise2D.VoroniData CalculateNearestSamplePosition(float x, float z)
         {
             var scaleAdjustment = (int)MathHelper.Clamp(m_densityFunction.FractalBrownianMotion(x, z, 512, 0, 4) * 5, -1, 1);
             var finalScale = m_sampleScale - scaleAdjustment*(m_sampleScale - m_minSampleScale);
             var data = m_cellNoise.Voroni(x, z, finalScale);
             var centroid = new Vector2
             {
                 X = x + (float) Math.Round(data.Delta.X*finalScale, MidpointRounding.ToEven),
                 Y = z + (float) Math.Round(data.Delta.Y*finalScale, MidpointRounding.ToEven)
             };
             data.Delta = centroid;
             return data;
         }

        public void PopulateTree(int worldPositionX, int worldPositionZ, int groundLevel)
        {
            var trunkHeight = 15 + groundLevel;
            for (var y = groundLevel; y < trunkHeight; y++)
            {
                m_blocks[m_mappingFunction(worldPositionX, y, worldPositionZ)] = new Block(m_treeId);
            }

           /* var radius = 3;
            for (var i = -radius; i < radius; i++)
            {
                for (var j = -radius; j < radius; j++)
                {
                    var offset = ChunkCache.BlockIndexByWorldPosition(worldPositionX + i, worldPositionZ + j);
                    for (var k = radius * 2; k > 0; k--)
                    {
                        ChunkCache.Blocks[offset + k + trunkHeight + 1] = new Block(BlockType.Leaves);
                    }
                }
            }*/
        }
    }
}