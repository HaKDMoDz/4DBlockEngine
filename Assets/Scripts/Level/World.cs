using UnityEngine;
using System.Collections;
using System.Collections.Generic;
 
public class World : MonoBehaviour
{
  
	public GameObject chunkGO;
	public int chunkSize = 16;
	public Dictionary<long, byte[,,,]> data;
	public int width = 64;
	public int height = 16;
	public int spissitude = 16;
	public int worldW = 0;

	public Dictionary<long, Chunk> awakeChunks;
	public Dictionary<long, Chunk> sleepingChunks;
	public Dictionary<long, int> chunkAge;

	public Stack<Chunk> chunkPool;
	protected Transform playerTransform;
	public Generator generator;
	public FluidSimulation simulation;

	protected int chunkOffsetXZ;
	protected long maskXZ;
	protected long maskY;
	protected long maskW;
	protected int shiftX;
	protected int shiftY;
	protected int shiftZ;
  
	// Use this for initialization
	void Start ()
	{
		RenderSettings.fog = true;
		chunkPool = new Stack<Chunk> ();
		data = new Dictionary<long, byte[,,,]> ();
		generator = new Generator(this);
		simulation = new FluidSimulation(this);
		awakeChunks = new Dictionary<long, Chunk> ();
		sleepingChunks = new Dictionary<long, Chunk> ();
		chunkAge = new Dictionary<long, int> ();

		chunkOffsetXZ = width / chunkSize;
		int bitsXZ = (int)Mathf.Log ((uint)width * 2 / chunkSize, 2);
		int bitsY = (int)Mathf.Log (height / chunkSize, 2);
		int bitsW = (int)Mathf.Log (spissitude / chunkSize, 2);
		shiftX = bitsY + bitsXZ + bitsW;
		shiftY = bitsXZ + bitsW;
		shiftZ = bitsW;
		maskXZ = (long)Mathf.Pow (2, bitsXZ) - 1;
		maskY = (long)Mathf.Pow (2, bitsY) - 1;
		maskW = (long)Mathf.Pow (2, bitsW) - 1;
	}

	public void SpawnWater(int x, int y, int z)
	{
		simulation.AddFluidAt (x, y, z, 0, 1, true);
	}

	public long Hash(Vector4 chunk)
	{
		return Hash ((long)chunk.x, (long)chunk.y, (long)chunk.z, (long)chunk.w);
	}

	public long Hash(long chunkX, long chunkY, long chunkZ, long chunkW)
	{
		return (((chunkX + chunkOffsetXZ) & maskXZ) << shiftX) | (((chunkY) & maskY) << shiftY) | (((chunkZ + chunkOffsetXZ) & maskXZ) << shiftZ) | ((chunkW) & maskW);
	}

	public void LoadChunk(int x, int y, int z, long chunkHash)
	{
		Chunk newChunk = null;
		if(sleepingChunks.ContainsKey(chunkHash))
		{
			newChunk = sleepingChunks[chunkHash];
			newChunk.gameObject.SetActive(true);
			sleepingChunks.Remove(chunkHash);
			chunkAge.Remove(chunkHash);
		}
		else 
		{
			GameObject newChunkGO = null;
			if(chunkPool.Count > 0) 
			{
				newChunkGO = chunkPool.Pop().gameObject;
				newChunkGO.transform.position = new Vector3 (x * chunkSize - 0.5f, y * chunkSize + 0.5f, z * chunkSize - 0.5f);
				newChunkGO.SetActive(true);
			} 
			else 
			{
				newChunkGO = Instantiate (chunkGO, new Vector3 (x * chunkSize - 0.5f, y * chunkSize + 0.5f, z * chunkSize - 0.5f), new Quaternion (0, 0, 0, 0)) as GameObject;
			}
			newChunk = newChunkGO.GetComponent ("Chunk") as Chunk;
			newChunk.worldGO = gameObject;
			newChunk.chunkX = x;
			newChunk.chunkY = y;
			newChunk.chunkZ = z;
			GetChunkData(x * chunkSize, y * chunkSize, z * chunkSize);
			newChunk.SetDirty();
		}
		awakeChunks[chunkHash] = newChunk;
	}
	
