﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;


public class Generator
{
	protected Queue<Vector4> requestedChunks;
	protected Queue<GeneratorResponse> generatedChunks;
	protected Thread generatorThread;
	protected Dictionary<long, Vector3[,,]> heightCubes;
	protected Dictionary<ulong, BiomeData> m_biomes;
	protected World m_world;
	protected int chunkSize;
	protected HashSet<long> pending;
	protected AutoResetEvent waitHandle;
	protected float[] biasTable;

	protected Noise elevation = new Noise((ulong)Random.Range(0, int.MaxValue));
	protected Noise detail = new Noise ((ulong)Random.Range(0, int.MaxValue));
	protected Noise detail2 = new Noise ((ulong)Random.Range(0, int.MaxValue));
	protected Noise volume = new Noise ((ulong)Random.Range(0, int.MaxValue));
	protected Noise biomeNoise = new Noise ((ulong)Random.Range(0, int.MaxValue));

	protected static float s_seaLevel = 64;
	protected static float s_temperatureOffset = 0;
	protected static float s_mountainHeight = 64;
	protected static float s_detailScale = 16;
	protected static float s_sinkHoleDepth = 8;

	public Generator(World world) 
	{
		pending = new HashSet<long>();
		heightCubes = new Dictionary<long, Vector3[,,]> ();
		m_biomes = new Dictionary<ulong, BiomeData>();
		m_world = world;
		chunkSize = world.chunkSize;
		waitHandle = new AutoResetEvent (false);
		requestedChunks = new Queue<Vector4> ();
		generatedChunks = new Queue<GeneratorResponse> ();
		generatorThread = new Thread (ThreadLoop);
		generatorThread.Start();
		biasTable = new float[101];
		for(int i = 0; i < 101; ++i)
		{
			biasTable[i] = Mathf.Log(i * 0.01f) / Mathf.Log(0.5f);
		}
	}
	
	public void Update () 
	{
		if(generatedChunks.Count > 0)
		{
			GeneratorResponse response = generatedChunks.Dequeue();
			m_world.SetChunkData(response);
			if(pending.Contains(response.chunkHash))
			{
				pending.Remove(response.chunkHash);
			}
		}
	}

	public void Enqueue(Vector4 chunk, long chunkHash)
	{
		if(!pending.Contains(chunkHash))
		{
			pending.Add(chunkHash);
			bool setHandle = requestedChunks.Count == 0;
			requestedChunks.Enqueue (chunk);
			if(setHandle)
			{
				waitHandle.Set(); 
			}
		}
	}

	protected void ThreadLoop()
	{
		while(true)
		{
			if(requestedChunks.Count > 0)
			{
				Vector4 chunkIndex = requestedChunks.Dequeue(); 
				byte[,,,] chunkData = GenerateDataForChunk((int)chunkIndex.x, (int)chunkIndex.y, (int)chunkIndex.z, (int)chunkIndex.w);
				generatedChunks.Enqueue(new GeneratorResponse(m_world.Hash(chunkIndex), chunkIndex, chunkData));
			}
			else 
			{
				waitHandle.WaitOne();
			}
		}
	}

	protected byte[,,,] GenerateDataForChunk(int chunkX, int chunkY, int chunkZ, int chunkW)
	{
		byte[,,,] chunkData = new byte[chunkSize, chunkSize, chunkSize, chunkSize];
		Vector3[,,] sampleCube = GetSampleCube (chunkX, chunkZ, chunkW);
		for (int x  = 0; x < chunkSize; ++x) 
		{
			int cX = chunkX * chunkSize + x;
			for (int z = 0; z < chunkSize; ++z) 
			{
				int cZ = chunkZ * chunkSize + z ;
				for (int w = 0; w < chunkSize; ++w) 
				{
					int cW = chunkW * chunkSize + w;
					Vector3 cube = sampleCube[x, z, w];	
					float groundLevel = cube.x;
					BiomeData biome = GetBiomeData (cX, cZ, cW, 16.0f, 256.0f);				
					float overhangStart = s_seaLevel + Mathf.Clamp(cube.z * 4, -1, 1) * 2;

					for (int y = 0; y < chunkSize; ++y)
					{
						int cY = chunkY * chunkSize + y;

						if(cY < 2)
						{
							chunkData[x, y, z, w] = 1;
						}
						else if(cY < groundLevel) 
						{
							if(cY > overhangStart)
							{	
								float density = (Mathf.Clamp(volume.Perlin4DFBM(cX, cY, cZ, cW, 64, 0, 4) * 4, -1, 1) + 1);
								if(density > 0)
								{
									chunkData [x, y, z, w] = BuildColumn(cY, groundLevel, (cube.z * 2 + 2), biome.id);
								}
							}
							else
							{
								chunkData [x, y, z, w] = BuildColumn(cY, groundLevel, (cube.z * 2 + 2), biome.id);
							}
						}
					}
					for (int y = 0; y < chunkSize && chunkY * chunkSize + y < s_seaLevel; ++y)
					{						
						if(chunkData[x, y, z, w] == 0)
						{
							chunkData[x, y, z, w] = 3;
						}
					}
				}
			}
		}
		return chunkData;
	}

	protected byte BuildColumn(int height, float surfaceHeight, float surfaceDepth, int biomeID)
	{
		byte block = 0;
		if(height < surfaceHeight - surfaceDepth)
		{
			block = 1;
		}
		else
		{
			if(biomeID == -1)
			{
				block = 4;
			}
			else if(biomeID == 0)
			{
				block = 2;
			}
			else if(biomeID == 1)
			{
				block = 5;
			}
		}
		return block;
	}
	
