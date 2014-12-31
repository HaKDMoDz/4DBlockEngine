using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using _4DMonoEngine.Core.Blocks;

namespace _4DMonoEngine.Core.Utils
{
    [ProtoContract]
    public class BlockIntervalTree
    {
        [ProtoMember(1)]
        private readonly BlockInterval[] m_backingList;

        public BlockIntervalTree(List<BlockInterval> intervals)
        {
            m_backingList = new BlockInterval[intervals.Count];
            Construct(intervals, 0, intervals.Count, 0);
        }

        private void Construct(List<BlockInterval> intervals, int start, int end, int nodeIndex)
        {
            while (true)
            {
                if (start > end)
                {
                    return;
                }
                var mid = (start + end)/2;
                m_backingList[nodeIndex] = intervals[mid];
                Construct(intervals, start, mid - 1, 2*nodeIndex + 1);
                start = mid + 1;
                nodeIndex = 2*nodeIndex + 2;
            }
        }


        public Block GetBlockAt(ushort index)
        {
            var nodeIndex = 0;
            while (nodeIndex < m_backingList.Length)
            {
                var node = m_backingList[nodeIndex];
                if (index < node.Min)
                {
                    nodeIndex = 2 * nodeIndex + 1;
                }
                else if (index > node.Max)
                {
                    nodeIndex = 2 * nodeIndex + 2;
                }
                else
                {
                    return node.Block;
                }
            }
            return Block.Empty;
        }

        public struct BlockInterval
        {
            [ProtoMember(1)]
            public ushort Min;
            [ProtoMember(2)]
            public ushort Max;
            [ProtoMember(3)]
            public Block Block;

            public BlockInterval(ushort min, ushort max, Block block)
            {
                Min = min;
                Max = max;
                Block = block;
            }
        }
    }
}
