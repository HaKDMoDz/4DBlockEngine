using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Noise;
using _4DMonoEngine.Core.Common.Random;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Common.Vector;

namespace _4DMonoEngine.Core.Chunks.Generators
{
    internal class TerrainGenerator 
    {
        public int Seed {get; private set;}
	    protected int m_chunkSize;
        protected readonly SimplexNoise m_elevation;
        protected readonly SimplexNoise m_detail;
        protected readonly SimplexNoise m_detail2;
        protected readonly SimplexNoise m_volume;
        protected readonly CellNoise m_voroni;
        protected readonly BiomeGeneratorCollection m_biomeGenerator;

	    public const float SeaLevel = 64;
        public const float TemperatureOffset = 0;
        public const float MountainHeight = 64;
        public const float DetailScale = 16;
        public const float SinkHoleDepth = 8;

        public TerrainGenerator(BlockDictionary blockDictionary, int chunkSize, int seed) 
	    {
            var random = new FastRandom(seed);
            Seed = seed;
            m_elevation = new SimplexNoise(random.NextUInt());
	        m_detail = new SimplexNoise (random.NextUInt());
	        m_detail2 = new SimplexNoise (random.NextUInt());
	        m_volume = new SimplexNoise (random.NextUInt());
            m_voroni = new CellNoise(random.NextUInt());
            m_biomeGenerator = new BiomeGeneratorCollection(random.NextUInt(), blockDictionary, GetHeight);
		    m_chunkSize = chunkSize;
	    }

	    public void GenerateDataForChunk(Vector3Int chunkWorldPosition, int chunkW)
        {
           var cW = chunkW * m_chunkSize;
           for (var x  = 0; x < m_chunkSize; ++x) 
		    {
                var cX = chunkWorldPosition.X * m_chunkSize + x;
			    for (var z = 0; z < m_chunkSize; ++z) 
			    {
                    var cZ = chunkWorldPosition.Z * m_chunkSize + z;
					var groundLevel = GetHeight(cX, cZ, cW);
					var detailNoise =  m_detail2.Perlin3DFMB(cX, cZ, cW, 64, 0, 8);
					var biome = m_biomeGenerator.GetBiomeGenerator(cX, cZ, cW);				
					var overhangStart = SeaLevel + MathHelper.Clamp(detailNoise * 4, -1, 1) * 2;
					for (var y = 0; y < m_chunkSize; ++y)
					{
                        var cY = chunkWorldPosition.Y * m_chunkSize + y;
						if(cY < 2)
						{
                            biome.Apply((int)groundLevel, (int)(detailNoise * 2 + 2), cX, cY, cZ, cW);
						}
						else if(cY < groundLevel) 
						{
							if(cY > overhangStart)
							{
                                var density = (MathHelper.Clamp(m_volume.Perlin4DFBM(cX, cY, cZ, cW, 64, 0, 4) * 3, -1, 1) + 1) * 0.5f;
								if(density > 0.125)
								{
                                    biome.Apply((int)groundLevel, (int)(detailNoise * 2 + 2), cX, cY, cZ, cW);
								}
							}
							else
							{
                                biome.Apply((int)groundLevel, (int)(detailNoise * 2 + 2), cX, cY, cZ, cW);
							}
						}
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
	    }

	    public float GetHeight(float x, float z, float w)
	    {
		    var seaElevation = MathHelper.Clamp(m_elevation.Perlin3DFMB(x, z, w, 512, 0.1715f, 4), -.5f, 0);
            var elevationGain = MathHelper.Clamp(((MathHelper.Clamp(m_detail.Perlin3DFMB(x, z, w, 256, 0, 2) * 5, -1, 1) + 1) / 2), 0.01f, 1);
            var elevation0ffset = MathHelper.Clamp(((MathHelper.Clamp(m_detail2.Perlin3DFMB(x, z, w, 256, 0, 2) * 5, -1, 1) + 1) / 2), 0.01f, 1);
		    var elevationValue = seaElevation < 0 ? seaElevation : m_elevation.RidgedMultiFractal3D (x, z, w, 128, elevation0ffset, elevationGain, 4);
		    var groundLevel = elevationValue * MountainHeight + SeaLevel;
		    var detailApplied = groundLevel +  m_detail.Perlin3DFMB(x, z, w, 64, 0, 8) * DetailScale;
		    if(groundLevel > SeaLevel + SinkHoleDepth)
		    {
                var sinkHoleBlend = m_voroni.VoroniFBM(x, z, w, 128.0f, 0, 3);
			    var sinkHoleOffset = (sinkHoleBlend > 0.08f ? 0 : (0.08f - sinkHoleBlend) / 0.08f) * SinkHoleDepth;	
			    detailApplied -= sinkHoleOffset;
		    }
		    return detailApplied;
	    }
    }
}