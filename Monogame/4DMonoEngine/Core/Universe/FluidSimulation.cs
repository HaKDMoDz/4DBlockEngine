using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

//TODO : get this cleaned up and running
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Universe
{
    public class FluidSimulation
    {
        private const float MaxLevel = 1;
        private const float MaxFlow = 1;
        private const float EvaporationLevel = 0.08f;
        private const float MinLevel = 0.0001f;
        private const float MaxCompression = 0.07f;
        private const float EvaporationRate = 0.005f;
        private const float MinFlow = 0.000001f;
        private Object m_world; //TODO: What should this be
        protected List<FluidContainer> m_containers;
        protected bool m_stepDone;
        private bool m_setupDone;

        private readonly long m_maskXz;
        private readonly long m_maskY;
        private readonly int m_shiftX;
        private readonly int m_shiftY;

        protected FluidCell solidCell;
        protected List<FluidCell> m_cellAccumulator;
        protected readonly Queue<List<FluidCell>> addQueue;

        //TODO : make a physics thread and put this on it

        public FluidSimulation()
        {
            /*m_world = world;
            var bitsXZ = (int) Math.Log((uint) world.width*2, 2);
            var bitsY = (int) Math.Log(world.height, 2);
            m_shiftX = bitsY + bitsXZ;
            m_shiftY = bitsXZ;
            m_maskXz = (long) Mathf.Pow(2, bitsXZ) - 1;
            m_maskY = (long) Mathf.Pow(2, bitsY) - 1;*/
            m_containers = new List<FluidContainer>();
            solidCell = new FluidCell(CellType.Solid);
            addQueue = new Queue<List<FluidCell>>();
            m_cellAccumulator = new List<FluidCell>();
        }

        public long CellHash(long x, long y, long z)
        {
            throw new NotImplementedException();
        }

        public void AddFluidAt(int x, int y, int z, int w, float amount, bool isSource)
        {
            if (!(amount > 0))
            {
                return;
            }
            var cell = new FluidCell(x, y, z, w, CellType.Water, amount) {isSource = isSource};
            foreach (var existing in m_cellAccumulator)
            {
                if (existing.x != cell.x || existing.y != cell.y || existing.z != cell.z || existing.w != cell.w)
                {
                    continue;
                }
                existing.levelNextStep = MathHelper.Clamp(existing.levelNextStep + amount, 0 , 1);
                existing.level = existing.levelNextStep;
                cell = null;
                break;
            }
            if (cell != null)
            {
                m_cellAccumulator.Add(cell);
            }
        }

        public void UpdateBlockAt(int x, int y, int z, int w, bool solid)
        {
            foreach (var container in m_containers)
            {
                if (!(container.alive && container.Contains(x, y, z) && container.w == w))
                {
                    continue;
                }
                var hash = CellHash(x, y, z);
                container.update = true;
                if (container.cellDictionary.ContainsKey(hash))
                {
                    var cell = container.cellDictionary[hash];
                    if (solid)
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
                for (var i = -1; i <= 1; ++i)
                {
                    if (i == 0) continue;
                    hash = CellHash(x + i, y, z);
                    if (container.cellDictionary.ContainsKey(hash))
                    {
                        var cell = container.cellDictionary[hash];
                        cell.awake = true;
                        container.BuildNeighborhood(cell);
                    }
                    hash = CellHash(x, y + i, z);
                    if (container.cellDictionary.ContainsKey(hash))
                    {
                        var cell = container.cellDictionary[hash];
                        cell.awake = true;
                        container.BuildNeighborhood(cell);
                    }
                    hash = CellHash(x, y, z + i);
                    if (container.cellDictionary.ContainsKey(hash))
                    {
                        var cell = container.cellDictionary[hash];
                        cell.awake = true;
                        container.BuildNeighborhood(cell);
                    }
                }
            }
        }
        public void Update(GameTime gameTime)
        {
            if (!m_setupDone)
            {
                return;
            }
            if (m_cellAccumulator.Count > 0)
            {
                addQueue.Enqueue(m_cellAccumulator);
                m_cellAccumulator = new List<FluidCell>();
            }
            TickSimulation();
        }

        private void TickSimulation()
        {
            if (addQueue.Count > 0)
            {
                var cellsToAdd = addQueue.Dequeue();
                var container = new FluidContainer {world = m_world, Simulation = this, w = 0};
                m_containers.Add(container);
                foreach (var next in cellsToAdd)
                {
                    var cellToAdd = next;
                    container.Add(cellToAdd);
                }
            }
            foreach (var container in m_containers)
            {
                if (container.update)
                {
                    container.Step();
                }
            }
            var keepList = new List<FluidContainer>();
            for (var i = 0; i < m_containers.Count; ++i)
            {
                var keepContainer = true;
                var discard = m_containers[i];
                if (!discard.alive)
                {
                    continue;
                }
                for (var j = i + 1; j < m_containers.Count; ++j)
                {
                    var keep = m_containers[j];
                    if (!(keep.alive && discard.Intersects(keep)))
                    {
                        continue;
                    }
                    foreach (var cell in discard.cells)
                    {
                        keep.Add(cell);
                    }
                    keepContainer = false;
                    break;
                }
                if (keepContainer)
                {
                    keepList.Add(discard);
                }
            }
            m_containers = keepList;
        }

        protected class FluidContainer
        {
            public Object world; //TODO : Simulation? ChunckCache? Some sort of Facade?
            public FluidSimulation Simulation;
            private Vector3Int m_min;
            private Vector3Int m_max;
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
                m_min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
                m_max = new Vector3Int(-int.MaxValue, -int.MaxValue, -int.MaxValue);
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
                return ((x >= m_min.X && x <= m_max.X) &&
                        (y >= m_min.Y && y <= m_max.Y) &&
                        (z >= m_min.Z && z <= m_max.Z));
            }

            public bool Intersects(FluidContainer container)
            {
                if (container.m_min.X > m_max.X || m_min.X > container.m_max.X)
                {
                    return false;
                }
                if (container.m_min.Y > m_max.Y || m_min.Y > container.m_max.Y)
                {
                    return false;
                }
                return container.m_min.Z <= m_max.Z && m_min.Z <= container.m_max.Z;
            }

            public void Add(FluidCell cell)
            {
                var cellHash = Simulation.CellHash(cell.x, cell.y, cell.z);
                if (cellDictionary.ContainsKey(cellHash))
                {
                    var existingCell = cellDictionary[cellHash];
                    if (existingCell.type != CellType.Solid)
                    {
                        existingCell.level += cell.level;
                        existingCell.levelNextStep += cell.level;
                        if (existingCell.levelNextStep > MinLevel && existingCell.type == CellType.Air)
                        {
                            existingCell.type = CellType.Water;
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

            //TODO : break this out into sub functions
            public void Step()
            {
                foreach (var cell in cells)
                {
                    if (cell.awake && cell.type == CellType.Water) //In theory this check is redundant...
                    {
                        cell.Propagate();
                    }
                }
                var hasChanged = false;
                var potentialPruningNeeded = false;
                foreach (var cell in updated)
                {
                    if (cell.type == CellType.Solid)
                    {
                        hasChanged = true;
                        potentialPruningNeeded = true;
                    }
                    if (Math.Abs(cell.level - cell.levelNextStep) >= MinFlow/2.0f)
                    {
                        hasChanged = true;
                        cell.awake = true;
                    }
                    cell.level = cell.levelNextStep;
                    if (cell.type == CellType.Water && cell.level < MinLevel)
                    {
                        potentialPruningNeeded = true;
                        cell.type = CellType.Air;
                    }
                    if (!(cell.type == CellType.Air && cell.level > MinLevel))
                    {
                        continue;
                    }
                    cell.type = CellType.Water;
                    RecalculateBounds(cell);
                    BuildNeighborhood(cell);
                    cells.Add(cell);
                }
                updated.Clear();
                if (potentialPruningNeeded)
                {
                    m_min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
                    m_max = new Vector3Int(-int.MaxValue, -int.MaxValue, -int.MaxValue);
                    var removeList = new List<FluidCell>();
                    foreach (var cell in cells)
                    {
                        if (cell.type == CellType.Solid ||
                            cell.type == CellType.Air)
                        {
                            removeList.Add(cell);
                        }
                        else
                        {
                            RecalculateBounds(cell);
                        }
                    }
                    foreach (var cell in removeList)
                    {
                        cells.Remove(cell);
                        if ((cell.up != null && cell.up.type == CellType.Water) ||
                            (cell.north != null && cell.north.type == CellType.Water) ||
                            (cell.east != null && cell.east.type == CellType.Water) ||
                            (cell.south != null && cell.south.type == CellType.Water) ||
                            (cell.west != null && cell.west.type == CellType.Water) ||
                            (cell.down != null && cell.down.type == CellType.Water))
                        {
                            continue;
                        }
                        var cellHash = Simulation.CellHash(cell.x, cell.y, cell.z);
                        cellDictionary.Remove(cellHash);
                    }
                }
                alive = cells.Count != 0;
                update = hasChanged;
            }

            private void RecalculateBounds(FluidCell cell)
            {
                if (cell.x > m_max.X)
                {
                    m_max.X = cell.x + 1;
                }
                if (cell.x < m_min.X)
                {
                    m_min.X = cell.x - 1;
                }

                if (cell.y > m_max.Y)
                {
                    m_max.Y = cell.y + 1;
                }
                if (cell.y < m_min.Y)
                {
                    m_min.Y = cell.y - 1;
                }

                if (cell.z > m_max.Z)
                {
                    m_max.Z = cell.z + 1;
                }
                if (cell.z < m_min.Z)
                {
                    m_min.Z = cell.z - 1;
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
                byte block = 0;//TODO : world.GetBlockAt(x, y, z, w);
                if (block != 0)
                {
                    cell = Simulation.solidCell;
                }
                else
                {
                    var cellHash = Simulation.CellHash(x, y, z);
                    if (cellDictionary.ContainsKey(cellHash))
                    {
                        cell = cellDictionary[cellHash];
                    }
                    else
                    {
                        cell = new FluidCell(x, y, z, w, CellType.Air, 0) {container = this};
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

            //TODO : break into sub functions
            public void Propagate()
            {
                if (isSource)
                {
                    level = MathHelper.Max(level, 1);
                    levelNextStep = level;
                }
                var levelRemaining = level;
                float outFlow = 0;
                if (level > MaxLevel && levelRemaining > (up.level + MaxCompression*2) && up.type != CellType.Solid)
                {
                    outFlow = ClampFlow(levelRemaining - GetStableStateVertical(up.level, levelRemaining),
                        levelRemaining);
                    levelNextStep -= outFlow;
                    levelRemaining -= outFlow;
                    up.levelNextStep += outFlow;
                    up.awake = true;
                    container.updated.Add(up);
                }
                else
                {
                    if (down.type != CellType.Solid)
                    {
                        outFlow = ClampFlow(GetStableStateVertical(levelRemaining, down.level) - down.level,
                            levelRemaining);
                        if (outFlow > 0)
                        {
                            levelNextStep -= outFlow;
                            levelRemaining -= outFlow;
                            down.levelNextStep += outFlow;
                            down.awake = true;
                            container.updated.Add(down);
                        }
                    }
                    if (levelRemaining > 0)
                    {
                        float average = 0;
                        var count = 0;
                        if (north.type != CellType.Solid && north.level < level)
                        {
                            average += north.level;
                            count += 1;
                        }
                        if (east.type != CellType.Solid && east.level < level)
                        {
                            average += east.level;
                            count += 1;
                        }
                        if (south.type != CellType.Solid && south.level < level)
                        {
                            average += south.level;
                            count += 1;
                        }
                        if (west.type != CellType.Solid && west.level < level)
                        {
                            average += west.level;
                            count += 1;
                        }
                        if (count > 0)
                        {
                            average = average/count;
                            outFlow = ClampFlow(levelRemaining - average, levelRemaining);
                            if (outFlow > 0)
                            {
                                levelNextStep -= outFlow;
                                levelRemaining -= outFlow;
                                outFlow /= count;
                                if (north.type != CellType.Solid && north.level <= levelRemaining)
                                {
                                    north.levelNextStep += outFlow;
                                    north.awake = true;
                                    container.updated.Add(north);
                                }
                                if (east.type != CellType.Solid && east.level <= levelRemaining)
                                {
                                    east.levelNextStep += outFlow;
                                    east.awake = true;
                                    container.updated.Add(east);
                                }
                                if (south.type != CellType.Solid && south.level <= levelRemaining)
                                {
                                    south.levelNextStep += outFlow;
                                    south.awake = true;
                                    container.updated.Add(south);
                                }
                                if (west.type != CellType.Solid && west.level <= levelRemaining)
                                {
                                    west.levelNextStep += outFlow;
                                    west.awake = true;
                                    container.updated.Add(west);
                                }
                            }
                        }
                    }
                    if (levelRemaining > 0)
                    {
                        if (up.type != CellType.Solid)
                        {
                            outFlow = ClampFlow(levelRemaining - GetStableStateVertical(up.level, levelRemaining),
                                levelRemaining);
                            if (outFlow > 0)
                            {
                                levelNextStep -= outFlow;
                                levelRemaining -= outFlow;
                                up.levelNextStep += outFlow;
                                up.awake = true;
                                container.updated.Add(up);
                            }
                        }
                        if (levelRemaining <= EvaporationLevel && up.type == CellType.Air)
                        {
                            levelNextStep -= EvaporationRate;
                        }
                    }
                }
                if (Math.Abs(level - levelNextStep) >= MinFlow / 2.0f)
                {
                    container.updated.Add(this);
                }
                else if (!isSource)
                {
                    awake = false;
                }
            }

            private static float ClampFlow(float flow, float level)
            {
                if (flow > MinFlow)
                {
                    flow *= 0.5f;
                }
                return MathHelper.Clamp(flow, 0, MathHelper.Min(level, MaxFlow));
            }

            private static float GetStableStateVertical(float cell, float down)
            {
                var sum = cell + down;
                float newDown;
                if (sum <= MaxLevel)
                {
                    newDown = MaxLevel;
                }
                else if (sum < 2*MaxLevel + MaxCompression)
                {
                    newDown = (MaxLevel*MaxLevel + sum*MaxCompression)/(MaxLevel + MaxCompression);
                }
                else
                {
                    newDown = (sum + MaxCompression)/2;
                }
                return newDown;
            }
        }

        public enum CellType
        {
            Solid,
            Water,
            Air
        }
    }
}
