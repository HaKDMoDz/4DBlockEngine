using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using _4DMonoEngine.Core.Config;
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

        private readonly Dictionary<ushort, BlockData> m_dict;
        private readonly Dictionary<String, ushort> m_blockNameMap;
        private readonly Dictionary<string, ushort[]> m_blockTypeMap;
        private readonly Dictionary<int, HalfVector2[]> m_blockTextureMappings;

        private BlockDictionary()
        {
            m_dict = new Dictionary<ushort, BlockData>();
            m_blockNameMap = new Dictionary<string, ushort>();
            m_blockTypeMap = new Dictionary<string, ushort[]>();
            m_blockTextureMappings = new Dictionary<int, HalfVector2[]>();
            
            var typeMapping = MainEngine.GetEngineInstance().GeneralSettings.BlockTypeMap;
            var textureUnitSize = MainEngine.GetEngineInstance().GeneralSettings.BlockTileMapUnitSize;
            var textureInitializer = new Dictionary<int, Task<BlockTextureData>>();
            foreach (var strings in typeMapping)
            {
                var type = strings.Key;
                var blockNameList = strings.Value;
                var idList = new List<ushort>();
                foreach (var blockName in blockNameList)
                {
                    var blockData = MainEngine.GetEngineInstance().GetConfig<BlockData>(type, blockName).Result;
                    idList.Add(blockData.BlockId);
                    m_dict.Add(blockData.BlockId, blockData);
                    m_blockNameMap.Add(blockName, blockData.BlockId);
                    for (var faceIndex = 0; faceIndex < 6; faceIndex++)
                    {
                        var textureName = blockData.TextureNames[faceIndex];
                        textureInitializer.Add(((blockData.BlockId << 3) + faceIndex), MainEngine.GetEngineInstance().GetConfig<BlockTextureData>("textures", textureName));
                    }
                }
                m_blockTypeMap.Add(type, idList.ToArray());
            }
            foreach (var pair in textureInitializer)
            {
                var blockId = pair.Key;
                var textureData = pair.Value.Result;
                m_blockTextureMappings.Add(blockId, GetBlockTextureMapping(textureData.TileU, textureData.TileV, textureUnitSize));
            }
        }

        private HalfVector2[] GetBlockTextureMapping(float xOffset, float yOffset, float unitSize)
        {
            var mapping = new HalfVector2[4]; // contains texture mapping for the two triangles contained.
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

        public BlockData GetStaticData(ushort blockId)
        {
            return m_dict[blockId];
        }

        public HalfVector2[] GetTextureMapping(ushort blockType, FaceDirection faceDir)
        {
            return m_blockTextureMappings[(int) ((blockType << 3) + faceDir)];
        }
    }
}
