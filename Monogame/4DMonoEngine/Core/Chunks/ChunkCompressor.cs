using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Chunks
{
    public class ChunkCompressor
    {
        public static ChunkCompressor GetCompressor(int chunkSize)
        {
            if (!Compressors.ContainsKey(chunkSize))
            {
                Compressors[chunkSize] = new ChunkCompressor(chunkSize);
            }
            return Compressors[chunkSize];
        }
        private static readonly Dictionary<int, ChunkCompressor> Compressors = new Dictionary<int, ChunkCompressor>();

        private int[] m_hilbertCurve;
        private int m_chunkSize;
        private ChunkCompressor() : this(32) {}

        private ChunkCompressor(int chunkSize)
        {
            m_chunkSize = chunkSize;
            SetupHilbertCurve(chunkSize);
        }

        private void SetupHilbertCurve(int chunkSize)
        {
            var blockStepX = chunkSize * chunkSize;
            var blockCount = chunkSize * chunkSize * chunkSize;
            var bitsPerAxis = (int)(Math.Ceiling(Math.Log(chunkSize, 2)));
            m_hilbertCurve = new int[blockCount];
            for (uint index = 0; index < blockCount; ++index)
            {
                var arr = HilbertCurve.HilbertAxes(index, 3, bitsPerAxis);
                var blockIndex = arr[0] * blockStepX + arr[1] * chunkSize + arr[2];
                m_hilbertCurve[index] = (int)blockIndex;
            }
        }

        public IntervalTree<Interval<Block, ushort>, ushort> ConvertArrayToIntervalTree(Block[] blocks, out float compressionRatio, CompressionFlag flag = CompressionFlag.None)
        {
            ScanDirection scanDirection = ScanDirection.Xyz;
            CompressionMode compressionMode;
            if (flag == CompressionFlag.None)
            {
                float hilbertCompressionRatio;
                float linearCompressionRatio;
                EvaluateHilbertTree(blocks, out hilbertCompressionRatio);
                EvaluateLinearTree(blocks, out linearCompressionRatio, out scanDirection);
                compressionMode = hilbertCompressionRatio > linearCompressionRatio ? CompressionMode.Hilbert : CompressionMode.Linear;
            }
            else
            {
                switch (flag)
                {
                    case CompressionFlag.LinearXyz:
                        compressionMode = CompressionMode.Linear;
                        scanDirection = ScanDirection.Xyz;
                        break;
                    case CompressionFlag.LinearXzy:
                        compressionMode = CompressionMode.Linear;
                        scanDirection = ScanDirection.Xzy;
                        break;
                    case CompressionFlag.LinearYxz:
                        compressionMode = CompressionMode.Linear;
                        scanDirection = ScanDirection.Yxz;
                        break;
                    case CompressionFlag.LinearYzx:
                        compressionMode = CompressionMode.Linear;
                        scanDirection = ScanDirection.Yzx;
                        break;
                    case CompressionFlag.LinearZxy:
                        compressionMode = CompressionMode.Linear;
                        scanDirection = ScanDirection.Zxy;
                        break;
                    case CompressionFlag.LinearZyx:
                        compressionMode = CompressionMode.Linear;
                        scanDirection = ScanDirection.Zyx;
                        break;
                    case CompressionFlag.Hilbert:
                        compressionMode = CompressionMode.Hilbert;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("flag");
                }
            }
            if (compressionMode == CompressionMode.Hilbert)
            {
                //Console.Out.WriteLine(CompressionMode.Hilbert.ToString());
                return ConvertArrayToIntervalTreeHilbert(blocks, out compressionRatio);
            }
            else
            {
                //Console.Out.WriteLine(scanDirection.ToString());
                return ConvertArrayToIntervalTreeLinear(blocks, scanDirection, out compressionRatio);
            }
        }

        private void EvaluateHilbertTree(Block[] blocks, out float compressionRatio)
        {
            var nodesRemoved = m_hilbertCurve.Length;
            var current = Block.Empty;
            foreach (var index in m_hilbertCurve)
            {
                var block = blocks[index];
                if (block == current)
                {
                    continue;
                }
                current = block;
                --nodesRemoved;
            }
            compressionRatio = nodesRemoved / (float)m_hilbertCurve.Length;
        }

        private void EvaluateLinearTree(Block[] blocks, out float compressionRatio, out ScanDirection optimalDirection)
        {
             var count = m_chunkSize * m_chunkSize * m_chunkSize;
            compressionRatio = 0;
            optimalDirection = ScanDirection.Xyz;
            foreach (ScanDirection scanDirection in Enum.GetValues(typeof(ScanDirection)))
            {
                var nodesRemoved = count;
                Block current = Block.Empty;
                Vector3Int workCoords = new Vector3Int();
                for (var i = 0; i < count; ++i)
                {
                    var block = blocks[GetNextIndex(scanDirection, m_chunkSize, ref workCoords)];
                    if (block == current)
                    {
                        continue;
                    }
                    current = block;
                    --nodesRemoved;
                }
                var currentRatio = nodesRemoved / (float)count;
                if (currentRatio <= compressionRatio)
                {
                    continue;
                }
                compressionRatio = currentRatio;
                optimalDirection = scanDirection;
                //this means we are mostly air and can just exit out now!
                if (currentRatio > 0.97f)
                {
                    break;
                }
            }
        }

        private IntervalTree<Interval<Block, ushort>, ushort> ConvertArrayToIntervalTreeHilbert(Block[] blocks,  out float compressionRatio)
        {
            var tree = new IntervalTree<Interval<Block, ushort>, ushort>(new IntervalSelector<Block, ushort>());
            var nodesRemoved = m_hilbertCurve.Length;
            var min = 0;
            var max = 0;
            var current = Block.Empty;
            var started = false;
            var intervals = new List<Interval<Block, ushort>>();
            for (var i = 0; i < m_hilbertCurve.Length; ++i)
            {
                var block = blocks[m_hilbertCurve[i]];
                if (!(started && block == current))
                {
                    if (started)
                    {
                        intervals.Add(new Interval<Block, ushort>((ushort)min, (ushort)max, current));
                    }
                    current = block;
                    min = i;
                    started = true;
                    --nodesRemoved;
                }
                max = i;
            }
            intervals.Add(new Interval<Block, ushort>((ushort)min, (ushort)max, current));
            compressionRatio = nodesRemoved / (float)m_hilbertCurve.Length;
            tree.AddRange(intervals);
            return tree;
        }

        private IntervalTree<Interval<Block, ushort>, ushort> ConvertArrayToIntervalTreeLinear(Block[] blocks, ScanDirection optimalDirection, out float compressionRatio)
        {
            var tree = new IntervalTree<Interval<Block, ushort>, ushort>(new IntervalSelector<Block, ushort>());
            var count = m_chunkSize * m_chunkSize * m_chunkSize;
            var nodesRemoved = count;
            var intervals = new List<Interval<Block, ushort>>();
            var min = 0;
            var max = 0;
            var current = Block.Empty;
            var started = false;
            var workCoords = new Vector3Int();
            for (var i = 0; i < count; ++i)
            {
                var block = blocks[GetNextIndex(optimalDirection, m_chunkSize, ref workCoords)];
                if (!(started && block == current))
                {
                    if (started)
                    {
                        intervals.Add(new Interval<Block, ushort>((ushort)min, (ushort)max, current));
                    }
                    current = block;
                    min = i;
                    started = true;
                    --nodesRemoved;
                }
                max = i;
            }
            intervals.Add(new Interval<Block, ushort>((ushort)min, (ushort)max, current));
            tree.AddRange(intervals);
            compressionRatio = nodesRemoved / (float)count;
            return tree;
        }

        private static int GetNextIndex(ScanDirection direction, int chunkSize, ref Vector3Int workCoords)
        {
            var x = workCoords.X;
            var y = workCoords.Y;
            var z = workCoords.Z;
            switch (direction)
            {
                case ScanDirection.Xyz:
                    workCoords.X = (x + 1)%chunkSize;
                    if (workCoords.X < x)
                    {
                        workCoords.Y = (y + 1)%chunkSize;
                        if (workCoords.Y < y)
                        {
                            workCoords.Z = (z + 1)%chunkSize;
                        }
                    }
                    break;
                case ScanDirection.Xzy:
                    workCoords.X = (x + 1)%chunkSize;
                    if (workCoords.X < x)
                    {
                        workCoords.Z = (z + 1)%chunkSize;
                        if (workCoords.Z < z)
                        {
                            workCoords.Y = (y + 1)%chunkSize;
                        }
                    }
                    break;
                case ScanDirection.Yxz:
                    workCoords.Y = (y + 1)%chunkSize;
                    if (workCoords.Y < y)
                    {
                        workCoords.X = (x + 1)%chunkSize;
                        if (workCoords.X < x)
                        {
                            workCoords.Z = (z + 1)%chunkSize;
                        }
                    }
                    break;
                case ScanDirection.Yzx:
                    workCoords.Y = (y + 1)%chunkSize;
                    if (workCoords.Y < y)
                    {
                        workCoords.Z = (z + 1) % chunkSize;
                        if (workCoords.Z < z)
                        {
                            workCoords.X = (x + 1) % chunkSize;
                        }
                    }
                    break;
                case ScanDirection.Zxy:
                    workCoords.Z = (z + 1) % chunkSize;
                    if (workCoords.Z < z)
                    {
                        workCoords.X = (x + 1) % chunkSize;
                        if (workCoords.X < x)
                        {
                            workCoords.Y = (y + 1) % chunkSize;
                        }
                    }
                    break;
                case ScanDirection.Zyx: 
                    workCoords.Z = (z + 1) % chunkSize;
                    if (workCoords.Z < z)
                    {
                        workCoords.Y = (y + 1) % chunkSize;
                        if (workCoords.Y < y)
                        {
                            workCoords.X = (x + 1) % chunkSize;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
            return workCoords.X*chunkSize*chunkSize + workCoords.Z*chunkSize + workCoords.Y;
        }

        private enum ScanDirection
        {
            Xyz,
            Xzy,
            Yxz,
            Yzx,
            Zxy,
            Zyx
        }

        private enum CompressionMode
        {
            Linear,
            Hilbert
        }

        public enum CompressionFlag
        {
            LinearXyz,
            LinearXzy,
            LinearYxz,
            LinearYzx,
            LinearZxy,
            LinearZyx,
            Hilbert,
            None
        }

        
    }
}
