using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Vector;
using System.IO;
using System.IO.Compression;
using _4DMonoEngine.Core.Blocks;

namespace _4DMonoEngine.Core.Pages
{
	public class PageManager
	{
		private readonly int[] m_hilbertCurve;

		public PageManager()
		{
			//precompute the hilbert curve, since it will be the same for every page
			uint count = Page.PageSizeInBlocks * Page.PageSizeInBlocks * Page.PageSizeInBlocks;
			int bitsPerAxis = (int)(Math.Ceiling(Math.Log (Page.PageSizeInBlocks, 2)));
			m_hilbertCurve = new int[count];
			for(uint index = 0; index < count; ++index)
			{
		 		var arr = HilbertCurve.HilbertAxes(index, 3, bitsPerAxis);
				var blockIndex = Page.BlockIndexFromRelativePosition((int)arr [0], (int)arr [1], (int)arr [2]);
				m_hilbertCurve [index] = blockIndex;
			}
		}

		public void CompressPage(Page page)
		{
			using (var blockCompressStream = new MemoryStream())
			{
				using (var blockCompressWriter = new BinaryWriter(blockCompressStream))
				{
					foreach (int blockIndex in m_hilbertCurve) 
					{
						var block = page.Data [blockIndex];
						blockCompressWriter.Write(block.Type);
					}
					foreach (int blockIndex in m_hilbertCurve) 
					{
						var block = page.Data [blockIndex];
						blockCompressWriter.Write (block.LightSun);
					}
					foreach (int blockIndex in m_hilbertCurve) 
					{
						var block = page.Data [blockIndex];
						blockCompressWriter.Write (block.LightRed);
					}
					foreach (int blockIndex in m_hilbertCurve) 
					{
						var block = page.Data [blockIndex];
						blockCompressWriter.Write (block.LightGreen);
					}
					foreach (int blockIndex in m_hilbertCurve) 
					{
						var block = page.Data [blockIndex];
						blockCompressWriter.Write (block.LightBlue);
					}
					foreach (int blockIndex in m_hilbertCurve) 
					{
						var block = page.Data [blockIndex];
						blockCompressWriter.Write (block.Color);
					}

				}
				using(var fileStream = new FileStream("Saves/Page" + page.PageId, FileMode.Create))
				{
					using (var compressor = new GZipStream(fileStream, CompressionLevel.Optimal)) 
					{
						blockCompressStream.CopyTo (compressor);
					}
				}
			}
		}


		public Page DecompressPage(int pageId)
		{
			Page page = null;
			using (var fileStream = new FileStream("Saves/Page" + pageId, FileMode.Open))
			{
				using(var decompressor = new GZipStream(fileStream, CompressionMode.Decompress))
				{
					using (var blockDecompressReader = new BinaryReader(decompressor))
					{
						page = new Page ();
						foreach (int blockIndex in m_hilbertCurve) 
						{
							Block block = new Block (blockDecompressReader.ReadUInt16 ());
							page.Data [blockIndex] = block;
						}
						foreach (int blockIndex in m_hilbertCurve) 
						{
							var block = page.Data [blockIndex];
							block.LightSun = blockDecompressReader.ReadByte ();
						}
						foreach (int blockIndex in m_hilbertCurve) 
						{
							var block = page.Data [blockIndex];
							block.LightRed = blockDecompressReader.ReadByte ();
						}
						foreach (int blockIndex in m_hilbertCurve) 
						{
							var block = page.Data [blockIndex];
							block.LightGreen = blockDecompressReader.ReadByte ();
						}
						foreach (int blockIndex in m_hilbertCurve) 
						{
							var block = page.Data [blockIndex];
							block.LightBlue = blockDecompressReader.ReadByte ();
						}
						foreach (int blockIndex in m_hilbertCurve) 
						{
							var block = page.Data [blockIndex];
							block.Color = blockDecompressReader.ReadUInt16();
						}
					}
				}
			}
			return page;
		}
	}
}