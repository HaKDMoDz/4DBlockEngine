using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using _4DMonoEngine.Core.Enums;

namespace _4DMonoEngine.Core.Blocks
{
    public class BlockDictionary
    {
        public static BlockDictionary GetInstance()
        {
            return s_instance ?? (s_instance = new BlockDictionary());
        }

        private static BlockDictionary s_instance;

        private readonly Dictionary<ushort, StaticBlockData> m_dict;
        private readonly Dictionary<String, ushort> m_blockNameMap;
        private readonly Dictionary<ushort, BlockTexture> m_texturDictonary;
        private readonly Dictionary<string, ushort[]> m_blockTypeMap;
        private readonly Dictionary<int, HalfVector2[]> m_blockTextureMappings;

        private BlockDictionary()
        {
            m_dict = new Dictionary<ushort, StaticBlockData>();
            m_blockNameMap = new Dictionary<string, ushort>();
            m_blockTypeMap = new Dictionary<string, ushort[]>();
            m_blockTextureMappings = new Dictionary<int, HalfVector2[]>();
        }

        public void LoadData()
        {
            
        }

        private HalfVector2[] GetBlockTextureMapping(float xOffset, float yOffset, float unitSize)
        {
            var mapping = new HalfVector2[6]; // contains texture mapping for the two triangles contained.
            mapping[0] = new HalfVector2(xOffset, yOffset); // 0,0
            mapping[1] = new HalfVector2(xOffset + unitSize, yOffset); // 1,0
            mapping[2] = new HalfVector2(xOffset, yOffset + unitSize); // 0, 1
            mapping[4] = new HalfVector2(xOffset + unitSize, yOffset + unitSize); // 1,1
            return mapping;
        }

        public bool IsValidBlockName(string blockName)
        {
            return m_blockNameMap.ContainsKey(blockName);
        }

        public ushort GetBlockIdForName(string blockName)
        {
            return m_blockNameMap[blockName];
        }

        public ushort[] GetBlockIdsForType(string type)
        {
            return m_blockTypeMap[type];
        }

        public bool IsValidBlockId(ushort blockId)
        {
            return m_dict.ContainsKey(blockId);
        }

        public StaticBlockData GetStaticData(ushort blockId)
        {
            return m_dict[blockId];
        }

        public HalfVector2[] GetTextureMapping(ushort blockType, FaceDirection faceDir)
        {
            return m_blockTextureMappings[(int) ((blockType << 3) + faceDir)];
        }
    }



    public struct StaticBlockData
    {
        public string Biome;
        public uint BlockStructureId;
        public float Opacity;
        public float Durability;
        public float Elasticity;
        public float Adhesion;
        public float Mass;
    }

    public struct BlockTexture
    {
        private readonly int[] m_faceTextureIds;
        public BlockTexture(int[] faceTextureIds)
        {
            m_faceTextureIds = faceTextureIds;
        }

        public int GetTextureForFace(FaceDirection facing)
        {
            return m_faceTextureIds[(int) facing];
        }
    }
}
