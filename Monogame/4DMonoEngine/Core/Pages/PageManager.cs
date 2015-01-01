using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Assets;
using _4DMonoEngine.Core.Assets.DataObjects;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Generators;
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
        private const int BlockCount = Page.PageSizeInBlocks * Page.PageSizeInBlocks * Page.PageSizeInBlocks;
        private const int HeaderSize = 12;
        private const int BufferSize = HeaderSize + BlockBytes * BlockCount;
        public bool IsInitialized { get; private set; }
	    private SaveDirectory m_directory;
	    private readonly JsonWriter m_jsonWriter;
	    private readonly Stack<byte[]> m_buffers;
	    private readonly HashSet<string> m_pagesPendingWrite;

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
		    m_buffers = new Stack<byte[]>();
            m_buffers.Push(new byte[BufferSize]);
            m_pagesPendingWrite = new HashSet<string>();
            m_jsonWriter = new JsonWriter(m_dataDirectory);
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
                m_jsonWriter.Write("SaveDirectory", m_directory);
	        }
	        IsInitialized = true;
	    }

	    public void LoadOrCreateChunk(Vector3Int position, Block[] blocks, MappingFunction mappingFunction)
        {
            var pagePositionX = position.X / Page.PageSizeInBlocks;
            var pagePositionY = position.Y / Page.PageSizeInBlocks;
            var pagePositionZ = position.Z / Page.PageSizeInBlocks;
            var pageId = CreatePageId(pagePositionX, pagePositionY, pagePositionZ);
            if (m_pageCache.ContainsPage(pageId))
            {
                var page = m_pageCache.GetPage(pageId);
                CopyPageToChunk(page, position, blocks, mappingFunction);
            }
            else if (m_directory.Pages.Contains(pageId))
            {
                var page = DecompressPage(pageId);
                CopyPageToChunk(page, position, blocks, mappingFunction);
                m_pageCache.InsertPage(page);
            }
            else if (!m_pagesPendingWrite.Contains(pageId))
            {
                m_pagesPendingWrite.Add(pageId);
                var page = new Page(position.X, position.Y, position.Z, pageId);
                var worldPosX = pagePositionX * Page.PageSizeInBlocks;
                var worldPosY = pagePositionY * Page.PageSizeInBlocks;
                var worldPosZ = pagePositionZ * Page.PageSizeInBlocks;
                TerrainGenerator.Instance.GenerateDataForChunk(worldPosX, worldPosY, worldPosZ,
                                                               Page.PageSizeInBlocks, page.Data, (x, y, z) => Page.BlockIndexFromRelativePosition(
                                                                   x - worldPosX, y - worldPosY, z - worldPosZ));

                //BlockDataUtilities.SetupScanDirectedHilbertCurve(Page.PageSizeInBlocks);
               // ChunkCompressor.GetHilbertCurve(32);
                var timer = Stopwatch.StartNew();
                float compressionRatio;
                //ChunkCompressor.ScanDirection scanDir;
                var tree = ChunkCompressor.GetCompressor(Page.PageSizeInBlocks).ConvertArrayToIntervalTree(page.Data,
                    out compressionRatio);
                Console.WriteLine("tree creation time: " + timer.ElapsedMilliseconds);
              //  Console.WriteLine("h: " + scanDir);
              //  Console.WriteLine("tree compresion ratio: " + compressionRatio);
              //  timer.Restart();
            //    var tree2 = ChunkCompressor.ConvertArrayToIntervalTreeLinear(page.Data, Page.PageSizeInBlocks,
            //       out compressionRatio, out scanDir);
                //Console.WriteLine("linear tree creation time: " + timer.ElapsedMilliseconds);
               // Console.WriteLine("l: " + scanDir);
              //  Console.WriteLine("l: " + compressionRatio);

               // tree.

                timer.Restart();
                var res = tree[new Interval(17490)];
                Console.WriteLine("tree query time: " + timer.ElapsedMilliseconds);
                using (var fileStream = new FileStream(Path.Combine(m_dataDirectory, page.PageId + "_tree.page"), FileMode.Create))
                {
                    using (var compressor = new GZipStream(fileStream, CompressionLevel.Optimal))
                    {
                        //Serializer.Serialize(compressor, tree);
                    }
                }
                Console.WriteLine("tree compressionTime time: " + timer.ElapsedMilliseconds);

                timer.Restart();
                CompressPage(page);
                Console.WriteLine("normal compressionTime time: " + timer.ElapsedMilliseconds);

                m_pageCache.InsertPage(page);
                m_directory.Pages.Add(pageId);
                CopyPageToChunk(page, position, blocks, mappingFunction);
                //m_jsonWriter.Write("SaveDirectory", m_directory);
              /*  Task.Run(() =>
                {
                    CompressPage(page);
                    m_pagesPendingWrite.Remove(page.PageId);
                });*/
            }
	    }

        private static void CopyPageToChunk(Page page, Vector3Int position, Block[] blocks, MappingFunction mappingFunction)
        {
            var originX = MathUtilities.Modulo(position.X, Page.PageSizeInBlocks);
            var originY = MathUtilities.Modulo(position.Y, Page.PageSizeInBlocks);
            var originZ = MathUtilities.Modulo(position.Z, Page.PageSizeInBlocks);
            for (var x = originX; x < originX + Chunk.SizeInBlocks; x++)
            {
                for (var y = originY; y < originY + Chunk.SizeInBlocks; y++)
                {
                    for (var z = originZ; z < originZ + Chunk.SizeInBlocks; z++)
                    {
                        blocks[mappingFunction(position.X + x, position.Y + y, position.Z + z)] =
                            page.Data[Page.BlockIndexFromRelativePosition(x, y, z)];
                    }
                }
            }
        }


	    public void DebugFlushPageDirectory()
	    {
            m_jsonWriter.Write("SaveDirectory", m_directory);
	    }

	    public void SaveChunk(Vector3Int position, Block[] blocks, MappingFunction mappingFunction, Action<bool> callback)
        {
            var pagePositionX = position.X >> 6;
            var pagePositionY = position.Y >> 6;
            var pagePositionZ = position.Z >> 6;
	        var pageId = CreatePageId(pagePositionX, pagePositionY, pagePositionZ);
	        if (m_pagesPendingWrite.Contains(pageId))
	        {
                if (callback != null)
                {
                    callback(false);
                }
	        }
	        else
	        {
	            Page page;
	            if (m_pageCache.ContainsPage(pageId))
	            {
                    page = m_pageCache.GetPage(pageId);
                    CopyCunkToPage(page, position, blocks, mappingFunction);
	            }
                else if (m_directory.Pages.Contains(pageId))
                {
                    page = DecompressPage(pageId);
                    CopyCunkToPage(page, position, blocks, mappingFunction);
                    m_pageCache.InsertPage(page);
                }
	            else
	            {
                    page = new Page(position.X, position.Y, position.Z, pageId);
                    for (var x = 0; x < Page.PageSizeInBlocks; x++)
                    {
                        for (var y = 0; y < Page.PageSizeInBlocks; y++)
                        {
                            for (var z = 0; z < Page.PageSizeInBlocks; z++)
                            {
                                page.Data[Page.BlockIndexFromRelativePosition(x, y, z)] =
                                    blocks[mappingFunction(position.X + x, position.Y + y, position.Z + z)];
                            }
                        }
                    }
                    m_pageCache.InsertPage(page);
                    m_directory.Pages.Add(pageId);
                   // m_jsonWriter.Write("SaveDirectory", m_directory);
	            }
	            m_pagesPendingWrite.Add(pageId);
	            Task.Run(() =>
	            {
	                CompressPage(page);
	                if (callback != null)
	                {
	                    callback(true);
	                }
	                m_pagesPendingWrite.Remove(page.PageId);
	            });
	        }
	    }

        private static void CopyCunkToPage(Page page, Vector3Int position, Block[] blocks, MappingFunction mappingFunction)
        {
            var originX = MathUtilities.Modulo(position.X, Page.PageSizeInBlocks);
            var originY = MathUtilities.Modulo(position.Y, Page.PageSizeInBlocks);
            var originZ = MathUtilities.Modulo(position.Z, Page.PageSizeInBlocks);
            for (var x = originX; x < originX + Chunk.SizeInBlocks; x++)
            {
                for (var y = originY; y < originY + Chunk.SizeInBlocks; y++)
                {
                    for (var z = originZ; z < originZ + Chunk.SizeInBlocks; z++)
                    {
                        page.Data[Page.BlockIndexFromRelativePosition(x, y, z)] =
                            blocks[mappingFunction(position.X + x, position.Y + y, position.Z + z)];
                    }
                }
            }
	    }

        private static string CreatePageId(int x, int y, int z)
        {
            return string.Format("{0} {1} {2}", x, y, z);
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

	    private byte[] CheckOutBuffer()
	    {
	        lock (m_buffers)
	        {
	            return m_buffers.Count > 0 ? m_buffers.Pop() : new byte[BufferSize];
	        }
	    }

        private void CheckInBuffer(byte[] buffer)
        {
            m_buffers.Push(buffer);
        }

        public void CompressPage(Page page)
        {
            var buffer = CheckOutBuffer();
            PutInt(buffer, page.X, 0);
            PutInt(buffer, page.Y, 4);
            PutInt(buffer, page.Z, 8);
            for (var curveIndex = 0; curveIndex < BlockCount; ++curveIndex)
            {
                var block = page.Data[m_hilbertCurve[curveIndex]];
                var offset = HeaderSize + curveIndex * 2;
                PutShort(buffer, block.Type, offset);
                offset = HeaderSize + BlockCount * 2 + curveIndex;
                buffer[offset] = block.LightSun;
                offset = HeaderSize + BlockCount * 3 + curveIndex;
                buffer[offset] = block.LightRed;
                offset = HeaderSize + BlockCount * 4 + curveIndex;
                buffer[offset] = block.LightGreen;
                offset = HeaderSize + BlockCount * 5 + curveIndex;
                buffer[offset] = block.LightBlue;
                offset = HeaderSize + BlockCount * 6 + curveIndex * 2;
                PutShort(buffer, block.Color, offset);
            }
            using (var fileStream = new FileStream(Path.Combine(m_dataDirectory, page.PageId + ".page"), FileMode.Create))
            {
                using (var compressor = new GZipStream(fileStream, CompressionLevel.Optimal))
                {
                    compressor.Write(buffer, 0, BufferSize);
                }
            }
            CheckInBuffer(buffer);
        }

	    private static void PutInt(byte[] buffer, int value, int offset)
	    {
            buffer[offset + 3] = (byte)((value >> 24) & 255);
            buffer[offset + 2] = (byte)((value >> 16) & 255);
            buffer[offset + 1] = (byte)((value >> 8) & 255);
            buffer[offset] = (byte)(value & 255);
	    }

        private static void PutShort(byte[] buffer, int value, int offset)
        {
            buffer[offset + 1] = (byte)((value >> 8) & 255);
            buffer[offset] = (byte)(value & 255);
        }
        
		public Page DecompressPage(string pageId)
		{
		    var buffer = CheckOutBuffer();
			Page page;
            using (var fileStream = new FileStream(Path.Combine(m_dataDirectory, pageId + ".page"), FileMode.Open))
			{
				using(var decompressor = new GZipStream(fileStream, CompressionMode.Decompress))
				{
				    decompressor.Read(buffer, 0, BufferSize);
                    var pageX = GetInt(buffer, 0);
                    var pageY = GetInt(buffer, 4);
                    var pageZ = GetInt(buffer, 8);
                    page = new Page(pageX, pageY, pageZ, pageId);
                    for (var curveIndex = 0; curveIndex < BlockCount; ++curveIndex)
                    {
                        var offset = HeaderSize + curveIndex * 2;
                        var block = new Block(GetShort(buffer, offset));
                        offset = HeaderSize + BlockCount * 2 + curveIndex;
                        block.LightSun = buffer[offset];
                        offset = HeaderSize + BlockCount * 3 + curveIndex;
                        block.LightRed = buffer[offset];
                        offset = HeaderSize + BlockCount * 4 + curveIndex;
                        block.LightGreen = buffer[offset];
                        offset = HeaderSize + BlockCount * 5 + curveIndex;
                        block.LightBlue = buffer[offset];
                        offset = HeaderSize + BlockCount * 6 + curveIndex * 2;
                        block.Color = GetShort(buffer, offset);
                        page.Data[m_hilbertCurve[curveIndex]] = block;
                    }
				}
            }
		    CheckInBuffer(buffer);
            return page;
		}

        private static int GetInt(byte[] buffer, int offset)
        {
            var value = 0;
            value = value | ((buffer[offset + 3] & 255) << 24);
            value = value | ((buffer[offset + 2] & 255) << 16);
            value = value | ((buffer[offset + 1] & 255) << 8);
            value = value | (buffer[offset] & 255);
            return value;
        }

        private static ushort GetShort(byte[] buffer, int offset)
        {
            var value = 0;
            value = value | ((buffer[offset + 1] & 255) << 8);
            value = value | (buffer[offset] & 255); ;
            return (ushort)value;
        }


	}
}