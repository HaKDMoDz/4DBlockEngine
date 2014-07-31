using System.Collections.Generic;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Utils;

namespace _4DMonoEngine.Core.Universe.Fluids
{
    public class FluidSimulation
    {
        private List<FluidContainer> m_containers;
        private readonly FluidCell m_solidCell;
        private List<FluidCell> m_cellAccumulator;
        private readonly Queue<List<FluidCell>> m_addQueue;
        private readonly MappingFunction m_mappingFunction;
        private readonly Block[] m_blocks;

        //TODO : make a physics thread and put this on it

        public FluidSimulation(MappingFunction mappingFunction, Block[] blocks)
        {
            m_mappingFunction = mappingFunction;
            m_blocks = blocks;
            m_containers = new List<FluidContainer>();
            m_solidCell = new FluidCell(FluidCell.CellType.Solid);
            m_addQueue = new Queue<List<FluidCell>>();
            m_cellAccumulator = new List<FluidCell>();
        }

        public void AddFluidAt(int x, int y, int z,float amount, bool isSource)
        {
            if (!(amount > 0))
            {
                return;
            }
            var cell = new FluidCell(x, y, z, FluidCell.CellType.Water, amount) {IsSource = isSource};
            foreach (var existing in m_cellAccumulator)
            {
                if (existing.X != cell.X || existing.Y != cell.Y || existing.Z != cell.Z)
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
                container.Update = true;
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
            if (m_cellAccumulator.Count > 0)
            {
                m_addQueue.Enqueue(m_cellAccumulator);
                m_cellAccumulator = new List<FluidCell>();
            }
            TickSimulation();
        }

        private void TickSimulation()
        {
            if (m_addQueue.Count > 0)
            {
                var cellsToAdd = m_addQueue.Dequeue();
                var container = new FluidContainer(m_mappingFunction, m_blocks, m_solidCell);
                m_containers.Add(container);
                foreach (var next in cellsToAdd)
                {
                    var cellToAdd = next;
                    container.Add(cellToAdd);
                }
            }
            foreach (var container in m_containers)
            {
                if (container.Update)
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
    }
}
