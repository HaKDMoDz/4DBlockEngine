using UnityEngine;
using System.Collections;
using System.Collections.Generic;
 
public class Chunk : MonoBehaviour 
{
	public GameObject worldGO;
	private World world;

	private List<Vector3> newVertices = new List<Vector3>();
	private List<int> newTriangles = new List<int>();
	private List<Color32> newColors = new List<Color32>();
	private List<Vector2> newUV = new List<Vector2>();
	private int faceCount = 0;

	private static float tUnit = 0.25f;
	private static Vector2 tStone = new Vector2 (3 * tUnit, tUnit);
	private static Vector2 tGrass = new Vector2 (0, 2 * tUnit);
	private static Vector2 tDirt = new Vector2 (tUnit, tUnit);
	private static Vector2 tGrassTop = new Vector2 (2 * tUnit, 0);
	private static Vector2 tWater = new Vector2 (2 * tUnit, 3 * tUnit);
	private static Vector2 tSand = new Vector2 (2 * tUnit, 2 * tUnit);
	private static Vector2 tSnow = new Vector2 (3 * tUnit, 0);

	private Mesh mesh;
	private MeshCollider col;

	public int chunkX;
	public int chunkY;
	public int chunkZ;
	public bool update;
 
	void Start () 
	{ 
		world=worldGO.GetComponent("World") as World;
		mesh = GetComponent<MeshFilter> ().mesh;
		col = GetComponent<MeshCollider> ();
		update = true;
 	}
  
	void LateUpdate () 
	{
		if(update)
		{
			update = false;
			GenerateMesh();
		}
	 }
  
	public void SetDirty() 
	{
		update = true;
	}

	public void Disable()
	{
		ClearGeometryBuildData ();
	}
	
 	//IEnumerator 
	private void GenerateMesh()
	{
		ClearGeometryBuildData();

		for (int x=0 ; x < world.chunkSize; x++)
		{
			for (int z=0 ; z < world.chunkSize; z++)
			{
				for (int y = 0 ; y < world.chunkSize; y++)
				{
					ConditionallyBuildFaces(x, y, z);
				}
			}
		}
		UpdateMesh ();
	}	
	
	public static Vector2 GetTopTextureIndex(byte block)
	{
		switch(block)
		{
		case 1:
			return tStone;
		case 2:
			return tGrass;
		case 3:
			return tWater;
		case 4:
			return tSnow;
		case 5: 
			return tSand;
		default:
			return tStone;
		}
	}

	public static Vector2 GetSideTextureIndex(byte block)
	{
		switch(block)
		{
			case 1:
				return tStone;
			case 2:
				return tDirt;
			case 3:
				return tWater;
			case 4:
				return tDirt;
			case 5: 
				return tSand;
		default:
				return tStone;
		}
	}

	public static Vector2 GetBottomTextureIndex(byte block)
	{
		switch(block)
		{
		case 1:
			return tStone;
		case 2:
			return tDirt;
		case 3:
			return tWater;
		case 4:
			return tDirt;
		case 5: 
			return tSand;
		default:
			return tStone;
		}
	}

	Color blockColor;
	private void ConditionallyBuildFaces(int x, int y, int z) 
	{
		byte block = Block (x, y, z);

		blockColor = BlockColor (x, y, z);
		//This code will run for every block in the chunk
		if(block != 0)
		{
			Vector2 topTexture = GetTopTextureIndex(block);
			Vector2 sideTexture = GetSideTextureIndex(block);
			Vector2 bottomTexture = GetBottomTextureIndex(block);
			if(Block(x, y + 1, z) == 0)
			{
				//Block above is air
				CubeTop(x, y, z, ref topTexture);
			}
			if(Block(x, y - 1, z) == 0){
				//Block below is air
				CubeBot(x, y, z, ref bottomTexture);
			}
			if(Block(x + 1, y, z) == 0)
			{
				//Block east is air
				CubeEast(x, y, z, ref sideTexture);
			}
			if(Block(x - 1, y, z ) == 0)
			{
				//Block west is air
				CubeWest(x, y, z, ref sideTexture);
			}
			if(Block(x, y, z + 1) == 0)
			{
				//Block north is air
				CubeNorth(x, y, z, ref sideTexture);
			}
			if(Block(x, y, z - 1) == 0)
			{
				//Block south is air
				CubeSouth(x, y, z, ref sideTexture);
			}
		}
	}
  