	public void UnloadChunk(long chunkHash)
	{
		Chunk chunk = awakeChunks[chunkHash];
		chunk.gameObject.SetActive(false);
		chunk.Disable();
		awakeChunks.Remove (chunkHash);
		int now = System.Environment.TickCount;
		sleepingChunks.Add (chunkHash, chunk);
		chunkAge.Add (chunkHash, now);
		List<long> removeChunks = new List<long> ();
		foreach(KeyValuePair<long, int> entry in chunkAge)
		{
			long hash = entry.Key;
			if((now - chunkAge[hash]) > 3000)
			{
				removeChunks.Add(hash);
			}
		}
		foreach(long hash in removeChunks)
		{
			chunk = sleepingChunks[hash];
			sleepingChunks.Remove(hash);
			chunkAge.Remove(hash);
			if(chunkPool.Count < 100) 
			{
				chunkPool.Push(chunk);
			} 
			else 
			{
				Object.Destroy(chunk.gameObject);
			}
		}
	}
  
	// Update is called once per frame
	void Update ()
	{
		generator.Update();
		simulation.Update();
		if(Input.GetKeyUp(KeyCode.E))
		{
			worldW = Mathf.Min(worldW + 1, spissitude - 1);
			Debug.Log(worldW);
			ReDrawWorld() ;
		}
		if(Input.GetKeyUp(KeyCode.Q))
		{
			worldW = Mathf.Max(worldW - 1, 0);
			Debug.Log(worldW);
			ReDrawWorld();
		}
		if(playerTransform == null) 
		{
			playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
		}
		LoadChunks(playerTransform.position, 10, 14);
	}

	protected void ReDrawWorld() 
	{
		foreach(KeyValuePair<long, Chunk>  chunk in awakeChunks)
		{
			chunk.Value.SetDirty();
		}
	}
	
	public void LoadChunks(Vector3 playerPos, int distToLoad, float distToUnload)
	{
		Vector3 playerChunk = playerPos / chunkSize;
		List<long> removeChunks = new List<long> ();

		foreach(KeyValuePair<long, Chunk>  entry in awakeChunks)
		{
			Chunk chunk = entry.Value;
			float dist = Vector3.Distance(new Vector3(chunk.chunkX, chunk.chunkY, chunk.chunkZ), playerChunk);
			if(dist > distToUnload)
			{
				removeChunks.Add(entry.Key);
			}
		}
		foreach(long hash in removeChunks)
		{
			UnloadChunk(hash);
		}
		List<Vector3> chunksToAdd = new List<Vector3>();
		for(int x = -distToLoad; x <= distToLoad; ++x)
		{
			for (int y = -distToLoad; y <= distToLoad; ++y) 
			{
				for(int z = -distToLoad; z <= distToLoad; ++z)
				{
					float dist = Mathf.Sqrt(x * x + y * y + z *z);
					if(dist <= distToLoad)
					{
						long chunkHash = Hash((int)(playerChunk.x + x),  (int)(playerChunk.y + y), (int)(playerChunk.z + z), 0);
						if(!awakeChunks.ContainsKey(chunkHash))
						{
							chunksToAdd.Add(new Vector3(playerChunk.x + x,  playerChunk.y + y, playerChunk.z + z));
						}
					}
				}
			}
		}

		chunksToAdd.Sort (CompareChunksForLoading);

		foreach(Vector3 chunkLoc in chunksToAdd)
		{
			int x = (int)chunkLoc.x;
			int y = (int)chunkLoc.y;
			int z = (int)chunkLoc.z;
			long chunkHash = Hash(x, y, z, 0);
			LoadChunk(x, y, z, chunkHash);
		}
	}

