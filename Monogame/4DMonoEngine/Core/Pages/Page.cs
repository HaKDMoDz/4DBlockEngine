using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Utils;

namespace _4DMonoEngine.Core.Pages
{
    public class Page
    {
		public const int PageSizeInBlocks = 64;
		public const int BlockStepX = PageSizeInBlocks * PageSizeInBlocks;
		public const int BlockStepZ = PageSizeInBlocks;
		public uint PageId;
    	public Block[] Data;

		public static int BlockIndexFromRelativePosition(int x, int y, int z)
		{
			var wrapX = MathUtilities.Modulo(x, PageSizeInBlocks);
			var wrapY = MathUtilities.Modulo(y, PageSizeInBlocks);
			var wrapZ = MathUtilities.Modulo(z, PageSizeInBlocks);
			var flattenIndex = wrapX * BlockStepX + wrapZ * BlockStepZ + wrapY;
			return flattenIndex;
		}

    }
}