	public float bias(float value, float bias)
	{
		return Mathf.Pow(value, biasTable[(int)(bias * 100)]);
	}

	public float gain(float value, float gain)
	{
		float ret;
		if(value < 0.5f)
		{
			ret = bias(2 * value, 1 - gain) * 0.5f;
		}
		else
		{
			ret = 1 - bias(2 - 2 * value, 1 - gain) * 0.5f;
		}
		return ret;
	}

	public float clamp(float value)
	{
		return value < 0 ? 0 : value > 1 ? 1 : value;
	}

	public Vector3[,,] GetSampleCube(int chunkX, int chunkZ, int chunkW)
	{
		var index = m_world.Hash (chunkX, 0, chunkZ, chunkW);
		if (!heightCubes.ContainsKey (index))
		{
			Vector3[,,] sampleCube = new Vector3[chunkSize, chunkSize, chunkSize];
			for (int x  = 0; x < chunkSize; ++x) 
			{
				int cX = chunkX * chunkSize + x;
				for (int z = 0; z < chunkSize; ++z) 
				{
					int cZ = chunkZ * chunkSize + z;
					for (int w = 0; w < chunkSize; ++w)
					{
						int cW = chunkW * chunkSize + w;
						sampleCube[x, z, w] = new Vector3(GetHeight(cX, cZ, cW), 0, detail2.Perlin3DFMB(cX, cZ, cW, 64, 0, 8));
					}
				}
			}
			heightCubes [index] = sampleCube;
		}
		return heightCubes [index];
	}

	public float GetHeight(float x, float z, float w)
	{
		float seaElevation = Mathf.Clamp(elevation.Perlin3DFMB(x, z, w, 256, 0.15f, 4), -.5f, 0);
		float elevationGain = Mathf.Clamp(((Mathf.Clamp(detail.Perlin3DFMB (x, z, w, 256, 0, 2) * 5, -1, 1) + 1) / 2), 0.01f, 1);
		float elevation0ffset = Mathf.Clamp(((Mathf.Clamp(detail2.Perlin3DFMB (x, z, w, 256, 0, 2) * 5, -1, 1) + 1) / 2), 0.01f, 1);
		float elevationValue = seaElevation < 0 ? seaElevation : elevation.RidgedMultiFractal3D (x, z, w, 128, elevation0ffset, elevationGain, 4);
		float groundLevel = elevationValue * s_mountainHeight + s_seaLevel;
		float detailApplied = groundLevel +  detail.Perlin3DFMB(x, z, w, 64, 0, 8) * s_detailScale;
		if(groundLevel > s_seaLevel + s_sinkHoleDepth)
		{
			float sinkHoleBlend = elevation.VoroniFBM(x, z, w, 128.0f, 0, 3);
			float sinkHoleOffset = (sinkHoleBlend > 0.08f ? 0 : (0.08f - sinkHoleBlend) / 0.08f) * s_sinkHoleDepth;	
			detailApplied -= sinkHoleOffset;
		}
		return detailApplied;
	}
	
	public struct BiomeData
	{
		public float rainfall;
		public float temperature;
		public int id;
	}

	public BiomeData GetBiomeData(float x, float y, float z, float scale, float biomeSampleRescale)
	{
		Noise.VoroniData data = biomeNoise.Voroni(x / scale,  y / scale, z / scale);
		BiomeData biome;
		if(m_biomes.ContainsKey(data.id))
		{
			biome = m_biomes[data.id];
		}
		else 
		{
			biome = new BiomeData ();
			Vector3 centroid = new Vector3(x, y, z) + data.delta * scale;
			float centroidHeight = GetHeight(centroid.x, centroid.y, centroid.z);
			biome.temperature = (Mathf.Clamp(biomeNoise.Perlin3DFMB(centroid.x, centroid.y, centroid.z, biomeSampleRescale * 2, 0, 3) * 5, -1, 1) + 1) / 2;
			//Adjust temperature with elevation based on atmospheric pressure (http://tinyurl.com/macaquk)
			biome.temperature *= Mathf.Pow(1 - 0.3158078f *  Mathf.Max(centroidHeight - (s_seaLevel + s_temperatureOffset), 0) / (m_world.height - s_seaLevel), 5.25588f);
			//Rainfall is biased with a curve based on temperature (http://tinyurl.com/ksj2qkf)
			biome.rainfall = ((Mathf.Clamp(biomeNoise.Perlin3DFMB(centroid.x, centroid.y, centroid.z, biomeSampleRescale, 0, 3) * 5, -1, 1) + 1) / 2) * bias(biome.temperature, 0.2f);
			biome.id = GetBiomeID(biome.rainfall, biome.temperature);
		}
		return biome;
	}

	protected int GetBiomeID(float rainfall, float temperature)
	{		
		return temperature <= .2f ? -1 : temperature >= .8f ? 1 : 0;
	}

	public struct GeneratorResponse
	{
		public Vector4 chunkLocation;
		public long chunkHash;
		public byte[,,,] chunkData;
		public GeneratorResponse(long hash, Vector4 loc, byte[,,,] data)
		{
			chunkHash = hash;
			chunkLocation = loc;
			chunkData = data;
		}
	}
}
