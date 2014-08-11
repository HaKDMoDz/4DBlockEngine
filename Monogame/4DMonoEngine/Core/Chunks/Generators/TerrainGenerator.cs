using System;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Chunks.Generators.Regions;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Chunks.Generators.Structures;
using _4DMonoEngine.Core.Processors;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Noise;
using _4DMonoEngine.Core.Utils.Random;

namespace _4DMonoEngine.Core.Chunks.Generators
{
    internal sealed class TerrainGenerator 
    {
        public uint Seed { get; private set; }
        private readonly int m_chunkSize;
        private SimplexNoise3D m_elevation;
        private SimplexNoise3D m_detail;
        private SimplexNoise3D m_detail2;
        private SimplexNoise4D m_volume;
        private CellNoise3D m_voroni;
        private BiomeGeneratorCollection m_biomeGenerator;
        private ProvinceGeneratorCollection m_provinceGenerator;
        private PlantPopulator m_populator;
        private PathGenerator m_riverGenerator;
        private readonly Block[] m_blocks;

        private int m_sealevel;
        private int m_mountainHeight;
        private float m_detailScale;
        private float m_sinkHoleDepth;
        private int m_biomeThickness;
        private readonly MappingFunction m_mappingFunction;

        public TerrainGenerator(int chunkSize, Block[] blocks, uint seed, MappingFunction mappingFunction) 
	    {
            Seed = seed;
            m_mappingFunction = mappingFunction;
            m_chunkSize = chunkSize;
            m_blocks = blocks;
            InitializeAsync();
	    }

        private async void InitializeAsync()
        {
            var random = new FastRandom(Seed);
            var settings = await MainEngine.GetEngineInstance().GeneralSettings;
            m_sealevel = settings.SeaLevel;
            m_mountainHeight = settings.MountainHeight - m_sealevel;
            var rescale = settings.BiomeSampleRescale;
            m_detailScale = settings.DetailScale;
            m_sinkHoleDepth = settings.SinkHoleDepth;
            m_biomeThickness = settings.BiomeThickness;
            m_elevation = new SimplexNoise3D(random.NextUInt());
            m_detail = new SimplexNoise3D(random.NextUInt());
            m_detail2 = new SimplexNoise3D(random.NextUInt());
            m_volume = new SimplexNoise4D(random.NextUInt());
            m_voroni = new CellNoise3D(random.NextUInt());
            m_populator = new PlantPopulator(random.NextUInt(), m_blocks, m_mappingFunction);
            m_riverGenerator = new PathGenerator(random.NextUInt(), GetHeight);
            m_riverGenerator.InitializePathSystem(0, 0, 0, 256);
            m_biomeGenerator = new BiomeGeneratorCollection(random.NextUInt(), GetHeight, settings.Biomes, rescale, m_sealevel, m_mountainHeight);
            m_provinceGenerator = new ProvinceGeneratorCollection(random.NextUInt(), GetHeight, settings.Provinces, rescale, m_sealevel, m_mountainHeight);
        }

