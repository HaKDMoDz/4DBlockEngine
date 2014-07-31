using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Universe.Fluids
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
        private bool m_setupDone;
        protected FluidCell solidCell;
        protected List<FluidCell> m_cellAccumulator;
        protected readonly Queue<List<FluidCell>> addQueue;
        private readonly MappingFunction m_mappingFunction;
        private readonly Block[] m_blocks;

        //TODO : make a physics thread and put this on it

        public FluidSimulation(MappingFunction mappingFunction, Block[] blocks)
        {
            m_mappingFunction = mappingFunction;
            m_blocks = blocks;
            m_containers = new List<FluidContainer>();
            solidCell = new FluidCell(CellType.Solid);
            addQueue = new Queue<List<FluidCell>>();
            m_cellAccumulator = new List<FluidCell>();
        }

        public void AddFluidAt(int x, int y, int z, int w, float amount, bool isSource)
        {
            if (!(amount > 0))
            {
                return;
            }
            var cell = new FluidCell(x, y, z, w, CellType.Water, amount) {IsSource = isSource};
            foreach (var existing in m_cellAccumulator)
            {
                if (existing.X != cell.X || existing.Y != cell.Y || existing.Z != cell.Z || existing.W != cell.W)
                {
                    continue;
                }
                existing.LevelNextStep = MathHelper.Clamp(existing.LevelNextStep + amount, 0 , 1);
                existing.Level = existing.LevelNextStep;
                cell = null;
                break;
            }
            if (cell != null)
            {
                m_cellAccumulator.Add(cell);
            }
        }

        public void UpdateBlockAt(int x, int y, int z, bool solid)
        {
            foreach (var container in m_containers)
            {
                if (!(container.Alive && container.Contains(x, y, z)))
                {
                    continue;
                }
                var hash = m_mappingFunction(x, y, z);
                container.update = true;
                if (container.CellDictionary.ContainsKey(hash))
                {
                    var cell = container.CellDictionary[hash];
                    if (solid)
                    {
                        container.CellDictionary.Remove(hash);
                        container.Cells.Remove(cell);
                    }
                    else
                    {
                        cell.Awake = true;
                        container.BuildNeighborhood(cell);
                    }
                }
                //TODO : this is crap
                for (var i = -1; i <= 1; ++i)
                {
                    if (i == 0)
                    {
                        continue;
                    }
                    FluidCell cell;
                    hash = m_mappingFunction(x + i, y, z);
                    if (container.CellDictionary.ContainsKey(hash))
                    {
                        cell = container.CellDictionary[hash];
                        cell.Awake = true;
                        container.BuildNeighborhood(cell);
                    }
                    hash = m_mappingFunction(x, y + i, z);
                    if (container.CellDictionary.ContainsKey(hash))
                    {
                        cell = container.CellDictionary[hash];
                        cell.Awake = true;
                        container.BuildNeighborhood(cell);
                    }
                    hash = m_mappingFunction(x, y, z + i);
                    if (!container.CellDictionary.ContainsKey(hash))
                    {
                        continue;
                    }
                    cell = container.CellDictionary[hash];
                    cell.Awake = true;
                    container.BuildNeighborhood(cell);
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
                var container = new FluidContainer(m_mappingFunction);
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
                if (!discard.Alive)
                {
                    continue;
                }
                for (var j = i + 1; j < m_containers.Count; ++j)
                {
                    var keep = m_containers[j];
                    if (!(keep.Alive && discard.Intersects(keep)))
                    {
                        continue;
                    }
                    foreach (var cell in discard.Cells)
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
            private Vector3Int m_min;
            private Vector3Int m_max;
            public bool update; //does this area need to update?
            public bool Alive;
            public readonly List<FluidCell> Cells;
            public readonly Dictionary<long, FluidCell> CellDictionary;
            public readonly List<FluidCell> Updated;
            private readonly MappingFunction m_mappingFunction;


            public FluidContainer(MappingFunction mappingFunction)
            {
                m_mappingFunction = mappingFunction;
                update = true;
                Alive = true;
                m_min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
                m_max = new Vector3Int(-int.MaxValue, -int.MaxValue, -int.MaxValue);
                Cells = new List<FluidCell>();
                CellDictionary = new Dictionary<long, FluidCell>();
                Updated = new List<FluidCell>();
            }

            public bool Contains(FluidCell cell)
            {
                return Contains(cell.X, cell.Y, cell.Z);
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
                var cellHash = m_mappingFunction(cell.X, cell.Y, cell.Z);
                if (CellDictionary.ContainsKey(cellHash))
                {
                    var existingCell = CellDictionary[cellHash];
                    if (existingCell.Type != CellType.Solid)
                    {
                        existingCell.Level += cell.Level;
                        existingCell.LevelNextStep += cell.Level;
                        if (existingCell.LevelNextStep > MinLevel && existingCell.Type == CellType.Air)
                        {
                            existingCell.Type = CellType.Water;
                            Cells.Add(existingCell);
                            BuildNeighborhood(existingCell);
                            RecalculateBounds(existingCell);
                        }
                    }
                }
                else
                {
                    cell.Container = this;
                    Cells.Add(cell);
                    CellDictionary.Add(cellHash, cell);
                    BuildNeighborhood(cell);
                    RecalculateBounds(cell);
                }
                update = true;
            }

            //TODO : break this out into sub functions
            public void Step()
            {
                foreach (var cell in Cells)
                {
                    if (cell.Awake && cell.Type == CellType.Water) //In theory this check is redundant...
                    {
                        cell.Propagate();
                    }
                }
                var hasChanged = false;
                var potentialPruningNeeded = false;
                foreach (var cell in Updated)
                {
                    if (cell.Type == CellType.Solid)
                    {
                        hasChanged = true;
                        potentialPruningNeeded = true;
                    }
                    if (Math.Abs(cell.Level - cell.LevelNextStep) >= MinFlow/2.0f)
                    {
                        hasChanged = true;
                        cell.Awake = true;
                    }
                    cell.Level = cell.LevelNextStep;
                    if (cell.Type == CellType.Water && cell.Level < MinLevel)
                    {
                        potentialPruningNeeded = true;
                        cell.Type = CellType.Air;
                    }
                    if (!(cell.Type == CellType.Air && cell.Level > MinLevel))
                    {
                        continue;
                    }
                    cell.Type = CellType.Water;
                    RecalculateBounds(cell);
                    BuildNeighborhood(cell);
                    Cells.Add(cell);
                }
                Updated.Clear();
                if (potentialPruningNeeded)
                {
                    m_min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
                    m_max = new Vector3Int(-int.MaxValue, -int.MaxValue, -int.MaxValue);
                    var removeList = new List<FluidCell>();
                    foreach (var cell in Cells)
                    {
                        if (cell.Type == CellType.Solid ||
                            cell.Type == CellType.Air)
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
                        Cells.Remove(cell);
                        if ((cell.Up != null && cell.Up.Type == CellType.Water) ||
                            (cell.North != null && cell.North.Type == CellType.Water) ||
                            (cell.East != null && cell.East.Type == CellType.Water) ||
                            (cell.South != null && cell.South.Type == CellType.Water) ||
                            (cell.West != null && cell.West.Type == CellType.Water) ||
                            (cell.Down != null && cell.Down.Type == CellType.Water))
                        {
                            continue;
                        }
                        var cellHash = m_mappingFunction(cell.X, cell.Y, cell.Z);
                        CellDictionary.Remove(cellHash);
                    }
                }
                Alive = Cells.Count != 0;
                update = hasChanged;
            }

            private void RecalculateBounds(FluidCell cell)
            {
                if (cell.X > m_max.X)
                {
                    m_max.X = cell.X + 1;
                }
                if (cell.X < m_min.X)
                {
                    m_min.X = cell.X - 1;
                }

                if (cell.Y > m_max.Y)
                {
                    m_max.Y = cell.Y + 1;
                }
                if (cell.Y < m_min.Y)
                {
                    m_min.Y = cell.Y - 1;
                }

                if (cell.Z > m_max.Z)
                {
                    m_max.Z = cell.Z + 1;
                }
                if (cell.Z < m_min.Z)
                {
                    m_min.Z = cell.Z - 1;
                }
            }

            public void BuildNeighborhood(FluidCell cell)
            {
                cell.Up = GetOrCreateCell(cell.X, cell.Y + 1, cell.Z);
                cell.Up.Down = cell;
                cell.Down = GetOrCreateCell(cell.X, cell.Y - 1, cell.Z);
                cell.Down.Up = cell;
                cell.North = GetOrCreateCell(cell.X, cell.Y, cell.Z + 1);
                cell.North.South = cell;
                cell.East = GetOrCreateCell(cell.X + 1, cell.Y, cell.Z);
                cell.East.West = cell;
                cell.South = GetOrCreateCell(cell.X, cell.Y, cell.Z - 1);
                cell.South.North = cell;
                cell.West = GetOrCreateCell(cell.X - 1, cell.Y, cell.Z);
                cell.West.East = cell;
            }

            private FluidCell GetOrCreateCell(int x, int y, int z)
            {
                FluidCell cell;
                byte block = GetBlockAt(x, y, z);
                if (block != 0)
                {
                    cell = Simulation.solidCell;
                }
                else
                {
                    var cellHash = m_mappingFunction(x, y, z);
                    if (CellDictionary.ContainsKey(cellHash))
                    {
                        cell = CellDictionary[cellHash];
                    }
                    else
                    {
                        cell = new FluidCell(x, y, z, W, CellType.Air, 0) {Container = this};
                        CellDictionary.Add(cellHash, cell);
                    }
                }
                return cell;
            }
        }

        protected class FluidCell
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;
            public readonly int W;
            public float Level;
            public float LevelNextStep;
            public CellType Type;
            public FluidCell Up;
            public FluidCell Down;
            public FluidCell North;
            public FluidCell East;
            public FluidCell South;
            public FluidCell West;
            public FluidContainer Container;
            public bool IsSource;
            public bool Awake;

            public FluidCell(CellType type)
            {
                Type = type;
            }

            public FluidCell(int x, int y, int z, int w, CellType type, float level)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
                Type = type;
                Level = level;
                LevelNextStep = level;
                Awake = true;
            }

            //TODO : break into sub functions
            public void Propagate()
            {
                if (IsSource)
                {
                    Level = MathHelper.Max(Level, 1);
                    LevelNextStep = Level;
                }
                var levelRemaining = Level;
                float outFlow = 0;
                if (Level > MaxLevel && levelRemaining > (Up.Level + MaxCompression*2) && Up.Type != CellType.Solid)
                {
                    outFlow = ClampFlow(levelRemaining - GetStableStateVertical(Up.Level, levelRemaining),
                        levelRemaining);
                    LevelNextStep -= outFlow;
                    levelRemaining -= outFlow;
                    Up.LevelNextStep += outFlow;
                    Up.Awake = true;
                    Container.Updated.Add(Up);
                }
                else
                {
                    if (Down.Type != CellType.Solid)
                    {
                        outFlow = ClampFlow(GetStableStateVertical(levelRemaining, Down.Level) - Down.Level,
                            levelRemaining);
                        if (outFlow > 0)
                        {
                            LevelNextStep -= outFlow;
                            levelRemaining -= outFlow;
                            Down.LevelNextStep += outFlow;
                            Down.Awake = true;
                            Container.Updated.Add(Down);
                        }
                    }
                    if (levelRemaining > 0)
                    {
                        float average = 0;
                        var count = 0;
                        if (North.Type != CellType.Solid && North.Level < Level)
                        {
                            average += North.Level;
                            count += 1;
                        }
                        if (East.Type != CellType.Solid && East.Level < Level)
                        {
                            average += East.Level;
                            count += 1;
                        }
                        if (South.Type != CellType.Solid && South.Level < Level)
                        {
                            average += South.Level;
                            count += 1;
                        }
                        if (West.Type != CellType.Solid && West.Level < Level)
                        {
                            average += West.Level;
                            count += 1;
                        }
                        if (count > 0)
                        {
                            average = average/count;
                            outFlow = ClampFlow(levelRemaining - average, levelRemaining);
                            if (outFlow > 0)
                            {
                                LevelNextStep -= outFlow;
                                levelRemaining -= outFlow;
                                outFlow /= count;
                                if (North.Type != CellType.Solid && North.Level <= levelRemaining)
                                {
                                    North.LevelNextStep += outFlow;
                                    North.Awake = true;
                                    Container.Updated.Add(North);
                                }
                                if (East.Type != CellType.Solid && East.Level <= levelRemaining)
                                {
                                    East.LevelNextStep += outFlow;
                                    East.Awake = true;
                                    Container.Updated.Add(East);
                                }
                                if (South.Type != CellType.Solid && South.Level <= levelRemaining)
                                {
                                    South.LevelNextStep += outFlow;
                                    South.Awake = true;
                                    Container.Updated.Add(South);
                                }
                                if (West.Type != CellType.Solid && West.Level <= levelRemaining)
                                {
                                    West.LevelNextStep += outFlow;
                                    West.Awake = true;
                                    Container.Updated.Add(West);
                                }
                            }
                        }
                    }
                    if (levelRemaining > 0)
                    {
                        if (Up.Type != CellType.Solid)
                        {
                            outFlow = ClampFlow(levelRemaining - GetStableStateVertical(Up.Level, levelRemaining),
                                levelRemaining);
                            if (outFlow > 0)
                            {
                                LevelNextStep -= outFlow;
                                levelRemaining -= outFlow;
                                Up.LevelNextStep += outFlow;
                                Up.Awake = true;
                                Container.Updated.Add(Up);
                            }
                        }
                        if (levelRemaining <= EvaporationLevel && Up.Type == CellType.Air)
                        {
                            LevelNextStep -= EvaporationRate;
                        }
                    }
                }
                if (Math.Abs(Level - LevelNextStep) >= MinFlow / 2.0f)
                {
                    Container.Updated.Add(this);
                }
                else if (!IsSource)
                {
                    Awake = false;
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
