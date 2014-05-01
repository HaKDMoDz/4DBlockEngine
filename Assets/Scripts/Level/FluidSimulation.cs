using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class FluidSimulation 
{
	protected static float s_maxLevel = 1;
	protected static float s_maxFlow = 1;
	protected static float s_evaporationLevel = 0.16f;
	protected static float s_minLevel = 0.0001f;
	protected static float s_maxCompression = 0.07f;
	protected static float s_evaporationRate = 0.01f;
	protected static float s_minFlow = 0.0005f;
	protected World m_world;
	protected List<FluidContainer> m_containers;
	protected bool m_stepDone;
	public bool m_setupDone;
	
	protected long maskXZ;
	protected long maskY;
	protected int shiftX;
	protected int shiftY;

	protected FluidCell solidCell;
	protected List<FluidCell> cellAccumulator;
	protected Queue<List<FluidCell>> addQueue;
	private FluidMeshBuilder meshBuilder;
	//protected AutoResetEvent waitHandle;
	//protected Thread fluidThread;

	public FluidSimulation(World world)
	{
		m_world = world;
		int bitsXZ = (int)Mathf.Log ((uint)world.width * 2, 2);
		int bitsY = (int)Mathf.Log (world.height, 2);
		shiftX = bitsY + bitsXZ;
		shiftY = bitsXZ;
		maskXZ = (long)Mathf.Pow (2, bitsXZ) - 1;
		maskY = (long)Mathf.Pow (2, bitsY) - 1;
		m_containers = new List<FluidContainer> ();
		solidCell = new FluidCell (CellType.SOLID);
		addQueue = new Queue<List<FluidCell>> ();
		cellAccumulator = new List<FluidCell> ();
		meshBuilder = new FluidMeshBuilder (world);
		//waitHandle = new AutoResetEvent (false);
		//fluidThread = new Thread (ThreadLoop);
		//fluidThread.Start ();
	}

	public long CellHash(long x, long y, long z)
	{
		return (((x + m_world.width) & maskXZ) << shiftX) | (((y) & maskY) << shiftY) | ((z + m_world.width) & maskXZ);
	}

	public void AddFluidAt(int x, int y, int z, int w, float amount, bool isSource)
	{
		if(amount > 0)
		{
			FluidCell cell = new FluidCell(x, y, z, w, CellType.WATER, amount);
			cell.isSource = isSource;
			//cell.isSource = true;
			foreach(FluidCell existing in cellAccumulator)
			{
				if(existing.x == cell.x && existing.y == cell.y && existing.z == cell.z && existing.w == cell.w)
				{
					existing.levelNextStep = Mathf.Clamp01(existing.levelNextStep + amount);
					existing.level = existing.levelNextStep;
					cell = null;
					break;
				}
			}
			if(cell != null)
			{
				cellAccumulator.Add(cell);
			}
		}
	}

	public void UpdateBlockAt (int x, int y, int z, int w, bool solid)
	{
		foreach(FluidContainer container in m_containers)
		{
			if(container.alive)
			{
				if(container.Contains(x, y, z) && container.w == w)
				{
					long hash = CellHash(x, y, z);
					container.update = true;
					if(container.cellDictionary.ContainsKey(hash))
					{
						FluidCell cell  = container.cellDictionary[hash];
						if(solid)
						{
							container.cellDictionary.Remove(hash);
							container.cells.Remove(cell);
						}
						else
						{
							cell.awake = true;
							container.BuildNeighborhood(cell);
						}
					}
					for(int i = -1; i < 1; ++i)
					{
						if(i == 0) continue;
						hash = CellHash(x + i, y, z);
						if(container.cellDictionary.ContainsKey(hash))
						{
							FluidCell cell  = container.cellDictionary[hash];
							cell.awake = true;
							container.BuildNeighborhood(cell);
						}
						hash = CellHash(x, y + i, z);
						if(container.cellDictionary.ContainsKey(hash))
						{
							FluidCell cell  = container.cellDictionary[hash];
							cell.awake = true;
							container.BuildNeighborhood(cell);
						}
						hash = CellHash(x, y, z + i);
						if(container.cellDictionary.ContainsKey(hash))
						{
							FluidCell cell  = container.cellDictionary[hash];
							cell.awake = true;
							container.BuildNeighborhood(cell);
						}
					}
				}
			}
		}
	}

	protected GameObject gameObject;
	protected int m_last = System.Environment.TickCount;

	public void Update()
	{

		if(gameObject == null)
		{
			gameObject = new GameObject("water");
			GameObject.Instantiate (gameObject, new Vector3(), new Quaternion());
			MeshFilter meshFilter = (MeshFilter)gameObject.AddComponent(typeof(MeshFilter));
			meshFilter.mesh = new Mesh();
			MeshRenderer renderer = gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
			renderer.material.shader = Shader.Find ("Particles/Additive");
		}

	//	if(m_stepDone)
	//	{
		if(m_setupDone &&  System.Environment.TickCount - m_last > 100)
		{
			meshBuilder.ClearGeometryBuildData();
			m_last = System.Environment.TickCount;
			if(cellAccumulator.Count > 0)
			{
				addQueue.Enqueue (cellAccumulator);
				cellAccumulator = new List<FluidCell>();
				//waitHandle.Set();
			}
			TickSimulation();
			foreach(FluidContainer container in m_containers)
			{
				if(container.alive)
				{
					foreach(FluidCell cell in container.cells)
					{
						meshBuilder.ConditionallyBuildCell(cell.x, cell.y, cell.z, cell);
					}
				}
			}
			meshBuilder.UpdateMesh((gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter).mesh);
		//	m_stepDone = false;
		//	waitHandle.Set();
		//}
		//	m_setupDone = false;
		}
	}
	

	/*protected void ThreadLoop()
	{
		while(true)
		{
			TickSimulation();
			m_stepDone = true;
			waitHandle.WaitOne ();
		}
	}*/

	protected void TickSimulation()
	{
		if(addQueue.Count > 0)
		{
			List<FluidCell> cellsToAdd = addQueue.Dequeue();
			FluidContainer container = new FluidContainer();
			container.world = m_world;
			container.simulation = this;
			container.w = 0;//cellToAdd.w;			
			//container.Add(cellToAdd);
			m_containers.Add(container);
			foreach(FluidCell next in cellsToAdd)
			{
				FluidCell cellToAdd = next;
				/*foreach(FluidContainer container in m_containers)
				{
					if(container.Contains(cellToAdd))
					{*/
						container.Add(cellToAdd);
				/*		cellToAdd = null;
						break;
					}
				}
				if(cellToAdd != null)
				{
					FluidContainer container = new FluidContainer();
					container.world = m_world;
					container.simulation = this;
					container.w = cellToAdd.w;			
					container.Add(cellToAdd);
					m_containers.Add(container);
				}*/
			}
		}
		foreach(FluidContainer container in m_containers)
		{
			if(container.update)
			{
				container.Step();
			}
		}
		List<FluidContainer> keepList = new List<FluidContainer> ();
		for(int i = 0; i < m_containers.Count; ++i)
		{
			bool keepContainer = true;
			FluidContainer discard = m_containers[i];
			if(discard.alive)
			{
				for(int j = i + 1; j < m_containers.Count; ++j)
				{
					FluidContainer keep = m_containers[j];
					if(keep.alive && discard.Intersects(keep))
					{
						foreach(FluidCell cell in discard.cells)
						{
							keep.Add(cell);
						}
						keepContainer = false;
						break;
					}
				}
				if(keepContainer)
				{
					keepList.Add(discard);
				}
			}
		}
		m_containers = keepList;
	}

	protected class FluidContainer
	{
		public World world;
		public FluidSimulation simulation;
		public Vector3 min;
		public Vector3 max;
		public int w;
		public bool update; //does this area need to update?
		public bool alive;
		public List<FluidCell> cells;
		public Dictionary<long, FluidCell> cellDictionary;
		public List<FluidCell> updated;

		public FluidContainer()
		{
			update = true;
			alive = true;
			min = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
			max = new Vector3(-int.MaxValue, -int.MaxValue, -int.MaxValue);
			cells = new List<FluidCell>();
			cellDictionary = new Dictionary<long, FluidCell>();
			updated = new List<FluidCell>();
		}
		
		public bool Contains(FluidCell cell)
		{
			return Contains(cell.x, cell.y, cell.z);
		}

		public bool Contains(int x, int y, int z)
		{
			return ((x >= min.x && x <= max.x) &&
			        (y >= min.y && y <= max.y) &&
			        (z >= min.z && z <= max.z));
		}

		public bool Intersects(FluidContainer container)
		{
			if(container.min.x > max.x || min.x > container.max.x)
			{
				return false;
			}
			if(container.min.y > max.y || min.y > container.max.y)
			{
				return false;
			}
			if(container.min.z > max.z || min.z > container.max.z)
			{
				return false;
			}
			return true;
		}

		public void Add(FluidCell cell)
		{
			long cellHash = simulation.CellHash(cell.x, cell.y, cell.z);
			if(cellDictionary.ContainsKey(cellHash))
			{
				FluidCell existingCell = cellDictionary[cellHash];
				if(existingCell.type != CellType.SOLID)
				{
					existingCell.level += cell.level;
					existingCell.levelNextStep += cell.level;
					if(existingCell.levelNextStep > s_minLevel && existingCell.type == CellType.AIR)
					{
						existingCell.type = CellType.WATER;
						cells.Add(existingCell);
						BuildNeighborhood(existingCell);
						RecalculateBounds(existingCell);
					}
				}
			}
			else
			{
				cell.container = this;
				cells.Add(cell);
				cellDictionary.Add(cellHash, cell);
				BuildNeighborhood(cell);
				RecalculateBounds(cell);
			}
			update = true;
		}

		public void Step()
		{
			foreach(FluidCell cell in cells)
			{
				if(cell.awake && cell.type == CellType.WATER) //In theory this check is redundant...
				{
					cell.Propagate();
				}
			}
			bool hasChanged = false;
			bool potentialPruningNeeded = false;
			foreach(FluidCell cell in updated)
			{
				if(cell.type == CellType.SOLID)
				{
					hasChanged = true;
					potentialPruningNeeded = true;
				}
				if(Mathf.Abs(cell.level - cell.levelNextStep) > s_minFlow / 2.0f)
				{
					hasChanged = true;
					cell.awake = true;
				}
				cell.level = cell.levelNextStep;
				if(cell.type == CellType.WATER && cell.level < s_minLevel)
				{
					potentialPruningNeeded = true;
					cell.type = CellType.AIR;
				}
				if(cell.type == CellType.AIR && cell.level > s_minLevel)
				{
					cell.type = CellType.WATER;
					RecalculateBounds(cell);
					BuildNeighborhood(cell);
					cells.Add(cell);
				}
			}
			updated.Clear();
			if(potentialPruningNeeded)
			{
				min = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
				max = new Vector3(-int.MaxValue, -int.MaxValue, -int.MaxValue);
				List<FluidCell> removeList = new List<FluidCell>();
				foreach(FluidCell cell in cells)
				{
					if(cell.type == CellType.SOLID ||
					   cell.type == CellType.AIR)
					{
						removeList.Add(cell);
					}
					else 
					{
						RecalculateBounds(cell);
					}
				}
				foreach(FluidCell cell in removeList)
				{
					cells.Remove(cell);
					if((cell.up == null || cell.up.type != CellType.WATER) &&
						(cell.north == null || cell.north.type != CellType.WATER) &&
						(cell.east == null || cell.east.type != CellType.WATER) &&
						(cell.south == null || cell.south.type != CellType.WATER) &&
						(cell.west == null || cell.west.type != CellType.WATER) &&
						(cell.down == null || cell.down.type != CellType.WATER))
					{
						long cellHash = simulation.CellHash(cell.x, cell.y, cell.z);
						cellDictionary.Remove(cellHash);
					}
				}
			}
			this.alive = cells.Count != 0;
			this.update = hasChanged;
		}

		private void RecalculateBounds(FluidCell cell)
		{
			if(cell.x > max.x)
			{
				max.x = cell.x + 1; 
			}
			if(cell.x < min.x)
			{
				min.x = cell.x - 1;
			}

			if(cell.y > max.y)
			{
				max.y = cell.y + 1; 
			}
			if(cell.y < min.y)
			{
				min.y = cell.y - 1;
			}

			if(cell.z > max.z)
			{
				max.z = cell.z + 1; 
			}
			if(cell.z < min.z)
			{
				min.z = cell.z - 1;
			}
		}

		public void BuildNeighborhood(FluidCell cell)
		{
			cell.up = GetOrCreateCell(cell.x, cell.y + 1, cell.z);
			cell.up.down = cell;
			cell.down = GetOrCreateCell(cell.x, cell.y - 1, cell.z);
			cell.down.up = cell;
			cell.north = GetOrCreateCell(cell.x, cell.y, cell.z + 1);
			cell.north.south = cell;
			cell.east = GetOrCreateCell(cell.x + 1, cell.y, cell.z);
			cell.east.west = cell;
			cell.south = GetOrCreateCell(cell.x, cell.y, cell.z - 1);
			cell.south.north = cell;
			cell.west = GetOrCreateCell(cell.x - 1, cell.y, cell.z);
			cell.west.east = cell;
		}

		public FluidCell GetOrCreateCell(int x, int y, int z)
		{
			FluidCell cell;
			byte block = world.GetBlockAt(x, y, z, w);
			if(block != 0)
			{
				cell = simulation.solidCell;
			}
			else
			{
				long cellHash = simulation.CellHash(x, y, z);
				if(cellDictionary.ContainsKey(cellHash))
				{
					cell = cellDictionary[cellHash];
				}
				else 
				{
					cell = new FluidCell(x, y, z, w, CellType.AIR, 0);
					cell.container = this;
					cellDictionary.Add(cellHash, cell);
				}
			}
			return cell;
		}		
	}
	
	protected class FluidCell
	{
		public int x;
		public int y;
		public int z;
		public int w;
		public float level;
		public float levelNextStep;
		public CellType type;
		public FluidCell up;
		public FluidCell down;
		public FluidCell north;
		public FluidCell east;
		public FluidCell south;
		public FluidCell west;
		public FluidContainer container;
		public bool isSource;
		public bool awake;

		public FluidCell(CellType type)
		{
			this.type = type;
		}

		public FluidCell(int x, int y, int z, int w, CellType type, float level)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
			this.type = type;
			this.level = level;
			this.levelNextStep = level;
			this.awake = true;
		}

		public void Propagate()
		{
			if (isSource)
			{
				level = Mathf.Max(level, 1);
				levelNextStep = level;
			}
			float levelRemaining = level;
			float outFlow = 0;
			if(level > s_maxLevel && levelRemaining > (up.level + s_maxCompression * 2) && up.type != CellType.SOLID)
			{
				outFlow = clampFlow(levelRemaining - GetStableStateVertical(up.level, levelRemaining), levelRemaining);
				levelNextStep -= outFlow;
				levelRemaining -= outFlow;
				up.levelNextStep += outFlow;
				container.updated.Add(up);
			}
			else 
			{
				if(down.type != CellType.SOLID)
				{
					outFlow = clampFlow(GetStableStateVertical (levelRemaining, down.level) - down.level, levelRemaining);
					if(outFlow > 0)
					{
						levelNextStep -= outFlow;
						levelRemaining -= outFlow;
						down.levelNextStep += outFlow;
						container.updated.Add(down);
					}
				}
				if(levelRemaining > 0)
				{
					float average = 0;
					int count = 0;
					if(north.type != CellType.SOLID && north.level < level)
					{
						average += north.level;
						count += 1;
					}
					if(east.type != CellType.SOLID && east.level < level)
					{
						average += east.level;
						count += 1;
					}
					if(south.type != CellType.SOLID && south.level < level)
					{
						average += south.level;
						count += 1;
					}
					if(west.type != CellType.SOLID && west.level < level)
					{
						average += west.level;
						count += 1;
					}
					if(count > 0)
					{
						average = (average / count) * .95f;
						outFlow = clampFlow(levelRemaining - average, levelRemaining);
						if(outFlow > 0)
						{
							levelNextStep -= outFlow;
							levelRemaining -= outFlow;
							outFlow /= count;
							if(north.type != CellType.SOLID && north.level <= levelRemaining)
							{
								north.levelNextStep += outFlow;
								container.updated.Add(north);
							}
							if(east.type != CellType.SOLID && east.level <= levelRemaining)
							{
								east.levelNextStep += outFlow;
								container.updated.Add(east);
							}
							if(south.type != CellType.SOLID && south.level <= levelRemaining)
							{
								south.levelNextStep += outFlow;
								container.updated.Add(south);
							}
							if(west.type != CellType.SOLID && west.level <= levelRemaining)
							{
								west.levelNextStep += outFlow;
								container.updated.Add(west);
							}
						}
					}
				}
				if(levelRemaining > 0)
				{
					if(up.type != CellType.SOLID)
					{
						outFlow = clampFlow(levelRemaining - GetStableStateVertical(up.level, levelRemaining), levelRemaining);
						if(outFlow > 0)
						{
							levelNextStep -= outFlow;
							levelRemaining -= outFlow;
							up.levelNextStep += outFlow;
							container.updated.Add(up);
						}
					}
					//only remove stagnant water
					if(levelNextStep == level && levelRemaining <= s_evaporationLevel && up.type == CellType.AIR)
					{
						levelNextStep -= s_evaporationRate;
					}
				}
			}
			if(Mathf.Abs(level - levelNextStep) > s_minFlow / 2.0f)
			{
				container.updated.Add(this);
			}
			else if(!isSource) 
			{
				awake = false;
			}
		}

		float clampFlow(float flow, float level)
		{
			if(flow > s_minFlow)
			{
				flow *= 0.5f;
			}
			return Mathf.Clamp (flow, 0, Mathf.Min(level, s_maxFlow));
		}

		float GetStableStateVertical (float cell, float down)
		{
			float sum = cell + down;
			float newDown = 0;
			if ( sum <= s_maxLevel)
			{
				newDown = s_maxLevel;
			} 
			else if ( sum < 2 * s_maxLevel + s_maxCompression )
			{
				newDown = (s_maxLevel * s_maxLevel + sum * s_maxCompression) / (s_maxLevel + s_maxCompression);
			} 
			else 
			{
				newDown = (sum + s_maxCompression) / 2;
			}
			return newDown;
		}
	}

	public enum CellType
	{
		SOLID,
		WATER,
		AIR
	}

	protected class FluidMeshBuilder
	{
		private World m_world;
		private int faceCount;
		private List<Vector3> newVertices = new List<Vector3>();
		private List<int> newTriangles = new List<int>();
		private List<Vector2> newUV = new List<Vector2>();
		private List<Color32> newColors = new List<Color32>();
		
		public FluidMeshBuilder (World world)
		{
			m_world = world;
		}
		
		byte Block(int x, int y, int z)
		{
			byte block = m_world.GetBlockAt(x, y, z, m_world.worldW);
			return block;
		}
		
		public void ConditionallyBuildCell(int x, int y, int z, FluidCell cell) 
		{
			Color32 blockColor = BlockColor (x, y, z);
			float fx = x - .5f;
			float fy = y + .5f;
			float fz = z - .5f;
			//This code will run for every block in the chunk
			float renderThickness = Mathf.Min(cell.level, 1);
			if(cell.up.type == CellType.WATER)
			{
				renderThickness = 1;
			}

			if(cell.up.type == CellType.AIR)
			{
				CubeTop(fx, fy, fz, renderThickness, ref blockColor);
			}
			if(cell.down.type == CellType.AIR){
				//Block below is air
				CubeBot(fx, fy, fz, ref blockColor);
			}
			if(cell.east.type == CellType.AIR || cell.east.level < cell.level)
			{
				//Block east is air
				CubeEast(fx, fy, fz, renderThickness, ref blockColor);
			}
			if(cell.west.type == CellType.AIR || cell.west.level < cell.level)
			{
				//Block west is air
				CubeWest(fx, fy, fz, renderThickness, ref blockColor);
			}
			if(cell.north.type == CellType.AIR || cell.north.level < cell.level)
			{
				//Block north is air
				CubeNorth(fx, fy, fz, renderThickness, ref blockColor);
			}
			if(cell.south.type == CellType.AIR || cell.south.level < cell.level)
			{
				//Block south is air
				CubeSouth(fx, fy, fz, renderThickness, ref blockColor);
			}
		}
		
		//protected static Noise noise = new Noise (1); 
		Color BlockColor(int x, int y, int z)
		{
			return Color.Lerp(Color.blue, Color.black, .75f);		
		}
		
		protected void CubeTop (float x, float y, float z, float renderThickness, ref Color32 block) 
		{
			float ty = y - (1 - renderThickness);
			newVertices.Add(new Vector3 (x,  ty,  z + 1));
			newVertices.Add(new Vector3 (x + 1, ty,  z + 1));
			newVertices.Add(new Vector3 (x + 1, ty,  z ));
			newVertices.Add(new Vector3 (x,  ty,  z ));
			
			Cube (ref block);
		}
		
		protected void CubeNorth (float x, float y, float z, float renderThickness, ref Color32 block) 
		{
			float ty = y - (1 - renderThickness);
			newVertices.Add(new Vector3 (x + 1, y-1, z + 1));
			newVertices.Add(new Vector3 (x + 1, ty, z + 1));
			newVertices.Add(new Vector3 (x, ty, z + 1));
			newVertices.Add(new Vector3 (x, y-1, z + 1));
			
			Cube (ref block);
		}
		
		protected void CubeEast (float x, float y, float z, float renderThickness, ref Color32 block) 
		{
			float ty = y - (1 - renderThickness);
			newVertices.Add(new Vector3 (x + 1, y - 1, z));
			newVertices.Add(new Vector3 (x + 1, ty, z));
			newVertices.Add(new Vector3 (x + 1, ty, z + 1));
			newVertices.Add(new Vector3 (x + 1, y - 1, z + 1));
			
			Cube (ref block);
		}
		
		protected void CubeSouth (float x, float y, float z, float renderThickness, ref Color32 block) 
		{
			float ty = y - (1 - renderThickness);
			newVertices.Add(new Vector3 (x, y - 1, z));
			newVertices.Add(new Vector3 (x, ty, z));
			newVertices.Add(new Vector3 (x + 1, ty, z));
			newVertices.Add(new Vector3 (x + 1, y - 1, z));
			
			Cube (ref block);
		}
		
		protected void CubeWest (float x, float y, float z, float renderThickness, ref Color32 block) 
		{
			float ty = y - (1 - renderThickness);
			newVertices.Add(new Vector3 (x, y- 1, z + 1));
			newVertices.Add(new Vector3 (x, ty, z + 1));
			newVertices.Add(new Vector3 (x, ty, z));
			newVertices.Add(new Vector3 (x, y - 1, z));
			
			Cube (ref block);
		}
		
		protected void CubeBot (float x, float y, float z, ref Color32 block) 
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

}
