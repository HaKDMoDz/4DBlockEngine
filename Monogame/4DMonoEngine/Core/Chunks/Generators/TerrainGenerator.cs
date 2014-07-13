using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Chunks.Generators.Regions;
using _4DMonoEngine.Core.Noise;
using _4DMonoEngine.Core.Random;
using Microsoft.Xna.Framework;

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

        public TerrainGenerator(int chunkSize, Block[] blocks, uint seed) 
	    {
            Seed = seed;
            m_chunkSize = chunkSize;
            m_blocks = blocks;
            Initialize();
	    }

        private async void Initialize()
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

	    public void GenerateDataForChunk(Chunk chunk, int chunkW)
	    {
           chunk.ChunkState = ChunkState.Generating;
	       var chunkWorldPosition = chunk.Position;
           var cW = chunkW * m_chunkSize;
           for (var x  = 0; x < m_chunkSize; ++x) 
		   {
                var cX = chunkWorldPosition.X * m_chunkSize + x;
			    for (var z = 0; z < m_chunkSize; ++z) 
			    {
                    var cZ = chunkWorldPosition.Z * m_chunkSize + z;
					var groundLevel = GetHeight(cX, cZ, cW);
					var detailNoise =  m_detail2.Perlin3Dfmb(cX, cZ, cW, 64, 0, 8);
                    var overhangStart = m_sealevel + MathHelper.Clamp(detailNoise * 4, -1, 1) * 2;
					for (var y = 0; y < m_chunkSize; ++y)
					{
                        var cY = chunkWorldPosition.Y * m_chunkSize + y;
					    if (!(cY < groundLevel))
					    {
					        continue;
					    }
					    Block block;
					    if (cY >= groundLevel - m_biomeThickness)
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
					    m_blocks[ChunkCache.BlockIndexByWorldPosition(cX, cY, cZ)] = block;
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
            chunk.ChunkState = ChunkState.AwaitingLighting;
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