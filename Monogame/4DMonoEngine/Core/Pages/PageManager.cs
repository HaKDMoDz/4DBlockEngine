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
	    private readonly LruCache<Page> m_pageCache;
	    private readonly string m_dataDirectory;

		public PageManager()
		{
			//precompute the hilbert curve, since it will be the same for every page
			const uint count = Page.PageSizeInBlocks * Page.PageSizeInBlocks * Page.PageSizeInBlocks;
			var bitsPerAxis = (int)(Math.Ceiling(Math.Log (Page.PageSizeInBlocks, 2)));
			m_hilbertCurve = new int[count];
			for(uint index = 0; index < count; ++index)
			{
		 		var arr = HilbertCurve.HilbertAxes(index, 3, bitsPerAxis);
				var blockIndex = Page.BlockIndexFromRelativePosition((int)arr [0], (int)arr [1], (int)arr [2]);
				m_hilbertCurve [index] = blockIndex;
			}

            m_pageCache = new LruCache<Page>(25);
            var executableDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase);
            Debug.Assert(!String.IsNullOrEmpty(executableDir));
            m_dataDirectory = Path.Combine(executableDir, "Saves").Substring(6);
		}


	    public void TestCompression(Page page)
	    {
	        CompressPage(page);
            var decompPage = DecompressPage(page.PageId);
            Debug.Assert(page.X == decompPage.X);
            Debug.Assert(page.Y == decompPage.Y);
            Debug.Assert(page.Z == decompPage.Z);
	        for (var i = 0; i < page.Data.Length; ++i)
            {
                Debug.Assert(page.Data[i].Type == decompPage.Data[i].Type);
                Debug.Assert(page.Data[i].LightRed == decompPage.Data[i].LightRed);
                Debug.Assert(page.Data[i].LightGreen == decompPage.Data[i].LightGreen);
                Debug.Assert(page.Data[i].LightBlue == decompPage.Data[i].LightBlue);
                Debug.Assert(page.Data[i].LightSun == decompPage.Data[i].LightSun);
                Debug.Assert(page.Data[i].Color == decompPage.Data[i].Color);
	        }

	    }


		public void CompressPage(Page page)
		{

            var timer = Stopwatch.StartNew();
			using (var blockCompressStream = new MemoryStream())
			{
				using (var blockCompressWriter = new BinaryWriter(blockCompressStream))
                {
                    blockCompressWriter.Write(page.X);
                    blockCompressWriter.Write(page.Y);
                    blockCompressWriter.Write(page.Z);
					foreach (var blockIndex in m_hilbertCurve) 
					{
						var block = page.Data [blockIndex];
						blockCompressWriter.Write(block.Type);
					}
					foreach (var blockIndex in m_hilbertCurve) 
					{
						var block = page.Data [blockIndex];
						blockCompressWriter.Write (block.LightSun);
					}
					foreach (var blockIndex in m_hilbertCurve) 
					{
						var block = page.Data [blockIndex];
						blockCompressWriter.Write (block.LightRed);
					}
					foreach (var blockIndex in m_hilbertCurve) 
					{
						var block = page.Data [blockIndex];
						blockCompressWriter.Write (block.LightGreen);
					}
					foreach (var blockIndex in m_hilbertCurve) 
					{
						var block = page.Data [blockIndex];
						blockCompressWriter.Write (block.LightBlue);
					}
					foreach (var blockIndex in m_hilbertCurve) 
					{
						var block = page.Data [blockIndex];
						blockCompressWriter.Write (block.Color);
                    }
                    blockCompressStream.Position = 0;
                    using (var fileStream = new FileStream(Path.Combine(m_dataDirectory, page.PageId + ".page"), FileMode.Create))
                    {
                        using (var compressor = new GZipStream(fileStream, CompressionLevel.Optimal))
                        {
                            blockCompressStream.CopyTo(compressor);
                        }
                    }
				}
            }
            Console.WriteLine("save time: " + timer.ElapsedMilliseconds);
		}


		public Page DecompressPage(int pageId)
		{
			Page page;
            using (var fileStream = new FileStream(Path.Combine(m_dataDirectory, pageId + ".page"), FileMode.Open))
			{
				using(var decompressor = new GZipStream(fileStream, CompressionMode.Decompress))
				{
					using (var blockDecompressReader = new BinaryReader(decompressor))
                    {
                        var pageX = blockDecompressReader.ReadInt32();
                        var pageY = blockDecompressReader.ReadInt32();
                        var pageZ = blockDecompressReader.ReadInt32();
                        page = new Page(pageX, pageY, pageZ, pageId);
						foreach (var blockIndex in m_hilbertCurve) 
						{
							var block = new Block (blockDecompressReader.ReadUInt16 ());
							page.Data [blockIndex] = block;
						}
						foreach (var blockIndex in m_hilbertCurve) 
						{
							page.Data [blockIndex].LightSun = blockDecompressReader.ReadByte ();
						}
						foreach (var blockIndex in m_hilbertCurve) 
						{
							page.Data [blockIndex].LightRed = blockDecompressReader.ReadByte ();
						}
						foreach (var blockIndex in m_hilbertCurve) 
						{
							page.Data [blockIndex].LightGreen = blockDecompressReader.ReadByte ();
						}
						foreach (var blockIndex in m_hilbertCurve) 
						{
							page.Data [blockIndex].LightBlue = blockDecompressReader.ReadByte ();
						}
						foreach (var blockIndex in m_hilbertCurve) 
						{
							page.Data [blockIndex].Color = blockDecompressReader.ReadUInt16();
						}
					}
				}
			}
			return page;
		}
	}
}