	protected int CompareChunksForLoading(Vector3 c1, Vector3 c2)
	{
		return (int)(100 * (Vector3.Distance (c1, playerTransform.position / chunkSize) - Vector3.Distance (c2, playerTransform.position / chunkSize)));
	}

	public void SetBlockAt (int x, int y, int z, int w, byte block)
	{
		byte[,,,] chunkData = GetChunkData (x, y, z, w);
		if(chunkData != null)
		{
			int x2 = x % chunkSize;
			if(x2 < 0) 
			{
				x2 = x2 + (chunkSize);
			}
			int z2 = z % chunkSize;
			if(z2 < 0) 
			{
				z2 = z2 + (chunkSize);
			}
			chunkData [x2, y % chunkSize, z2, w % chunkSize] = block;
			simulation.UpdateBlockAt(x, y, z, w, block != 0);
		}
	}

	public void SetChunkData (Generator.GeneratorResponse response)
	{
		long dataIndex = Hash((int)response.chunkLocation.x, (int)response.chunkLocation.y, (int)response.chunkLocation.z, (int)response.chunkLocation.w);
		long chunkIndex = Hash((int)response.chunkLocation.x, (int)response.chunkLocation.y, (int)response.chunkLocation.z, 0);
		if(!data.ContainsKey(dataIndex) || data[dataIndex] == null)
		{
			data [dataIndex] = response.chunkData;
			if(awakeChunks.ContainsKey(chunkIndex))
			{
				awakeChunks[chunkIndex].SetDirty();
			}
			else if(sleepingChunks.ContainsKey(chunkIndex))
			{
				sleepingChunks[chunkIndex].SetDirty();
			}
		}
	}

	/*public void InitializeWater()
	{
		foreach()
		{
			byte[,,,] value = entry.Value;
			for (int x = 0; x <= chunkSize; ++x) 
			{
				for (int y = 0; y <= chunkSize; ++y) 
				{
					for(int z = 0; z <= chunkSize; ++z)
					{
						simulation.AddFluidAt(x, y, z, 0, 1);
					}
				}
			}
		}
	}*/

	public Chunk GetChunk(int x, int y, int z)
	{
		long chunkIndex = Hash(x, y, z, 0);
		if(awakeChunks.ContainsKey(chunkIndex))
		{
			return awakeChunks[chunkIndex];
		}
		else if(sleepingChunks.ContainsKey(chunkIndex))
		{
			return sleepingChunks[chunkIndex];
		}
		return null;
	}
	
	public byte[,,,] GetChunkData(int x, int y, int z) 
	{
		return GetChunkData (x, y, z, worldW);
	}

	public byte[,,,] GetChunkData(int x, int y, int z, int w) 
	{
		int cX = Mathf.FloorToInt(x / (float)chunkSize);
		int cY = (int)(y / chunkSize);
		int cZ = Mathf.FloorToInt(z / (float)chunkSize);
		int cW = (int)(w / chunkSize);
		long index = Hash(cX, cY, cZ, cW);
		if (!data.ContainsKey(index)) 
		{
			generator.Enqueue(new Vector4(cX, cY, cZ, cW), index);
			return  null;
		}
		return data[index];
	}

	public byte GetBlockAt (int x, int y, int z, int w)
	{
		if (x >= width || x < -width || y >= height || y < 0 || z >= width || z < -width || w >= spissitude || w < 0)
		{
			return (byte) 0;
		}

		byte[,,,] chunkData = GetChunkData (x, y, z, w);

		if(chunkData != null)
		{
			x = x % chunkSize;
			if(x < 0) 
			{
				x = x + (chunkSize);
			}
			z = z % chunkSize;
			if(z < 0) 
			{
				z = z + (chunkSize);
			}
			return chunkData[x, y % chunkSize, z, w % chunkSize];
		}
		return 0;
	}
}