	byte Block(int x, int y, int z)
	{
		byte block = world.GetBlockAt(x + chunkX * world.chunkSize, y + chunkY * world.chunkSize, z + chunkZ * world.chunkSize, world.worldW);
		if(block == 3)
		{
			//world.simulation.AddFluidAt(x + chunkX * world.chunkSize, y + chunkY * world.chunkSize, z + chunkZ * world.chunkSize, 0, 1, false);
		//	world.SetBlockAt(x + chunkX * world.chunkSize, y + chunkY * world.chunkSize, z + chunkZ * world.chunkSize, world.worldW, 0);
		//	block = 0;
		}
		return block;
	}
	protected static Noise noise = new Noise (1); 
	Color BlockColor(int x, int y, int z)
	{
	//	Generator.BiomeData biomeData = world.generator.GetBiomeData (x + chunkX * world.chunkSize, z + chunkZ * world.chunkSize, 0, 32.0f, 128.0f);
	//	int color = Mathf.RoundToInt (biomeData.rainfall * 16);

	//	byte r = (byte)((Mathf.RoundToInt (biomeData.temperature * 16) * 15) & 255);
	//	byte g = 0;
	//	byte b = (byte)((Mathf.RoundToInt (biomeData.rainfall * 16) * 15) & 255);



		//Noise.VoroniData data = noise.Voroni((x + chunkX * world.chunkSize) / 128.0f, (z + chunkZ * world.chunkSize) / 128.0f, 0);
		//float blend = noise.VoroniFBM (x + chunkX * world.chunkSize, z + chunkZ * world.chunkSize, 0, 128.0f, 0, 3);
		/*switch(data.id % 4)
		{
		case 0:
			return Color.yellow;
		case 1:
			return Color.green;
		case 2:
			return Color.red;
		default:
			return Color.blue;
		}*/
		/*byte r = (byte)((data.id >> 16) & 255);
		byte g = (byte)((data.id >> 8) & 255);
		byte b = (byte)((data.id) & 255);*/

	//	return new Color32 (r, g, b, 255);

	/*	if(blend < 0.07f)
		{
			return Color.blue;
		}
		else
		{*/
			return Color.white;
		//}

	}

	protected void CubeTop (int x, int y, int z, ref Vector2 block) 
	{
		newVertices.Add(new Vector3 (x,  y,  z + 1));
		newVertices.Add(new Vector3 (x + 1, y,  z + 1));
		newVertices.Add(new Vector3 (x + 1, y,  z ));
		newVertices.Add(new Vector3 (x,  y,  z ));
		
		Cube (ref block);
	}
	
	protected void CubeNorth (int x, int y, int z, ref Vector2 block) 
	{
		newVertices.Add(new Vector3 (x + 1, y-1, z + 1));
		newVertices.Add(new Vector3 (x + 1, y, z + 1));
		newVertices.Add(new Vector3 (x, y, z + 1));
		newVertices.Add(new Vector3 (x, y-1, z + 1));
		
		Cube (ref block);
	}
	
	protected void CubeEast (int x, int y, int z, ref Vector2 block) 
	{
		newVertices.Add(new Vector3 (x + 1, y - 1, z));
		newVertices.Add(new Vector3 (x + 1, y, z));
		newVertices.Add(new Vector3 (x + 1, y, z + 1));
		newVertices.Add(new Vector3 (x + 1, y - 1, z + 1));
		
		Cube (ref block);
	}
	
	protected void CubeSouth (int x, int y, int z, ref Vector2 block) 
	{
		newVertices.Add(new Vector3 (x, y - 1, z));
		newVertices.Add(new Vector3 (x, y, z));
		newVertices.Add(new Vector3 (x + 1, y, z));
		newVertices.Add(new Vector3 (x + 1, y - 1, z));
		
		Cube (ref block);
	}
	
	protected void CubeWest (int x, int y, int z, ref Vector2 block) 
	{
		newVertices.Add(new Vector3 (x, y- 1, z + 1));
		newVertices.Add(new Vector3 (x, y, z + 1));
		newVertices.Add(new Vector3 (x, y, z));
		newVertices.Add(new Vector3 (x, y - 1, z));
		
		Cube (ref block);
	}
	
	protected void CubeBot (int x, int y, int z, ref Vector2 block) 
	{
		newVertices.Add(new Vector3 (x,  y-1,  z ));
		newVertices.Add(new Vector3 (x + 1, y-1,  z ));
		newVertices.Add(new Vector3 (x + 1, y-1,  z + 1));
		newVertices.Add(new Vector3 (x,  y-1,  z + 1));
		
		Cube (ref block);
	}
	
	private void Cube (ref Vector2 texturePos) 
	{
		newTriangles.Add(faceCount * 4  ); //1
		newTriangles.Add(faceCount * 4 + 1 ); //2
		newTriangles.Add(faceCount * 4 + 2 ); //3
		newTriangles.Add(faceCount * 4  ); //1
		newTriangles.Add(faceCount * 4 + 2 ); //3
		newTriangles.Add(faceCount * 4 + 3 ); //4
		
		newUV.Add(new Vector2 (texturePos.x + tUnit, texturePos.y));
		newUV.Add(new Vector2 (texturePos.x + tUnit, texturePos.y + tUnit));
		newUV.Add(new Vector2 (texturePos.x, texturePos.y + tUnit));
		newUV.Add(new Vector2 (texturePos.x, texturePos.y));
		
		newColors.Add (blockColor);
		newColors.Add (blockColor);
		newColors.Add (blockColor);
		newColors.Add (blockColor);

		faceCount++; // Add this line
	}
  
	 private void UpdateMesh ()
	 {
		mesh.Clear ();
		mesh.vertices = newVertices.ToArray();
		mesh.uv = newUV.ToArray();
		mesh.colors32 = newColors.ToArray();
		mesh.triangles = newTriangles.ToArray();
		mesh.Optimize ();
		mesh.RecalculateNormals ();

		col.sharedMesh=null;
		col.sharedMesh=mesh;
	 }

	private void ClearGeometryBuildData()
	{
		faceCount = 0;
		newUV.Clear();
		newColors.Clear();
		newTriangles.Clear();
		newVertices.Clear();
	}





}