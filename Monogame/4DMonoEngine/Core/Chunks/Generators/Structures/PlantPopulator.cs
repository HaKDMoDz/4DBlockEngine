using System.Collections.Generic;
using System.Linq;
using _4DMonoEngine.Core.Blocks;

namespace _4DMonoEngine.Core.Chunks.Generators.Structures
{
    public class PlantPopulator
    {
        private readonly Dictionary<string, List<PlantData>> m_biomeMapping;
        private readonly IEnumerable<Block> m_blocks; 
        public PlantPopulator(IEnumerable<Block> blocks)
        {
            m_blocks = blocks;
            BlockDictionary blockDictionary = BlockDictionary.GetInstance();
            var trunks = blockDictionary.GetBlockIdsForType("trunk");
            var leaves = blockDictionary.GetBlockIdsForType("leaf");
            var flowers = blockDictionary.GetBlockIdsForType("flower");
            var grasses = blockDictionary.GetBlockIdsForType("grass");
            m_biomeMapping = new Dictionary<string, List<PlantData>>();
            //we iterate over leaves because a trunk must have leaves, but a leaf may not have a trunk (which would make it a bush)
            foreach (var leaf in leaves)
            {
                var leafBlock = blockDictionary.GetStaticData(leaf);
                var blockStructureId = leafBlock.BlockStructureId;
                string biome = leafBlock.Biome;
                var validTrunks = trunks.Where(trunk => blockDictionary.GetStaticData(trunk).BlockStructureId == blockStructureId);
                var enumerable = validTrunks as ushort[] ?? validTrunks.ToArray();
                if (enumerable.Any())
                {
                    foreach (var trunk in enumerable)
                    {
                       GetPlantDataListForBiome(biome).Add(new PlantData(PlantType.Tree, trunk, leaf));
                    }
                }
                else
                {
                    GetPlantDataListForBiome(biome).Add(new PlantData(PlantType.Bush, 0, leaf));
                }
            }
        }

        private List<PlantData> GetPlantDataListForBiome(string biome)
        {
            if (!m_biomeMapping.ContainsKey(biome))
            {
                m_biomeMapping[biome] = new List<PlantData>();
            }
            return m_biomeMapping[biome];
        }



        public void PopulateTree(int worldPositionX, int worldPositionZ, int groundLevel)
        {
           /* var trunkHeight = 5;
            var trunkOffset = ChunkCache.BlockIndexByWorldPosition(worldPositionX, worldPositionZ);

            for (var y = trunkHeight + groundLevel; y > groundLevel; y--)
            {
                ChunkCache.Blocks[trunkOffset + y] = new Block(BlockType.Tree);
            }

            var radius = 3;
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
        
        protected enum PlantType
        {
            Grass,
            Flower,
            Bush,
            Tree
        }

        protected struct PlantData
        {
            public readonly PlantType Type;
            public readonly ushort TrunkId;
            public readonly ushort LeafId;
            public PlantData(PlantType plantType, ushort trunk, ushort leaf)
            {
                Type = plantType;
                TrunkId = trunk;
                LeafId = leaf;
            }
        }



    }
}