	    public void GenerateDataForChunk(int chunkX, int chunkY, int chunkZ, int chunkW)
	    {
           var cW = chunkW * m_chunkSize;
           for (var x  = 0; x < m_chunkSize; ++x) 
		   {
                var cX = chunkX + x;
			    for (var z = 0; z < m_chunkSize; ++z) 
			    {
                    var cZ = chunkZ + z;
			        var groundLevel = GetHeight(cX, cZ, cW);
					var detailNoise =  m_detail2.FractalBrownianMotion(cX, cZ, cW, 64, 0, 8);
                    var overhangStart = m_sealevel + MathHelper.Clamp(detailNoise * 4, -1, 1) * 2;
                    var biome = m_biomeGenerator.GetRegionGenerator(cX, cZ, cW);
                    var province = m_provinceGenerator.GetRegionGenerator(cX, cZ, cW);

			      /*  var data = m_populator.CalculateNearestSamplePosition(cX, cZ);
			        var color = (ushort)data.Id;*/
			        var riverData = m_riverGenerator.GetRiverData(cX, cZ);

                    if (riverData != null)
			        {
			            groundLevel = riverData.Position.Y;
			        }

					for (var y = m_chunkSize - 1; y >= 0 ; --y)
					{
                        var cY = chunkY + y;
                        Block block;

					    if (cY > groundLevel + 10)
					    {
					        block = new Block(Block.None) {LightSun = CellularLighting<Block>.MaxSun}; //MinLight };
					    }
					    else if (cY > groundLevel)
					    {
                            block = new Block(Block.None) { LightSun = CellularLighting<Block>.MinLight };
					    }
                        else if (cY >= groundLevel - m_biomeThickness)
					    {
					        if (cY > overhangStart)
					        {
					            var density = (MathHelper.Clamp(m_volume.FractalBrownianMotion(cX, cY, cZ, cW, 64, 0, 4)*3, -1, 1) + 1)*0.5f;
					            block = density > 0.125 ? biome.Apply((int) groundLevel, cX, cY, cZ, cW) : Block.Empty;
					        }
					        else
					        {
					            block = biome.Apply((int) groundLevel, cX, cY, cZ, cW);
					        }
					    }
					    else
					    {
					        block = province.Apply((int)groundLevel - m_biomeThickness, cX, cY, cZ, cW);
					    }

					    //block.Color = color;

					    if (riverData != null && cY <= groundLevel && cY >= groundLevel - 1)
					    {
                            block = new Block(BlockDictionary.GetInstance().GetBlockIdForName("Water"));
					        block.Color = (ushort)riverData.Id;
					    }

                        m_blocks[m_mappingFunction(cX, cY, cZ)] = block;
					}
                    
                    for (var y = 0; y < m_chunkSize && chunkY + y < m_sealevel; ++y)
					{		
                        var cY = chunkY + y;
					    var blockIndex = m_mappingFunction(cX, cY, cZ);
					    if (!m_blocks[blockIndex].Exists)
					        m_blocks[blockIndex] = new Block(BlockDictionary.GetInstance().GetBlockIdForName("Water"));// {LightRed = 255};
					}

			       /* if (Math.Abs(data.Delta.X - cX) < 0.01 && Math.Abs(data.Delta.Y - cZ) < 0.01)
			        {
			            m_populator.PopulateTree(cX, cZ, (int)groundLevel);
			        }*/
				}
		    }
            //TODO : query if chunk has all neighbors loaded so we can run our erosion step

            //TODO : after eroding generate structures
	    }

        private float GetHeight(float x, float z, float w)
	    {
		    var seaElevation = MathHelper.Clamp(m_elevation.FractalBrownianMotion(x, z, w, 512, 0.1715f, 4), -.5f, 0);
            var gain = MathHelper.Clamp(((MathHelper.Clamp(m_detail.FractalBrownianMotion(x, z, w, 256, 0, 2) * 5, -1, 1) + 1) / 2), 0.01f, 1);
            var offset = MathHelper.Clamp(((MathHelper.Clamp(m_detail2.FractalBrownianMotion(x, z, w, 256, 0, 2) * 5, -1, 1) + 1) / 2), 0.01f, 1);
		    var elevationValue = seaElevation < 0 ? seaElevation : m_elevation.RidgedMultiFractal (x, z, w, 128, offset, gain, 4);
            var groundLevel = elevationValue * m_mountainHeight + m_sealevel;
		    var detailApplied = groundLevel +  m_detail.FractalBrownianMotion(x, z, w, 64, 0, 8) * m_detailScale;
            if (!(groundLevel > m_sealevel + m_sinkHoleDepth))
            {
                return detailApplied;
            }
            var sinkHoleBlend = m_voroni.VoroniFbm(x, z, w, 128.0f, 0, 3);
            var sinkHoleOffset = (sinkHoleBlend > 0.08f ? 0 : (0.08f - sinkHoleBlend) / 0.08f) * m_sinkHoleDepth;	
            detailApplied -= sinkHoleOffset;
            return detailApplied;
	    }
    }
}