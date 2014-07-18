using System;
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
        private const byte CacheRange = 12;
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

        private const int CacheSizeInBlocks = (CacheRange*2 + 1)*Chunk.SizeInBlocks;
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

        private readonly TerrainGenerator m_generator;
        private readonly CellularLighting<Block> m_lightingEngine;
        private readonly VertexBuilder<Block> m_vertexBuilder;
        private readonly SparseArray3D<Chunk> m_chunkStorage;
        private Vector4 m_cacheCenterPosition;
        private readonly Action<EventArgs> m_wrappedPositionHandler; 

        private bool m_cachePositionUpdated;

        public Vector4 CachePosition { get { return m_cacheCenterPosition; } }

        public Block[] Blocks { get; private set; }
        private readonly Logger m_logger;
        private StartUpState m_startUpState;

        public ChunkCache(Game game, uint seed) : base(game)
        {
            m_logger = MainEngine.GetEngineInstance().GetLogger("ChunkCache");
            Debug.Assert(game != null);
            var graphicsDevice = game.GraphicsDevice;
            Debug.Assert(ViewRange < CacheRange);
            Debug.Assert(graphicsDevice != null);
            Blocks = new Block[CacheSizeInBlocks * CacheSizeInBlocks * CacheSizeInBlocks];
            m_generator = new TerrainGenerator(Chunk.SizeInBlocks, Blocks, seed);
            m_lightingEngine = new CellularLighting<Block>(Blocks, BlockIndexByWorldPosition, Chunk.SizeInBlocks, BlockIndexOffsetX, BlockIndexOffsetY, BlockIndexOffsetZ);
            m_vertexBuilder = new VertexBuilder<Block>(Blocks, BlockIndexByWorldPosition, graphicsDevice);
            m_chunkStorage = new SparseArray3D<Chunk>(CacheRange * 2 + 1, CacheRange * 2 + 1);
            m_cacheCenterPosition = new Vector4();
            m_wrappedPositionHandler = EventHelper.Wrap<Vector3Args>(UpdateCachePosition);
            m_startUpState = StartUpState.NotStarted;
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
            if (chunk != null)
            {
                var flattenIndex = BlockIndexByWorldPosition(x, y, z);
                Blocks[flattenIndex] = block;
                chunk.AddBlock(x, y, z);
                m_lightingEngine.AddBlock(flattenIndex);
                UpdateChunkStateAfterModification(chunk, x, y, z);
            }
        }

        public void RemoveBlock(int x, int y, int z)
        {
            var chunk = GetChunkByWorldPosition(x, y, z); // get the chunk that block is hosted in.
            if (chunk != null)
            {
                var flattenIndex = BlockIndexByWorldPosition(x, y, z);
                Blocks[flattenIndex] = Block.Empty;
                chunk.RemoveBlock(x, y, z);
                m_lightingEngine.RemoveBlock(flattenIndex);
                UpdateChunkStateAfterModification(chunk, x, y, z);
            }
        }

        private void UpdateChunkStateAfterModification(Chunk chunk, int x, int y, int z)
        {
            var edgesBlockIsIn = Chunk.GetChunkEdgesBlockIsIn(x, y, z);
            foreach (var edge in edgesBlockIsIn)
            {
                var neighborChunk = GetNeighborChunk(chunk, edge);
                neighborChunk.ChunkState = ChunkState.AwaitingBuild;
            }
            chunk.ChunkState = ChunkState.AwaitingBuild;
        }

        private void UpdateCachePosition(Vector3Args args)
        {
            UpdateCachePosition((int)args.Vector.X, (int)args.Vector.Y, (int)args.Vector.Z);
        }

        public void UpdateCachePosition(int x, int y, int z)
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

        public override void Update(GameTime gameTime)
        {
            if(m_startUpState == StartUpState.AwaitingStart)
            {
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
        }

        public bool IsInViewRange(Chunk chunk)
        {
            return m_viewRangeBoundingBox.Contains(chunk.BoundingBox) == ContainmentType.Contains;
        }

        public bool IsInViewRange(int x, int y, int z)
        {
            return !(x < m_viewRangeBoundingBox.Min.X || z < m_viewRangeBoundingBox.Min.Z || x >= m_viewRangeBoundingBox.Max.X ||
                     z >= m_viewRangeBoundingBox.Max.Z || y < m_viewRangeBoundingBox.Min.Y || y >= m_viewRangeBoundingBox.Max.Y);
        }

        public bool IsInCacheRange(Chunk chunk)
        {
            return m_cacheRangeBoundingBox.Contains(chunk.BoundingBox) == ContainmentType.Contains;
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
            }
// ReSharper disable once FunctionNeverReturns
        }

        private void Process()
        {
            foreach (var chunk in m_chunkStorage.Values.ToArray())
            {
                if (IsInViewRange(chunk))
                {
                    ProcessChunkInViewRange(chunk);
                }
                else if (IsInCacheRange(chunk))
                {
                    ProcessChunkInCacheRange(chunk);
                }
            }
            if (m_cachePositionUpdated)
            {
                RecacheChunks();
            }
        }

        private void RecacheChunks()
        {
            foreach (var chunk in m_chunkStorage.Values.Where(chunk => !IsInCacheRange(chunk)))
            {
                m_chunkStorage.Remove(chunk.ChunkCachePosition.X, chunk.ChunkCachePosition.Y, chunk.ChunkCachePosition.Z);
                chunk.PrepForRemoval();
            }
            var chunkPosition = new Vector3Int((int)(m_cacheCenterPosition.X / Chunk.SizeInBlocks), (int)(m_cacheCenterPosition.Y / Chunk.SizeInBlocks), ((int)m_cacheCenterPosition.Z / Chunk.SizeInBlocks));
            for (var z = -CacheRange; z <= CacheRange; z++)
            {
                for (var y = -CacheRange; y <= CacheRange; y++)
                {
                    for (var x = -CacheRange; x <= CacheRange; x++)
                    {
                        if (m_chunkStorage.ContainsKey(chunkPosition.X + x,
                            chunkPosition.Y + y, chunkPosition.Z + z))
                        {
                            continue;
                        }
                        var chunk = new Chunk(new Vector3Int(chunkPosition.X + x, chunkPosition.Y + y, chunkPosition.Z + z), Blocks);
                        m_chunkStorage[chunk.ChunkCachePosition.X, chunk.ChunkCachePosition.Y, chunk.ChunkCachePosition.Z] = chunk;
                    }
                }
            }
        }

        private void ProcessChunkInCacheRange(Chunk chunk)
        {
            if (chunk.ChunkState == ChunkState.AwaitingGenerate)
            {
                chunk.ChunkState = ChunkState.Generating;
                m_generator.GenerateDataForChunk(chunk.Position.X, chunk.Position.Y, chunk.Position.Z, 0);
               // chunk.UpdateBoundingBox();
                chunk.ChunkState = ChunkState.AwaitingLighting;
            }
        }

        private void ProcessChunkInViewRange(Chunk chunk)
        {
            switch (chunk.ChunkState) 
            {
                case ChunkState.AwaitingGenerate:
                    chunk.ChunkState = ChunkState.Generating;
                    m_generator.GenerateDataForChunk(chunk.Position.X, chunk.Position.Y, chunk.Position.Z, 0);
                  //  chunk.UpdateBoundingBox();
                    chunk.ChunkState = ChunkState.AwaitingLighting;
                    break;
                case ChunkState.AwaitingLighting:
                    chunk.ChunkState = ChunkState.Lighting;
                    m_lightingEngine.Process(chunk.Position.X, chunk.Position.Y, chunk.Position.Z, chunk.LightSources);
                    chunk.ChunkState = ChunkState.AwaitingBuild;
                    break;
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

            // general parameters
            m_blockEffect.Parameters["World"].SetValue(Matrix.Identity);
            m_blockEffect.Parameters["View"].SetValue(m_camera.View);
            m_blockEffect.Parameters["Projection"].SetValue(m_camera.Projection);
            m_blockEffect.Parameters["CameraPosition"].SetValue(m_camera.Ray.Position);

            // texture parameters
            m_blockEffect.Parameters["BlockTextureAtlas"].SetValue(m_blockTextureAtlas);

            // atmospheric settings
            m_blockEffect.Parameters["SunColor"].SetValue(RenderingConstants.SunColor);
            m_blockEffect.Parameters["NightColor"].SetValue(RenderingConstants.NightColor);
            m_blockEffect.Parameters["HorizonColor"].SetValue(RenderingConstants.HorizonColor);
            m_blockEffect.Parameters["MorningTint"].SetValue(RenderingConstants.MorningTint);
            m_blockEffect.Parameters["EveningTint"].SetValue(RenderingConstants.EveningTint);

            // time of day parameters
            m_blockEffect.Parameters["TimeOfDay"].SetValue(m_getTimeOfDay());

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

        public Chunk GetChunkByRelativePosition(int x, int y, int z)
        {
            return !m_chunkStorage.ContainsKey(x, y, z) ? null : m_chunkStorage[x, y, z];
        }

        public Chunk GetNeighborChunk(Chunk origin, FaceDirection edge)
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

        public static int BlockIndexOffsetX(int offset, int x = 0)
        {
            var wrapX = offset / BlockStepX;
            var wrapZ = offset / BlockStepZ - wrapX;
            var wrapY = offset - (wrapX + wrapZ);
            return BlockIndexByWorldPosition(wrapX + x, wrapY, wrapZ);
        }

        public static int BlockIndexOffsetY(int offset, int y = 0)
        {
            var wrapX = offset / BlockStepX;
            var wrapZ = offset / BlockStepZ - wrapX;
            var wrapY = offset - (wrapX + wrapZ);
            return BlockIndexByWorldPosition(wrapX, wrapY + y, wrapZ);
        }

        public static int BlockIndexOffsetZ(int offset, int z = 0)
        {
            var wrapX = offset / BlockStepX;
            var wrapZ = offset / BlockStepZ - wrapX;
            var wrapY = offset - (wrapX + wrapZ);
            return BlockIndexByWorldPosition(wrapX, wrapY, wrapZ + z);
        }

        public static int BlockIndexByRelativePosition(Chunk chunk, int x, int y, int z)
        {
            var xIndex = chunk.Position.X + x;
            var yIndex = chunk.Position.Y + y;
            var zIndex = chunk.Position.Z + z;
            var wrapX = MathUtilities.Modulo(xIndex, CacheSizeInBlocks);
            var wrapY = MathUtilities.Modulo(yIndex, CacheSizeInBlocks);
            var wrapZ = MathUtilities.Modulo(zIndex, CacheSizeInBlocks);
            var flattenIndex = wrapX * BlockStepX + wrapZ * BlockStepZ + wrapY;
            return flattenIndex;
        }

        public bool CanHandleEvent(string eventName)
        {
            switch (eventName)
            {
                case EventConstants.PlayerPositionUpdated:
                    return true;
                default:
                    return false;
            }
        }

        public Action<EventArgs> GetHandlerForEvent(string eventName)
        {
            switch (eventName)
            {
                case EventConstants.PlayerPositionUpdated:
                    return m_wrappedPositionHandler;
                default:
                    return null;
            }
        }

        public Chunk GetChunkByWorldPosition(Vector3 position)
        {
            return GetChunkByWorldPosition((int)position.X, (int)position.Y, (int)position.Z);
        }
    }
}