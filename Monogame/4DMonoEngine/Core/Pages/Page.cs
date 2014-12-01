using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Interfaces;

namespace _4DMonoEngine.Core.Pages
{
    public class Page : IPageable
    {
		public const int PageSizeInBlocks = 64;
		public const int BlockStepX = PageSizeInBlocks * PageSizeInBlocks;
		public const int BlockStepZ = PageSizeInBlocks;
        public string PageId { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }
        public Block[] Data { get; private set; }

        public Page(int x, int y, int z, string pageId)
            : this(x, y, z, pageId, new Block[PageSizeInBlocks * PageSizeInBlocks * PageSizeInBlocks])
        {}

        public Page(int x, int y, int z, string pageId, Block[] data)
        {
            X = x;
            Y = y;
            Z = z;
            PageId = pageId;
            Data = data;
        }

		public static int BlockIndexFromRelativePosition(int x, int y, int z)
		{
            if (x < 0 || x >= PageSizeInBlocks) return -1;
            if (y < 0 || y >= PageSizeInBlocks) return -1;
            if (z < 0 || z >= PageSizeInBlocks) return -1;
			return x * BlockStepX + y * BlockStepZ + z;
		}

    }
}