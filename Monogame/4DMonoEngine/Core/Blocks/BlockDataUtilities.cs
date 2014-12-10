using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Blocks
{
    public static class BlockDataUtilities
    {
        private static readonly Dictionary<int, long[]> HilbertCurves = new Dictionary<int, long[]>(); 

        private static readonly Dictionary<ScanDirection, int[]> ScanDirectedHilbertCurves = new Dictionary<ScanDirection, int[]>();

        public static IntervalTree<Interval<Block>, int> ConvertArrayToIntervalTreeHilbert(Block[] blocks, int chunkSize, out float compressionRatio)
        {
            var tree = new IntervalTree<Interval<Block>, int>(new IntervalSelector<Block>());
            var hilbertCurve = GetHilbertCurve(chunkSize);
            var nodesRemoved = hilbertCurve.Length;
            var min = 0;
            var max = 0;
            var current = Block.Empty;
            var started = false;
            var intervals = new List<Interval<Block>>();
            for(var i = 0 ; i < hilbertCurve.Length; ++i)
            {
                var index = hilbertCurve[i];
                var block = blocks[index];
                if (!(started && block == current))
                {
                    if (started)
                    {
                        intervals.Add(new Interval<Block>(min, max, current));
                    }
                    current = block;
                    min = i;
                    started = true;
                    --nodesRemoved;
                }
                max = i;
            }
            intervals.Add(new Interval<Block>(min, max, current));
            compressionRatio = nodesRemoved / (float)hilbertCurve.Length;
            tree.AddRange(intervals);
            return tree;
        }

        internal static object ConvertArrayToIntervalTreeLinear(Block[] blocks, int chunkSize, out float compressionRatio, out ScanDirection optimalDirection)
        {
            var tree = new IntervalTree<Interval<Block>, int>(new IntervalSelector<Block>());
            var count = chunkSize * chunkSize * chunkSize;
            Vector3Int workCoords;
            Block current;
            var intervals = new List<Interval<Block>>();
            compressionRatio = 0;
            optimalDirection = ScanDirection.Xyz;
            foreach (ScanDirection scanDirection in Enum.GetValues(typeof (ScanDirection)))
            {
                var nodesRemoved = count;
                current = Block.Empty;
                workCoords = new Vector3Int();
                for (var i = 0; i < count; ++i)
                {
                    var block = blocks[GetNextIndex(scanDirection, chunkSize, ref workCoords)];
                    if (block == current)
                    {
                        continue;
                    }
                    current = block;
                    --nodesRemoved;
                }
                var currentRatio = nodesRemoved/(float) count;
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
            var min = 0;
            var max = 0;
            current = Block.Empty;
            var started = false;
            workCoords = new Vector3Int();
            for (var i = 0; i < count; ++i)
            {
                var block = blocks[GetNextIndex(optimalDirection, chunkSize, ref workCoords)];
                if (!(started && block == current))
                {
                    if (started)
                    {
                        intervals.Add(new Interval<Block>(min, max, current));
                    }
                    current = block;
                    min = i;
                    started = true;
                }
                max = i;
            }
            intervals.Add(new Interval<Block>(min, max, current));
            tree.AddRange(intervals);
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

        public enum ScanDirection
        {
            Xyz,
            Xzy,
            Yxz,
            Yzx,
            Zxy,
            Zyx
        }

        public static long[] GetHilbertCurve(int chunkSize)
        {
            if (!HilbertCurves.ContainsKey(chunkSize))
            {
                SetupHilbertCurve(chunkSize);
            }
            return HilbertCurves[chunkSize];
        }

        public static void SetupHilbertCurve(int chunkSize)
        {
            var blockStepX = chunkSize * chunkSize;
            var blockCount = chunkSize * chunkSize * chunkSize;
            var bitsPerAxis = (int)(Math.Ceiling(Math.Log(chunkSize, 2)));
            var hilbertCurve = new long[blockCount];
            for (uint index = 0; index < blockCount; ++index)
            {
                var arr = HilbertCurve.HilbertAxes(index, 3, bitsPerAxis);
                var blockIndex = arr[0] * blockStepX + arr[2] * chunkSize + arr[1];
                hilbertCurve[index] = blockIndex;
            }

            HilbertCurves[chunkSize] = hilbertCurve;
        }
    }
}
