
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshBuilder
{
	private World m_world;
	private List<Color32> newColors = new List<Color32>();

	public MeshBuilder (World world)
	{
		m_world = world;
	}
	
	private int faceCount;
	private List<Vector3> newVertices = new List<Vector3>();
	private List<int> newTriangles = new List<int>();
	private List<Vector2> newUV = new List<Vector2>();

	byte Block(int x, int y, int z)
	{
		byte block = m_world.GetBlockAt(x, y, z, m_world.worldW);
		return block;
	}

	public void ConditionallyBuildCell(int x, int y, int z, float renderThickness, bool isSurface) 
	{
		Color32 blockColor = BlockColor (x, y, z);
		//This code will run for every block in the chunk
		if(isSurface)//Block(x, y + 1, z) == 0)
		{
			//Block above is air
			CubeTop(x, y, z, renderThickness, ref blockColor);
		//}
		//if(Block(x, y - 1, z) == 0){
			//Block below is air
			CubeBot(x, y, z, ref blockColor);
		//}
		//if(Block(x + 1, y, z) == 0)
		//{
			//Block east is air
			CubeEast(x, y, z, renderThickness, ref blockColor);
		//}
		//if(Block(x - 1, y, z ) != 0)
		//{
			//Block west is air
			CubeWest(x, y, z, renderThickness, ref blockColor);
	//	}
	//	if(Block(x, y, z + 1) != 0)
	//	{
			//Block north is air
			CubeNorth(x, y, z, renderThickness, ref blockColor);
	//	}
	//	if(Block(x, y, z - 1) != 0)
	//	{
			//Block south is air
			CubeSouth(x, y, z, renderThickness, ref blockColor);
		}
	}

	//protected static Noise noise = new Noise (1); 
	Color BlockColor(int x, int y, int z)
	{
		return Color.blue;		
	}
	
	protected void CubeTop (int x, float y, int z, float renderThickness, ref Color32 block) 
	{
		float ty = y - (1 - renderThickness);
		newVertices.Add(new Vector3 (x,  ty,  z + 1));
		newVertices.Add(new Vector3 (x + 1, ty,  z + 1));
		newVertices.Add(new Vector3 (x + 1, ty,  z ));
		newVertices.Add(new Vector3 (x,  ty,  z ));
		
		Cube (ref block);
	}
	
	protected void CubeNorth (int x, float y, int z, float renderThickness, ref Color32 block) 
	{
		float ty = y - (1 - renderThickness);
		newVertices.Add(new Vector3 (x + 1, y-1, z + 1));
		newVertices.Add(new Vector3 (x + 1, ty, z + 1));
		newVertices.Add(new Vector3 (x, ty, z + 1));
		newVertices.Add(new Vector3 (x, y-1, z + 1));
		
		Cube (ref block);
	}
	
	protected void CubeEast (int x, float y, int z, float renderThickness, ref Color32 block) 
	{
		float ty = y - (1 - renderThickness);
		newVertices.Add(new Vector3 (x + 1, y - 1, z));
		newVertices.Add(new Vector3 (x + 1, ty, z));
		newVertices.Add(new Vector3 (x + 1, ty, z + 1));
		newVertices.Add(new Vector3 (x + 1, y - 1, z + 1));
		
		Cube (ref block);
	}
	
	protected void CubeSouth (int x, float y, int z, float renderThickness, ref Color32 block) 
	{
		float ty = y - (1 - renderThickness);
		newVertices.Add(new Vector3 (x, y - 1, z));
		newVertices.Add(new Vector3 (x, ty, z));
		newVertices.Add(new Vector3 (x + 1, ty, z));
		newVertices.Add(new Vector3 (x + 1, y - 1, z));
		
		Cube (ref block);
	}
	
	protected void CubeWest (int x, float y, int z, float renderThickness, ref Color32 block) 
	{
		float ty = y - (1 - renderThickness);
		newVertices.Add(new Vector3 (x, y- 1, z + 1));
		newVertices.Add(new Vector3 (x, ty, z + 1));
		newVertices.Add(new Vector3 (x, ty, z));
		newVertices.Add(new Vector3 (x, y - 1, z));
		
		Cube (ref block);
	}
	
	protected void CubeBot (int x, int y, int z, ref Color32 block) 
	{
		newVertices.Add(new Vector3 (x,  y-1,  z ));
		newVertices.Add(new Vector3 (x + 1, y-1,  z ));
		newVertices.Add(new Vector3 (x + 1, y-1,  z + 1));
		newVertices.Add(new Vector3 (x,  y-1,  z + 1));
		
		Cube (ref block);
	}
	
	private void Cube (ref Color32 blockColor) 
	{
		newTriangles.Add(faceCount * 4  ); //1
		newTriangles.Add(faceCount * 4 + 1 ); //2
		newTriangles.Add(faceCount * 4 + 2 ); //3
		newTriangles.Add(faceCount * 4  ); //1
		newTriangles.Add(faceCount * 4 + 2 ); //3
		newTriangles.Add(faceCount * 4 + 3 ); //4
		
		newColors.Add (blockColor);
		newColors.Add (blockColor);
		newColors.Add (blockColor);
		newColors.Add (blockColor);
		
		faceCount++; // Add this line
	}
	
	public void UpdateMesh (Mesh mesh)
	{
		mesh.Clear ();
		mesh.vertices = newVertices.ToArray();
		mesh.uv = newUV.ToArray();
		mesh.colors32 = newColors.ToArray();
		mesh.triangles = newTriangles.ToArray();
		mesh.Optimize ();
		mesh.RecalculateNormals ();
	}
	
	public void ClearGeometryBuildData()
	{
		faceCount = 0;
		newUV.Clear();
		newColors.Clear();
		newTriangles.Clear();
		newVertices.Clear();
	}
}


