using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Assets;
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
        private const int BlockBytes = sizeof(byte) * 4 + sizeof(ushort) * 2;

	    private readonly byte[] m_buffer;

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
            m_buffer = new byte[sizeof(int)* 3 + BlockBytes * count];
          /*  if (File.Exists(Path.Combine(m_dataDirectory, "SaveDirectory.json")))
		    {
		        var loader = new JsonLoader(m_dataDirectory);
                loader.Load<SaveDirectory>("SaveDirectory", "SaveDirectory");
		    }*/
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
                Debug.Assert(page.Data[i].Type == decompPage.Data[i].Type, "Type on block index: " + i);
                Debug.Assert(page.Data[i].LightRed == decompPage.Data[i].LightRed, "Red on block index: " + i);
                Debug.Assert(page.Data[i].LightGreen == decompPage.Data[i].LightGreen, "Green on block index: " + i);
                Debug.Assert(page.Data[i].LightBlue == decompPage.Data[i].LightBlue, "Blue on block index: " + i);
                Debug.Assert(page.Data[i].LightSun == decompPage.Data[i].LightSun, "Sun on block index: " + i);
                Debug.Assert(page.Data[i].Color == decompPage.Data[i].Color, "Color on block index: " + i);
	        }

	    }

        public void CompressPage(Page page)
        {
            var timer = Stopwatch.StartNew();
            
            PutInt(page.X, 0);
            PutInt(page.Y, 4);
            PutInt(page.Z, 8);
            const int headerSize = 12;
            var count = m_hilbertCurve.Length;
            for (var curveIndex = 0; curveIndex < count; ++curveIndex)
            {
                var block = page.Data[m_hilbertCurve[curveIndex]];
                var offset = headerSize + curveIndex * 2;
                PutShort(block.Type, offset);
                offset = headerSize + count * 2 + curveIndex;
                m_buffer[offset] = block.LightSun;
                offset = headerSize + count * 3 + curveIndex;
                m_buffer[offset] = block.LightRed;
                offset = headerSize + count * 4 + curveIndex;
                m_buffer[offset] = block.LightGreen;
                offset = headerSize + count * 5 + curveIndex;
                m_buffer[offset] = block.LightBlue;
                offset = headerSize + count * 6 + curveIndex * 2;
                PutShort(block.Color, offset);
            }
            using (var fileStream = new FileStream(Path.Combine(m_dataDirectory, page.PageId + ".page"), FileMode.Create))
            {
                using (var compressor = new GZipStream(fileStream, CompressionLevel.Optimal))
                {
                    compressor.Write(m_buffer, 0, m_buffer.Length);
                }
            }
            Console.WriteLine("save time: " + timer.ElapsedMilliseconds);
        }

	    private void PutInt(int value, int offset)
	    {
            m_buffer[offset + 3] = (byte)((value >> 24) & 255);
            m_buffer[offset + 2] = (byte)((value >> 16) & 255);
            m_buffer[offset + 1] = (byte)((value >> 8) & 255);
            m_buffer[offset] = (byte)(value & 255);
	    }

        private void PutShort(int value, int offset)
        {
            m_buffer[offset + 1] = (byte)((value >> 8) & 255);
            m_buffer[offset] = (byte)(value & 255);
        }
        
		public Page DecompressPage(int pageId)
        {
            var timer = Stopwatch.StartNew();
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
            Console.WriteLine("load time: " + timer.ElapsedMilliseconds);
            return page;
		}
	}
}