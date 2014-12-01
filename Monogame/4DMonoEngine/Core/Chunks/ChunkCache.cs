using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Chunks.Generators;
using _4DMonoEngine.Core.Common.AbstractClasses;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Events;
using _4DMonoEngine.Core.Events.Args;
using _4DMonoEngine.Core.Graphics;
using _4DMonoEngine.Core.Logging;
using _4DMonoEngine.Core.Pages;
using _4DMonoEngine.Core.Processors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Chunks
{

    public class ChunkCache : WorldRenderable, IEventSink
    {
        private enum StartUpState
        {
            NotStarted,
            AwaitingStart,
            Starting,
            Started,
        }

        private const byte CacheRange = 6;
        private BoundingBox m_cacheRangeBoundingBox;
        /*TODO : Implement LOD here
         * 
         * The level of detail zones should be smaller than ViewRange.
         * This means that we will have to make the cache much larger.
         * Some form of paging scheme may need to be devised to avoid 
         * issues with the Large Object Heap
         * 
         */
        public const byte ViewRange = CacheRange - 2;
        private BoundingBox m_viewRangeBoundingBox;

        private const int CacheSizeInBlocks = (CacheRange * 2 + 1) * Chunk.SizeInBlocks;
        public const int BlockStepX = CacheSizeInBlocks * CacheSizeInBlocks;
        public const int BlockStepZ = CacheSizeInBlocks;
#if DEBUG
        public int ChunksDrawn;
        public int ChunksLoaded { get { return m_chunkStorage.Count; } }
        public Dictionary<ChunkState, int> StateStatistics { get; private set; }
#endif

        // assets & resources
        private Effect m_blockEffect; // block effect.
        private Texture2D m_blockTextureAtlas; // block texture atlas

        private readonly CellularLighting<Block> m_lightingEngine;
        private readonly VertexBuilder<Block> m_vertexBuilder;
        private readonly SparseArray3D<Chunk> m_chunkStorage;
        private Vector4 m_cacheCenterPosition;
		private readonly EventSinkImpl m_eventSinkImpl;

        private bool m_cachePositionUpdated;

        public Vector4 CachePosition { get { return m_cacheCenterPosition; } }

        public Block[] Blocks { get; private set; }
        private readonly Logger m_logger;
        private StartUpState m_startUpState;

        private readonly Queue<Chunk> m_processingQueue;
        private readonly ConcurrentQueue<WorldEdit> m_EditQueue; 

        public ChunkCache(Game game) : base(game)
        {
            m_logger = MainEngine.GetEngineInstance().GetLogger("ChunkCache");
            Debug.Assert(game != null);
            var graphicsDevice = game.GraphicsDevice;
            Debug.Assert(ViewRange < CacheRange);
            Debug.Assert(graphicsDevice != null);
            Blocks = new Block[CacheSizeInBlocks * CacheSizeInBlocks * CacheSizeInBlocks];
            m_lightingEngine = new CellularLighting<Block>(Blocks, BlockIndexByWorldPosition, Chunk.SizeInBlocks, GetChunkByWorldPosition);
            m_vertexBuilder = new VertexBuilder<Block>(Blocks, BlockIndexByWorldPosition, graphicsDevice);
            m_chunkStorage = new SparseArray3D<Chunk>(CacheRange * 2 + 1, CacheRange * 2 + 1);
            m_cacheCenterPosition = new Vector4();
			m_eventSinkImpl = new EventSinkImpl ();
            m_eventSinkImpl.AddHandler<Vector3Args>(EventConstants.PlayerPositionUpdated, OnUpdateCachePosition);
            m_startUpState = StartUpState.NotStarted;
            m_processingQueue = new Queue<Chunk>();
            m_EditQueue = new ConcurrentQueue<WorldEdit>();
#if DEBUG
            StateStatistics = new Dictionary<ChunkState, int> // init. the debug stastics.
                                       {
                                           {ChunkState.AwaitingGenerate, 0},
                                           {ChunkState.Generating, 0},
                                           {ChunkState.AwaitingLighting, 0},
                                           {ChunkState.Lighting, 0},
                                           {ChunkState.AwaitingBuild, 0},
                                           {ChunkState.Building, 0},
                                           {ChunkState.Ready, 0},
                                           {ChunkState.AwaitingRemoval, 0},
                                       };
#endif
        }

        public override void Initialize(GraphicsDevice graphicsDevice, Camera camera, GetTimeOfDay getTimeOfDay, GetFogVector getFogVector)
        {
            base.Initialize(graphicsDevice, camera, getTimeOfDay, getFogVector);
            MainEngine.GetEngineInstance().CentralDispatch.Register(EventConstants.PlayerPositionUpdated, GetHandlerForEvent(EventConstants.PlayerPositionUpdated));
        }

        public override void LoadContent()
        {
            m_blockEffect = MainEngine.GetEngineInstance().GetAsset<Effect>("BlockEffect");
            m_blockTextureAtlas = MainEngine.GetEngineInstance().GetAsset<Texture2D>("BlockTextureAtlas");
        }

        public void AddBlock(int x, int y, int z, ref Block block)
        {
            var chunk = GetChunkByWorldPosition(x, y, z); // get the chunk that block is hosted in.
            if (chunk == null)
            {
                return;
            }
            var flattenIndex = BlockIndexByWorldPosition(x, y, z);
            Blocks[flattenIndex] = block;
            chunk.AddBlock(x, y, z);
            m_lightingEngine.AddBlock(x, y, z, block);
        }

        public void RemoveBlock(int x, int y, int z)
        {
            var chunk = GetChunkByWorldPosition(x, y, z); // get the chunk that block is hosted in.
            if (chunk == null)
            {
                return;
            }
            var flattenIndex = BlockIndexByWorldPosition(x, y, z);
            Blocks[flattenIndex] = Block.Empty;
            chunk.RemoveBlock(x, y, z);
            m_lightingEngine.RemoveBlock(x, y, x);
        }

        private void OnUpdateCachePosition(Vector3Args args)
        {
            OnUpdateCachePosition((int)args.Vector.X, (int)args.Vector.Y, (int)args.Vector.Z);
        }

        public void OnUpdateCachePosition(int x, int y, int z)
        {
            if (x == (int) m_cacheCenterPosition.X && y == (int) m_cacheCenterPosition.Y &&
                z == (int) m_cacheCenterPosition.Z)
            {
                return;
            }
            m_cachePositionUpdated = true;
            m_cacheCenterPosition.X = x;
            m_cacheCenterPosition.Y = y;
            m_cacheCenterPosition.Z = z;
            UpdateBoundingBoxes();
            if (m_startUpState == StartUpState.NotStarted)
            {
                m_startUpState = StartUpState.AwaitingStart;
            }
        }

        private bool add = true;
        private int count = 0;
        public override void Update(GameTime gameTime)
        {
            if (m_startUpState == StartUpState.Started)
            {
                /*if (count++ > 100 && m_EditQueue.Count == 0)
                {
                    count = 0;
                    if (add)
                    {
                        m_EditQueue.Enqueue(new WorldEdit(new Vector3Int(-25, 68, 10), WorldEdit.WorldEditType.AddLight, new Vector3Byte(255, 255, 255)));
                    }
                    else
                    {
                        m_EditQueue.Enqueue(new WorldEdit(new Vector3Int(-25, 68, 10), WorldEdit.WorldEditType.RemoveLight));
                    }
                    add = !add;
                    m_cachePositionUpdated = true;
                }*/
            }
            if (m_startUpState != StartUpState.AwaitingStart)
            {
                return;
            }
            m_startUpState = StartUpState.Starting;
            Task.Run(() =>
            {
                RecacheChunks();
                Parallel.ForEach(m_chunkStorage.Values.ToList(), ProcessChunkInCacheRange);
                var cacheThread = new Thread(CacheThread) { IsBackground = true };
                cacheThread.Start();
                m_startUpState = StartUpState.Started;   
            });
        }

        public bool IsInViewRange(Chunk chunk)
        {
            return m_viewRangeBoundingBox.Contains(chunk.BoundingBox) != ContainmentType.Disjoint;
        }

        public bool IsInViewRange(int x, int y, int z)
        {
            return !(x < m_viewRangeBoundingBox.Min.X || z < m_viewRangeBoundingBox.Min.Z || x >= m_viewRangeBoundingBox.Max.X ||
                     z >= m_viewRangeBoundingBox.Max.Z || y < m_viewRangeBoundingBox.Min.Y || y >= m_viewRangeBoundingBox.Max.Y);
        }

        public bool IsInCacheRange(Chunk chunk)
        {
            return m_cacheRangeBoundingBox.Contains(chunk.BoundingBox) != ContainmentType.Disjoint;
        }

        public bool IsInCacheRange(int x, int y, int z)
        {
            return !(x < m_cacheRangeBoundingBox.Min.X || y < m_cacheRangeBoundingBox.Min.Y || z < m_cacheRangeBoundingBox.Min.Z ||
                     x > m_cacheRangeBoundingBox.Max.X || y > m_cacheRangeBoundingBox.Max.Y || z > m_cacheRangeBoundingBox.Max.Z);
        }

        private void UpdateBoundingBoxes()
        {
            m_viewRangeBoundingBox = new BoundingBox(
                        new Vector3(m_cacheCenterPosition.X - (ViewRange * Chunk.SizeInBlocks), m_cacheCenterPosition.Y - (ViewRange * Chunk.SizeInBlocks),
                                    m_cacheCenterPosition.Z - (ViewRange * Chunk.SizeInBlocks)),
                        new Vector3(m_cacheCenterPosition.X + (ViewRange * Chunk.SizeInBlocks), m_cacheCenterPosition.Y + (ViewRange * Chunk.SizeInBlocks),
                                    m_cacheCenterPosition.Z + (ViewRange * Chunk.SizeInBlocks)));
                
            m_cacheRangeBoundingBox = new BoundingBox(
                        new Vector3(m_cacheCenterPosition.X - (CacheRange * Chunk.SizeInBlocks), m_cacheCenterPosition.Y - (CacheRange * Chunk.SizeInBlocks),
                                    m_cacheCenterPosition.Z - (CacheRange * Chunk.SizeInBlocks)),
                        new Vector3(m_cacheCenterPosition.X + (CacheRange * Chunk.SizeInBlocks), m_cacheCenterPosition.Y + (CacheRange * Chunk.SizeInBlocks),
                                    m_cacheCenterPosition.Z + (CacheRange * Chunk.SizeInBlocks)));
        }

        private void CacheThread()
        {
            while (true)
            {
                Process();
                Thread.Sleep(10);
            }
// ReSharper disable once FunctionNeverReturns
        }

        private void Process()
        {
            while (!m_cachePositionUpdated && m_processingQueue.Count > 0)
            {
                var chunk = m_processingQueue.Dequeue();
                if (IsInViewRange(chunk))
                {
                    ProcessChunkInViewRange(chunk);
                }
                else if (IsInCacheRange(chunk))
                {
                    ProcessChunkInCacheRange(chunk);
                }
            }
            WorldEdit result;
            while (m_EditQueue.TryDequeue(out result))
            {
                var chunk = GetChunkByWorldPosition(result.Position);
                if (chunk == null)
                {
                    continue;
                }
                switch (result.EditType)
                {
                    case WorldEdit.WorldEditType.RemoveBlock:
                        Console.WriteLine("clearing block");
                        break;
                    case WorldEdit.WorldEditType.AddBlock:
                        Console.WriteLine("adding block");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            m_cachePositionUpdated = false;
            RecacheChunks();
        }

        private void RecacheChunks()
        {
            foreach (var chunk in m_chunkStorage.Values.Where(chunk => !IsInCacheRange(chunk)))
            {
                m_chunkStorage.Remove(chunk.ChunkCachePosition.X, chunk.ChunkCachePosition.Y, chunk.ChunkCachePosition.Z);
                chunk.PrepForRemoval();
            }
            m_processingQueue.Clear();
            var toAdd = new List<Chunk>();
            var chunkPosition = new Vector3Int((int)(m_cacheCenterPosition.X / Chunk.SizeInBlocks), (int)(m_cacheCenterPosition.Y / Chunk.SizeInBlocks), ((int)m_cacheCenterPosition.Z / Chunk.SizeInBlocks));
            for (var z = -CacheRange; z <= CacheRange; z++)
            {
                for (var y = -CacheRange; y <= CacheRange; y++)
                {
                    for (var x = -CacheRange; x <= CacheRange; x++)
                    {
                        Chunk chunk;
                        if (!m_chunkStorage.ContainsKey(chunkPosition.X + x,
                            chunkPosition.Y + y, chunkPosition.Z + z))
                        {
                            chunk =
                                new Chunk(
                                    new Vector3Int(chunkPosition.X + x, chunkPosition.Y + y, chunkPosition.Z + z),
                                    Blocks, BlockIndexByWorldPosition, GetNeighborChunk);
                            m_chunkStorage[
                                chunk.ChunkCachePosition.X, chunk.ChunkCachePosition.Y, chunk.ChunkCachePosition.Z] =
                                chunk;
                        }
                        else
                        {
                            chunk = m_chunkStorage[chunkPosition.X + x,
                                chunkPosition.Y + y, chunkPosition.Z + z];
                        }
                        if ((IsInViewRange(chunk) && chunk.ChunkState == ChunkState.Ready)
                            || (!IsInViewRange(chunk) && chunk.ChunkState == ChunkState.AwaitingLighting))
                        {
                            continue;
                        }
                        toAdd.Add(chunk);
                    }
                }
            }
            var center = new Vector3(m_cacheCenterPosition.X - Chunk.SizeInBlocks / 2, m_cacheCenterPosition.Y - Chunk.SizeInBlocks / 2, m_cacheCenterPosition.Z - Chunk.SizeInBlocks / 2);
            toAdd.Sort((chunk1, chunk2) =>
            {
                var ret = (int) (chunk1.Position.DistanceSquared(ref center) - chunk2.Position.DistanceSquared(ref center));
                if (ret == 0)
                {
                    return (chunk2.ChunkState - chunk1.ChunkState);
                }
                return ret;
            });
            foreach (var chunk in toAdd)
            {
                m_processingQueue.Enqueue(chunk);
            }
        }

        private void ProcessChunkInCacheRange(Chunk chunk)
        {
            if (chunk.ChunkState != ChunkState.AwaitingGenerate)
            {
                return;
            }
            chunk.ChunkState = ChunkState.Generating;
            TerrainGenerator.Instance.GenerateDataForChunk(chunk.Position.X, chunk.Position.Y, chunk.Position.Z, Chunk.SizeInBlocks, Blocks, BlockIndexByWorldPosition);
            chunk.UpdateBoundingBox();
            chunk.ChunkState = ChunkState.AwaitingLighting;
        }

        private void ProcessChunkInViewRange(Chunk chunk)
        {
            switch (chunk.ChunkState) 
            {
                case ChunkState.AwaitingGenerate:
                    chunk.ChunkState = ChunkState.Generating;
                    TerrainGenerator.Instance.GenerateDataForChunk(chunk.Position.X, chunk.Position.Y, chunk.Position.Z, Chunk.SizeInBlocks, Blocks, BlockIndexByWorldPosition);
                    chunk.UpdateBoundingBox();
                    chunk.ChunkState = ChunkState.AwaitingLighting;
                    goto case ChunkState.AwaitingLighting;
                case ChunkState.AwaitingLighting:
                    chunk.ChunkState = ChunkState.Lighting;
                    m_lightingEngine.Process(chunk.Position.X, chunk.Position.Y, chunk.Position.Z);
                    chunk.ChunkState = ChunkState.AwaitingBuild;
                    goto case ChunkState.AwaitingBuild;
                case ChunkState.AwaitingBuild:
                    chunk.ChunkState = ChunkState.Building; 
                    m_vertexBuilder.Build(chunk);
                    chunk.ChunkState = ChunkState.Ready; 
                    break;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            var viewFrustrum = new BoundingFrustum(m_camera.View*m_camera.Projection);

            Game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Game.GraphicsDevice.BlendState = BlendState.Opaque;
            Game.GraphicsDevice.RasterizerState = MainEngine.GetEngineInstance().Rasterizer.State;

            // general parameters
            m_blockEffect.Parameters["World"].SetValue(Matrix.Identity);
            m_blockEffect.Parameters["View"].SetValue(m_camera.View);
            m_blockEffect.Parameters["Projection"].SetValue(m_camera.Projection);
            m_blockEffect.Parameters["CameraPosition"].SetValue(m_camera.Ray.Position);

            // texture parameters
            m_blockEffect.Parameters["BlockTextureAtlas"].SetValue(m_blockTextureAtlas);

            // atmospheric settings
            m_blockEffect.Parameters["SunColor"].SetValue(GetSunColor());
            m_blockEffect.Parameters["HorizonColor"].SetValue(GetHorizonColor());
            // fog parameters
            m_blockEffect.Parameters["FogNear"].SetValue(m_getFogVector().X);
            m_blockEffect.Parameters["FogFar"].SetValue(m_getFogVector().Y);

#if DEBUG
            ChunksDrawn = 0;
#endif
            foreach (var pass in m_blockEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                foreach (var chunk in m_chunkStorage.Values)
                {
                    if (chunk.IndexBuffer == null || chunk.VertexBuffer == null)
                    {
                        continue;
                    }

                    if (chunk.VertexBuffer.VertexCount == 0)
                    {
                        continue;
                    }

                    if (chunk.IndexBuffer.IndexCount == 0)
                    {
                        continue;
                    }

                    if (!IsInViewRange(chunk))
                    {
                        continue;
                    }

                    if (!chunk.BoundingBox.Intersects(viewFrustrum))
                    {
                        continue;
                    }

                    Game.GraphicsDevice.SetVertexBuffer(chunk.VertexBuffer);
                    Game.GraphicsDevice.Indices = chunk.IndexBuffer;
                    Game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, chunk.VertexBuffer.VertexCount, 0, chunk.IndexBuffer.IndexCount / 3);
#if DEBUG
                    ChunksDrawn++;
#endif
                }
            }
#if DEBUG
            StateStatistics[ChunkState.AwaitingGenerate] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.AwaitingGenerate);
            StateStatistics[ChunkState.Generating] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.Generating);
            StateStatistics[ChunkState.AwaitingLighting] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.AwaitingLighting);
            StateStatistics[ChunkState.Lighting] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.Lighting);
            StateStatistics[ChunkState.AwaitingBuild] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.AwaitingBuild);
            StateStatistics[ChunkState.Building] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.Building);
            StateStatistics[ChunkState.Ready] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.Ready);
            StateStatistics[ChunkState.AwaitingRemoval] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.AwaitingRemoval);
