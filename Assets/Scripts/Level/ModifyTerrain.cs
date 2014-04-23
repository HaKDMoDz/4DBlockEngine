using UnityEngine;
using System.Collections;

public class ModifyTerrain : MonoBehaviour {
	
	World world;
	GameObject cameraGO;

	// Use this for initialization
	void Start () {
	
		world=gameObject.GetComponent("World") as World;
		cameraGO=GameObject.FindGameObjectWithTag("MainCamera");
			
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (Input.GetMouseButtonDown (0)) 
		{
			ReplaceBlockCenter (5, 0);
		}

		if (Input.GetMouseButtonDown (1)) 
		{
			AddBlockCenter (5, 255);
		}

		if(Input.GetKeyUp(KeyCode.F))
		{
			SpawnWaterCenter(5);
		}
		if(Input.GetKeyUp(KeyCode.G))
		{
			world.simulation.m_setupDone = true;
			//SpawnWaterCenter(5);
		}
	}
	
	public void ReplaceBlockCenter(float range, byte block){
		//Replaces the block directly in front of the player
		
		Ray ray = new Ray(cameraGO.transform.position, cameraGO.transform.forward);
		RaycastHit hit;
	
		if (Physics.Raycast (ray, out hit)) {
			
			if(hit.distance<range){
				ReplaceBlockAt(hit, block);
			}
		}
		
	}

	public void AddBlockCenter(float range, byte block){
		//Adds the block specified directly in front of the player
		
		Ray ray = new Ray(cameraGO.transform.position, cameraGO.transform.forward);
		RaycastHit hit;
		
		if (Physics.Raycast (ray, out hit)) {
			
			if(hit.distance<range){
				AddBlockAt(hit,block);
			}
			//Debug.DrawLine(ray.origin,ray.origin+( ray.direction*hit.distance),Color.green,2);
		}
		
	}

	public void SpawnWaterCenter(float range){
		//Adds the block specified directly in front of the player
		
		Ray ray = new Ray(cameraGO.transform.position, cameraGO.transform.forward);
		RaycastHit hit;
		
		if (Physics.Raycast (ray, out hit)) {
			
			if(hit.distance<range){
				SpawnWaterAt(hit);
			}
			//Debug.DrawLine(ray.origin,ray.origin+( ray.direction*hit.distance),Color.green,2);
		}
		
	}
	
	public void ReplaceBlockAt(RaycastHit hit, byte block) {
		//removes a block at these impact coordinates, you can raycast against the terrain and call this with the hit.point
			Vector3 position = hit.point;
			position+=(hit.normal*-0.5f);
			
			SetBlockAt(position, block);
	}
	
	public void AddBlockAt(RaycastHit hit, byte block) {
		//adds the specified block at these impact coordinates, you can raycast against the terrain and call this with the hit.point
			Vector3 position = hit.point;
			position+=(hit.normal*0.5f);
			
			SetBlockAt(position,block);
			
	}
	
	public void SetBlockAt(Vector3 position, byte block) {
		//sets the specified block at these coordinates
		
		int x= Mathf.RoundToInt( position.x );
		int y= Mathf.RoundToInt( position.y );
		int z= Mathf.RoundToInt( position.z );
		
		SetBlockAt(x,y,z,block);
	}

	public void SpawnWaterAt(RaycastHit hit) {
		//adds the specified block at these impact coordinates, you can raycast against the terrain and call this with the hit.point
		Vector3 position = hit.point;
		position+=(hit.normal*0.5f);
		
		SpawnWaterAt(position);
		
	}
	
	public void SetBlockAt(int x, int y, int z, byte block) {
		//adds the specified block at these coordinates
		int w = world.worldW;		
		
		world.SetBlockAt(x, y, z, w, block);
		//
		UpdateChunkAt(x,y,z,block);
		
	}

	public void SpawnWaterAt(Vector3 position) {
		//sets the specified block at these coordinates
		
		int x= Mathf.RoundToInt( position.x );
		int y= Mathf.RoundToInt( position.y );
		int z= Mathf.RoundToInt( position.z );
		
		SpawnWaterAt(x,y,z);
	}

	public void SpawnWaterAt(int x, int y, int z) {
		//adds the specified block at these coordinates
		int w = world.worldW;		
		
		world.SpawnWater (x, y, z);
		
	}
	
	public void UpdateChunkAt(int x, int y, int z, byte block){		//To do: add a way to just flag the chunk for update and then it updates in lateupdate
		//Updates the chunk containing this block
		
		int updateX= Mathf.FloorToInt( x/world.chunkSize);
		int updateY= Mathf.FloorToInt( y/world.chunkSize);
		int updateZ= Mathf.FloorToInt( z/world.chunkSize);
		
		Chunk chunk = world.GetChunk(updateX,updateY, updateZ);

		chunk.update=true;
		
		if(x % world.chunkSize == 0)
		{
			chunk = world.GetChunk(updateX-1,updateY, updateZ);
			if(chunk != null)
			{
				chunk.update=true;
			}
		}
		
		if(Mathf.Abs(x % world.chunkSize) == 15)
		{
			chunk = world.GetChunk(updateX+1,updateY, updateZ);
			if(chunk != null)
			{
				chunk.update=true;
			}
		}
		
		if(y % world.chunkSize == 0)
		{
			chunk = world.GetChunk(updateX,updateY-1, updateZ);
			if(chunk != null)
			{
				chunk.update=true;
			}
		}
		
		if(y % world.chunkSize == 15)
		{
			chunk = world.GetChunk(updateX,updateY+1, updateZ);
			if(chunk != null)
			{
				chunk.update=true;
			}
		}
		
		if(z % world.chunkSize == 0)
		{
			chunk = world.GetChunk(updateX,updateY, updateZ-1);
			if(chunk != null)
			{
				chunk.update=true;
			}
		}
		
		if(Mathf.Abs(z % world.chunkSize) == 15)
		{
			chunk = world.GetChunk(updateX,updateY, updateZ+1);
			if(chunk != null)
			{
				chunk.update=true;
			}
		}
	}
}
