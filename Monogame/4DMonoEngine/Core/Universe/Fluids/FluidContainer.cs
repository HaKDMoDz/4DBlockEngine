using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Universe.Fluids
{
    internal class FluidContainer
    {
        private Vector3Int m_min;
        private Vector3Int m_max;
        public bool Update; //does this area need to update?
        public bool Alive;
        public readonly List<FluidCell> Cells;
        public readonly Dictionary<long, FluidCell> CellDictionary;
        public readonly List<FluidCell> Updated;
        private readonly MappingFunction m_mappingFunction;
        private readonly Block[] m_blocks;
        private readonly FluidCell m_solidCellReference;


        public FluidContainer(MappingFunction mappingFunction, Block[] blocks, FluidCell solidCellReference)
        {
            m_mappingFunction = mappingFunction;
            m_blocks = blocks;
            m_solidCellReference = solidCellReference;
            Update = true;
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
                if (existingCell.Type != FluidCell.CellType.Solid)
                {
                    existingCell.Level += cell.Level;
                    existingCell.LevelNextStep += cell.Level;
                    if (existingCell.LevelNextStep > FluidCell.MinLevel && existingCell.Type == FluidCell.CellType.Air)
                    {
                        existingCell.Type = FluidCell.CellType.Water;
                        Cells.Add(existingCell);
                        BuildNeighborhood(existingCell);
                        RecalculateBounds(existingCell);
                    }
                }
            }
            else
            {
                cell.UpdateCells = Updated;
                Cells.Add(cell);
                CellDictionary.Add(cellHash, cell);
                BuildNeighborhood(cell);
                RecalculateBounds(cell);
            }
            Update = true;
        }

        //TODO : break this out into sub functions
        public void Step()
        {
            foreach (var cell in Cells)
            {
                if (cell.Awake && cell.Type == FluidCell.CellType.Water) //In theory this check is redundant...
                {
                    cell.Propagate();
                }
            }
            var hasChanged = false;
            var potentialPruningNeeded = false;
            foreach (var cell in Updated)
            {
                if (cell.Type == FluidCell.CellType.Solid)
                {
                    hasChanged = true;
                    potentialPruningNeeded = true;
                }
                if (Math.Abs(cell.Level - cell.LevelNextStep) >= FluidCell.MinFlow / 2.0f)
                {
                    hasChanged = true;
                    cell.Awake = true;
                }
                cell.Level = cell.LevelNextStep;
                if (cell.Type == FluidCell.CellType.Water && cell.Level < FluidCell.MinLevel)
                {
                    potentialPruningNeeded = true;
                    cell.Type = FluidCell.CellType.Air;
                }
                if (!(cell.Type == FluidCell.CellType.Air && cell.Level > FluidCell.MinLevel))
                {
                    continue;
                }
                cell.Type = FluidCell.CellType.Water;
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
                    if (cell.Type == FluidCell.CellType.Solid ||
                        cell.Type == FluidCell.CellType.Air)
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
                    if ((cell.Up != null && cell.Up.Type == FluidCell.CellType.Water) ||
                        (cell.North != null && cell.North.Type == FluidCell.CellType.Water) ||
                        (cell.East != null && cell.East.Type == FluidCell.CellType.Water) ||
                        (cell.South != null && cell.South.Type == FluidCell.CellType.Water) ||
                        (cell.West != null && cell.West.Type == FluidCell.CellType.Water) ||
                        (cell.Down != null && cell.Down.Type == FluidCell.CellType.Water))
                    {
                        continue;
                    }
                    var cellHash = m_mappingFunction(cell.X, cell.Y, cell.Z);
                    CellDictionary.Remove(cellHash);
                }
            }
            Alive = Cells.Count != 0;
            Update = hasChanged;
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
            var cellHash = m_mappingFunction(x, y, z);
            var block = m_blocks[cellHash].Type;
            if (block != 0)
            {
                cell = m_solidCellReference;
            }
            else
            {
                if (CellDictionary.ContainsKey(cellHash))
                {
                    cell = CellDictionary[cellHash];
                }
                else
                {
                    cell = new FluidCell(x, y, z, FluidCell.CellType.Air, 0) {UpdateCells = Updated};
                    CellDictionary.Add(cellHash, cell);
                }
            }
            return cell;
        }
    }
}