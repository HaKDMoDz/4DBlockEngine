using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Chunks.Generators.Regions;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Noise;
using _4DMonoEngine.Core.Utils.Random;

namespace _4DMonoEngine.Core.Chunks.Generators
{
    internal sealed class TerrainGenerator 
    {
        public uint Seed { get; private set; }
        private readonly int m_chunkSize;
        private SimplexNoise m_elevation;
        private SimplexNoise m_detail;
        private SimplexNoise m_detail2;
        private SimplexNoise m_volume;
        private CellNoise m_voroni;
        private BiomeGeneratorCollection m_biomeGenerator;
        private ProvinceGeneratorCollection m_provinceGenerator;
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
            m_elevation = new SimplexNoise(random.NextUInt());
            m_detail = new SimplexNoise(random.NextUInt());
            m_detail2 = new SimplexNoise(random.NextUInt());
            m_volume = new SimplexNoise(random.NextUInt());
            m_voroni = new CellNoise(random.NextUInt());
            m_sealevel = settings.SeaLevel;
            m_mountainHeight = settings.MountainHeight - m_sealevel;
            var centroidScale = settings.BiomeCentroidSampleScale;
            var rescale = settings.BiomeSampleRescale;
            m_biomeGenerator = new BiomeGeneratorCollection(random.NextUInt(), GetHeight, settings.Biomes, centroidScale, rescale, m_sealevel, m_mountainHeight);
            m_provinceGenerator = new ProvinceGeneratorCollection(random.NextUInt(), GetHeight, settings.Provinces, centroidScale, rescale, m_sealevel, m_mountainHeight);
            m_detailScale = settings.DetailScale;
            m_sinkHoleDepth = settings.SinkHoleDepth;
            m_biomeThickness = settings.BiomeThickness;
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
					var detailNoise =  m_detail2.Perlin3Dfmb(cX, cZ, cW, 64, 0, 8);
                    var overhangStart = m_sealevel + MathHelper.Clamp(detailNoise * 4, -1, 1) * 2;
					for (var y = 0; y < m_chunkSize; ++y)
					{
                        var cY = chunkY + y;
                        Block block;

					    if (cY > groundLevel + 10)
					    {
                            block = new Block(0) {LightSun = Chunk.MaxSunValue};
					    }
					    else if (cY > groundLevel)
					    {
					        block = new Block(0);
					    }
                        else if (cY >= groundLevel - m_biomeThickness)
					    {
					        var biome = m_biomeGenerator.GetRegionGenerator(cX, cZ, cW);
					        if (cY > overhangStart)
					        {
					            var density = (MathHelper.Clamp(m_volume.Perlin4Dfbm(cX, cY, cZ, cW, 64, 0, 4)*3, -1, 1) + 1)*0.5f;
					            block = density > 0.125 ? biome.Apply((int) groundLevel, cX, cY, cZ, cW) : Block.Empty;
					        }
					        else
					        {
					            block = biome.Apply((int) groundLevel, cX, cY, cZ, cW);
					        }
					    }
					    else
					    {
					        var province = m_provinceGenerator.GetRegionGenerator(cX, cZ, cW);
					        block = province.Apply((int)groundLevel - m_biomeThickness, cX, cY, cZ, cW);
					    }
                        m_blocks[m_mappingFunction(cX, cY, cZ)] = block;
					}
                    //TODO : fill with water
                    /*
                    for (var y = 0; y < m_chunkSize && chunkWorldPosition.Y * m_chunkSize + y < SeaLevel; ++y)
					{						
						if(chunkData[x, y, z, w] == 0)
						{
							chunkData[x, y, z, w] = 3;
						}
					}*/
				}
		    }
            //TODO : query if chunk has all neighbors loaded so we can run our erosion step

            //TODO : after eroding generate structures
	    }

        private float GetHeight(float x, float z, float w)
	    {
		    var seaElevation = MathHelper.Clamp(m_elevation.Perlin3Dfmb(x, z, w, 512, 0.1715f, 4), -.5f, 0);
            var gain = MathHelper.Clamp(((MathHelper.Clamp(m_detail.Perlin3Dfmb(x, z, w, 256, 0, 2) * 5, -1, 1) + 1) / 2), 0.01f, 1);
            var offset = MathHelper.Clamp(((MathHelper.Clamp(m_detail2.Perlin3Dfmb(x, z, w, 256, 0, 2) * 5, -1, 1) + 1) / 2), 0.01f, 1);
		    var elevationValue = seaElevation < 0 ? seaElevation : m_elevation.RidgedMultiFractal3D (x, z, w, 128, offset, gain, 4);
            var groundLevel = elevationValue * m_mountainHeight + m_sealevel;
		    var detailApplied = groundLevel +  m_detail.Perlin3Dfmb(x, z, w, 64, 0, 8) * m_detailScale;
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