#endif
        }

        public Chunk GetChunkByWorldPosition(int x, int y, int z)
        {
            if (x < 0)
            {
                x -= Chunk.SizeInBlocks;
            }
            if (y < 0)
            {
                y -= Chunk.SizeInBlocks;
            }
            if (z < 0)
            {
                z -= Chunk.SizeInBlocks;
            }
            return !m_chunkStorage.ContainsKey(x / Chunk.SizeInBlocks, y / Chunk.SizeInBlocks, z / Chunk.SizeInBlocks) ? null : m_chunkStorage[x / Chunk.SizeInBlocks, y / Chunk.SizeInBlocks, z / Chunk.SizeInBlocks];
        }

        public Chunk GetChunkByWorldPosition(Vector3Int position)
        {
            return GetChunkByWorldPosition(position.X, position.Y, position.Z);
        }

        public Chunk GetChunkByRelativePosition(int x, int y, int z)
        {
            return !m_chunkStorage.ContainsKey(x, y, z) ? null : m_chunkStorage[x, y, z];
        }

        private Chunk GetNeighborChunk(Chunk origin, FaceDirection edge)
        {
            switch (edge)
            {
                case FaceDirection.XDecreasing:
                    return GetChunkByRelativePosition(origin.ChunkCachePosition.X - 1, origin.ChunkCachePosition.Y, origin.ChunkCachePosition.Z);
                case FaceDirection.XIncreasing:
                    return GetChunkByRelativePosition(origin.ChunkCachePosition.X + 1, origin.ChunkCachePosition.Y, origin.ChunkCachePosition.Z);
                case FaceDirection.YDecreasing:
                    return GetChunkByRelativePosition(origin.ChunkCachePosition.X, origin.ChunkCachePosition.Y - 1, origin.ChunkCachePosition.Z);
                case FaceDirection.YIncreasing:
                    return GetChunkByRelativePosition(origin.ChunkCachePosition.X, origin.ChunkCachePosition.Y + 1, origin.ChunkCachePosition.Z);
                case FaceDirection.ZDecreasing:
                    return GetChunkByRelativePosition(origin.ChunkCachePosition.X, origin.ChunkCachePosition.Y, origin.ChunkCachePosition.Z - 1);
                case FaceDirection.ZIncreasing:
                    return GetChunkByRelativePosition(origin.ChunkCachePosition.X, origin.ChunkCachePosition.Y, origin.ChunkCachePosition.Z + 1);
                default:
                    return null;
            }
        }

        public static int BlockIndexByWorldPosition(ref Vector3 position)
        {
            return BlockIndexByWorldPosition((int)position.X, (int)position.Y, (int)position.Z);
        }

        public static int BlockIndexByWorldPosition(int x, int y, int z)
        {
            var wrapX = MathUtilities.Modulo(x, CacheSizeInBlocks);
            var wrapY = MathUtilities.Modulo(y, CacheSizeInBlocks);
            var wrapZ = MathUtilities.Modulo(z, CacheSizeInBlocks);
            var flattenIndex = wrapX * BlockStepX + wrapZ * BlockStepZ + wrapY;
            return flattenIndex;
        }

        public bool CanHandleEvent(string eventName)
        {
            return m_eventSinkImpl.CanHandleEvent(eventName);
        }

        public Action<EventArgs> GetHandlerForEvent(string eventName)
        {
           return  m_eventSinkImpl.GetHandlerForEvent(eventName);
        }

        private struct WorldEdit
        {
            public enum WorldEditType
            {
                RemoveBlock,
                AddBlock
            }

            public Vector3Int Position;
            public WorldEditType EditType;

            public WorldEdit(Vector3Int position, WorldEditType editType)
            {
                Position = position;
                EditType = editType;
            }
        }
    }
}