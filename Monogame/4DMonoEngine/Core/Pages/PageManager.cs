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
        public static PageManager Instance
        {
            get { return s_instance ?? (s_instance = new PageManager()); }
        }
        private static PageManager s_instance;

		private readonly int[] m_hilbertCurve;
	    private readonly LruCache<Page> m_pageCache;
        private readonly string m_dataDirectory;
        private const int BlockBytes = sizeof(byte) * 4 + sizeof(ushort) * 2;
        const int BlockCount = Page.PageSizeInBlocks * Page.PageSizeInBlocks * Page.PageSizeInBlocks;
        const int HeaderSize = 12;
        public bool IsInitialized { get; private set; }
	    private SaveDirectory m_directory;

	    private readonly byte[] m_buffer;

		private PageManager()
		{
			//precompute the hilbert curve, since it will be the same for every page
			var bitsPerAxis = (int)(Math.Ceiling(Math.Log (Page.PageSizeInBlocks, 2)));
			m_hilbertCurve = new int[BlockCount];
			for(uint index = 0; index < BlockCount; ++index)
			{
		 		var arr = HilbertCurve.HilbertAxes(index, 3, bitsPerAxis);
				var blockIndex = Page.BlockIndexFromRelativePosition((int)arr [0], (int)arr [1], (int)arr [2]);
				m_hilbertCurve [index] = blockIndex;
			}

            m_pageCache = new LruCache<Page>(25);
            var executableDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase);
            Debug.Assert(!String.IsNullOrEmpty(executableDir));
            m_dataDirectory = Path.Combine(executableDir, "Saves").Substring(6);
            m_buffer = new byte[sizeof(int)* 3 + BlockBytes * BlockCount];
		}

	    public async void Initialize()
	    {
	        if (File.Exists(Path.Combine(m_dataDirectory, "SaveDirectory.json")))
	        {
	            var loader = new JsonLoader(m_dataDirectory);
                m_directory = await loader.Load<SaveDirectory>("SaveDirectory", "SaveDirectory");
	        }
	        else
	        {
                m_directory = new SaveDirectory();
	        }
	        IsInitialized = true;
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
            for (var curveIndex = 0; curveIndex < BlockCount; ++curveIndex)
            {
                var block = page.Data[m_hilbertCurve[curveIndex]];
                var offset = HeaderSize + curveIndex * 2;
                PutShort(block.Type, offset);
                offset = HeaderSize + BlockCount * 2 + curveIndex;
                m_buffer[offset] = block.LightSun;
                offset = HeaderSize + BlockCount * 3 + curveIndex;
                m_buffer[offset] = block.LightRed;
                offset = HeaderSize + BlockCount * 4 + curveIndex;
                m_buffer[offset] = block.LightGreen;
                offset = HeaderSize + BlockCount * 5 + curveIndex;
                m_buffer[offset] = block.LightBlue;
                offset = HeaderSize + BlockCount * 6 + curveIndex * 2;
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
				    decompressor.Read(m_buffer, 0, m_buffer.Length);
                    var pageX = GetInt(0);
                    var pageY = GetInt(4);
                    var pageZ = GetInt(8);
                    page = new Page(pageX, pageY, pageZ, pageId);
                    for (var curveIndex = 0; curveIndex < BlockCount; ++curveIndex)
                    {
                        var offset = HeaderSize + curveIndex * 2;
                        var block = new Block(GetShort(offset));
                        offset = HeaderSize + BlockCount * 2 + curveIndex;
                        block.LightSun = m_buffer[offset];
                        offset = HeaderSize + BlockCount * 3 + curveIndex;
                        block.LightRed = m_buffer[offset];
                        offset = HeaderSize + BlockCount * 4 + curveIndex;
                        block.LightGreen = m_buffer[offset];
                        offset = HeaderSize + BlockCount * 5 + curveIndex;
                        block.LightBlue = m_buffer[offset];
                        offset = HeaderSize + BlockCount * 6 + curveIndex * 2;
                        block.Color = GetShort(offset);
                        page.Data[m_hilbertCurve[curveIndex]] = block;
                    }
				}
            }
            Console.WriteLine("load time: " + timer.ElapsedMilliseconds);
            return page;
		}

        private int GetInt(int offset)
        {
            var value = 0;
            value = value | ((m_buffer[offset + 3] & 255) << 24);
            value = value | ((m_buffer[offset + 2] & 255) << 16);
            value = value | ((m_buffer[offset + 1] & 255) << 8);
            value = value | (m_buffer[offset] & 255);
            return value;
        }

        private ushort GetShort(int offset)
        {
            var value = 0;
            value = value | ((m_buffer[offset + 1] & 255) << 8);
            value = value | (m_buffer[offset] & 255); ;
            return (ushort)value;
        }


	}
}