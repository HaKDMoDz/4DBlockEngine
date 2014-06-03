using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Chunks.Generators;
using _4DMonoEngine.Core.Chunks.Processors;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Vector;
using _4DMonoEngine.Core.Common;
using _4DMonoEngine.Core.Universe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Chunks
{

    public class ChunkCache : WorldRenderable
    {
        public static byte CacheRange = 16;
        public BoundingBox CacheRangeBoundingBox;
        /*TODO : Implement LOD here
         * 
         * The level of detail zones should be smaller than ViewRange.
         * This means that we will have to make the cache much larger.
         * Some form of paging scheme may need to be devised to avoid 
         * issues with the Large Object Heap
         * 
         */
        public static byte ViewRange = 12;
        public BoundingBox ViewRangeBoundingBox;

        public static int CacheSizeInBlocks = (CacheRange * 2 + 1) * Chunk.SizeInBlocks;
        public static readonly int BlockStepX = Chunk.SizeInBlocks * Chunk.SizeInBlocks;
        public static readonly int BlockStepZ = Chunk.SizeInBlocks;
        public static readonly int BlockStepY = 1;


        public int ChunksDrawn;

        // assets & resources
        private Effect m_blockEffect; // block effect.
        private Texture2D m_blockTextureAtlas; // block texture atlas

        private readonly TerrainGenerator m_generator;
        private readonly CellularLighting<Block> m_lightingEngine;
        private readonly VertexBuilder<Block> m_vertexBuilder;
        private readonly SparseArray3D<Chunk> m_chunkStorage;
        private Vector4 m_cacheCenterPosition;
        private Chunk m_currentChunk;

        public bool CacheThreadStarted;

        public Dictionary<ChunkState, int> StateStatistics { get; private set; }
        public readonly Block[] Blocks;


        public ChunkCache(GraphicsDevice graphicsDevice, BlockDictionary blockDictionary, int seed)
        {
            if (ViewRange > CacheRange)
            {
                throw new ChunkCacheException();
            }

            Blocks = new Block[CacheSizeInBlocks * CacheSizeInBlocks * CacheSizeInBlocks];
            m_generator = new TerrainGenerator(blockDictionary, Chunk.SizeInBlocks, seed);
            m_lightingEngine = new CellularLighting<Block>(Blocks);
            m_vertexBuilder = new VertexBuilder<Block>(Blocks, BlockIndexByWorldPosition, graphicsDevice);
            m_chunkStorage = new SparseArray3D<Chunk>(CacheRange * 2 + 1, CacheRange * 2 + 1);
            m_cacheCenterPosition = new Vector4();
            CacheThreadStarted = false;
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
        }

        public override void LoadContent()
        {
            m_blockEffect = MainEngine.GetEngineInstance().GetAsset<Effect>("BlockEffect");
            m_blockTextureAtlas = MainEngine.GetEngineInstance().GetAsset<Texture2D>("BlockTextureAtlas");
        }

        public override void Update(GameTime gameTime)
        {
            
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

        public void UpdateCachePosition(int x, int y, int z)
        {
            m_cacheCenterPosition.X = x;
            m_cacheCenterPosition.Y = y;
            m_cacheCenterPosition.Z = z;
            UpdateBoundingBoxes();
            if (!CacheThreadStarted)
            {
                var cacheThread = new Thread(CacheThread) {IsBackground = true};
                cacheThread.Start();
                CacheThreadStarted = true;
            }
        }

        public bool IsInViewRange(Chunk chunk)
        {
            return ViewRangeBoundingBox.Contains(chunk.BoundingBox) == ContainmentType.Contains;
        }

        public bool IsInViewRange(int x, int y, int z)
        {
            return !(x < ViewRangeBoundingBox.Min.X || z < ViewRangeBoundingBox.Min.Z || x >= ViewRangeBoundingBox.Max.X ||
                     z >= ViewRangeBoundingBox.Max.Z || y < ViewRangeBoundingBox.Min.Y || y >= ViewRangeBoundingBox.Max.Y);
        }

        public bool IsInCacheRange(Chunk chunk)
        {
            return CacheRangeBoundingBox.Contains(chunk.BoundingBox) == ContainmentType.Contains;
        }

        public bool IsInCacheRange(int x, int y, int z)
        {
            return !(x < CacheRangeBoundingBox.Min.X || y < CacheRangeBoundingBox.Min.Y || z < CacheRangeBoundingBox.Min.Z ||
                     x > CacheRangeBoundingBox.Max.X || y > CacheRangeBoundingBox.Max.Y || z > CacheRangeBoundingBox.Max.Z);
        }

        protected void UpdateBoundingBoxes()
        {
            ViewRangeBoundingBox = new BoundingBox(
                        new Vector3(m_cacheCenterPosition.X - (ViewRange * Chunk.SizeInBlocks), m_cacheCenterPosition.Y - (ViewRange * Chunk.SizeInBlocks),
                                    m_cacheCenterPosition.Z - (ViewRange * Chunk.SizeInBlocks)),
                        new Vector3(m_cacheCenterPosition.X + (ViewRange * Chunk.SizeInBlocks), m_cacheCenterPosition.Y + (ViewRange * Chunk.SizeInBlocks),
                                    m_cacheCenterPosition.Z + (ViewRange * Chunk.SizeInBlocks)));
                
            CacheRangeBoundingBox = new BoundingBox(
                        new Vector3(m_cacheCenterPosition.X - (CacheRange * Chunk.SizeInBlocks), m_cacheCenterPosition.Y - (CacheRange * Chunk.SizeInBlocks),
                                    m_cacheCenterPosition.Z - (CacheRange * Chunk.SizeInBlocks)),
                        new Vector3(m_cacheCenterPosition.X + (CacheRange * Chunk.SizeInBlocks), m_cacheCenterPosition.Y + (CacheRange * Chunk.SizeInBlocks),
                                    m_cacheCenterPosition.Z + (CacheRange * Chunk.SizeInBlocks)));
        }

        private void CacheThread()
        {
            while (true)
            {
                if (m_currentChunk != null)
                {
                    Process();
                }
            }
// ReSharper disable once FunctionNeverReturns
        }

        protected void Process()
        {
            foreach (var chunk in m_chunkStorage.Values)
            {
                if (IsInViewRange(chunk))
                {
                    ProcessChunkInViewRange(chunk);
                }
                else
                {
                    if (IsInCacheRange(chunk))
                    {
                        ProcessChunkInCacheRange(chunk);
                    }
                    else
                    {
                        m_chunkStorage.Remove(chunk.RelativePosition.X, chunk.RelativePosition.Y, chunk.RelativePosition.Z);
                        chunk.PrepForRemoval();
                    }
                }
            }
            RecacheChunks();
        }

        private void RecacheChunks()
        {
            m_currentChunk = GetChunkByWorldPosition((int)m_cacheCenterPosition.X, (int)m_cacheCenterPosition.Y, (int)m_cacheCenterPosition.Z);
            if (m_currentChunk != null)
            {
                for (var z = -CacheRange; z <= CacheRange; z++)
                {
                    for (var y = -CacheRange; y <= CacheRange; y++)
                    {
                        for (var x = -CacheRange; x <= CacheRange; x++)
                        {
                            if (!m_chunkStorage.ContainsKey(m_currentChunk.RelativePosition.X + x, m_currentChunk.RelativePosition.Y + y, m_currentChunk.RelativePosition.Z + z))
                            {
                                var chunk = new Chunk(new Vector3Int(m_currentChunk.RelativePosition.X + x, m_currentChunk.RelativePosition.Y + y, m_currentChunk.RelativePosition.Z + z));
                                m_chunkStorage[chunk.RelativePosition.X, chunk.RelativePosition.Y, chunk.RelativePosition.Z] = chunk;
                            }
                        }
                    }
                }
            }
        }

        private void ProcessChunkInCacheRange(Chunk chunk)
        {
            if (chunk.ChunkState == ChunkState.AwaitingGenerate)
            {
                m_generator.GenerateDataForChunk(chunk.Position, 0);
            }
        }

        private void ProcessChunkInViewRange(Chunk chunk)
        {
            switch (chunk.ChunkState) // switch on the chunk state.
            {
                case ChunkState.AwaitingGenerate:
                    m_generator.GenerateDataForChunk(chunk.Position, 0);
                    break;
                case ChunkState.AwaitingLighting:
                    m_lightingEngine.Process(chunk);
                    break;
                case ChunkState.AwaitingBuild:
                    chunk.ChunkState = ChunkState.Building; // set chunk state to building.
                    m_vertexBuilder.Build(chunk);
                    chunk.ChunkState = ChunkState.Ready; // chunk is all ready now.
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
            m_blockEffect.Parameters["CameraPosition"].SetValue(m_camera.Position);

            // texture parameters
            m_blockEffect.Parameters["BlockTextureAtlas"].SetValue(m_blockTextureAtlas);

            // atmospheric settings
            m_blockEffect.Parameters["SunColor"].SetValue(World.SunColor);
            m_blockEffect.Parameters["NightColor"].SetValue(World.NightColor);
            m_blockEffect.Parameters["HorizonColor"].SetValue(World.HorizonColor);
            m_blockEffect.Parameters["MorningTint"].SetValue(World.MorningTint);
            m_blockEffect.Parameters["EveningTint"].SetValue(World.EveningTint);

            // time of day parameters
            m_blockEffect.Parameters["TimeOfDay"].SetValue(m_getTimeOfDay());

            // fog parameters
            m_blockEffect.Parameters["FogNear"].SetValue(m_getFogVector().X);
            m_blockEffect.Parameters["FogFar"].SetValue(m_getFogVector().Y);


            ChunksDrawn = 0;
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
                    Game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, chunk.VertexBuffer.VertexCount, 0, chunk.IndexBuffer.IndexCount/3);

                    ChunksDrawn++;
                }
            }

            StateStatistics[ChunkState.AwaitingGenerate] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.AwaitingGenerate);
            StateStatistics[ChunkState.Generating] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.Generating);
            StateStatistics[ChunkState.AwaitingLighting] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.AwaitingLighting);
            StateStatistics[ChunkState.Lighting] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.Lighting);
            StateStatistics[ChunkState.AwaitingBuild] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.AwaitingBuild);
            StateStatistics[ChunkState.Building] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.Building);
            StateStatistics[ChunkState.Ready] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.Ready);
            StateStatistics[ChunkState.AwaitingRemoval] = m_chunkStorage.Values.Count(chunk => chunk.ChunkState == ChunkState.AwaitingRemoval);
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
                    return GetChunkByRelativePosition(origin.RelativePosition.X - 1, origin.RelativePosition.Y, origin.RelativePosition.Z);
                case FaceDirection.XIncreasing:
                    return GetChunkByRelativePosition(origin.RelativePosition.X + 1, origin.RelativePosition.Y, origin.RelativePosition.Z);
                case FaceDirection.YDecreasing:
                    return GetChunkByRelativePosition(origin.RelativePosition.X, origin.RelativePosition.Y - 1, origin.RelativePosition.Z);
                case FaceDirection.YIncreasing:
                    return GetChunkByRelativePosition(origin.RelativePosition.X, origin.RelativePosition.Y + 1, origin.RelativePosition.Z);
                case FaceDirection.ZDecreasing:
                    return GetChunkByRelativePosition(origin.RelativePosition.X, origin.RelativePosition.Y, origin.RelativePosition.Z - 1);
                case FaceDirection.ZIncreasing:
                    return GetChunkByRelativePosition(origin.RelativePosition.X, origin.RelativePosition.Y, origin.RelativePosition.Z + 1);
                default:
                    return null;
            }
        }

        public static int BlockIndexByWorldPosition(int x, int y, int z)
        {
            var wrapX = MathUtilities.Modulo(x, CacheSizeInBlocks);
            var wrapY = MathUtilities.Modulo(y, CacheSizeInBlocks);
            var wrapZ = MathUtilities.Modulo(z, CacheSizeInBlocks);
            var flattenIndex = wrapX * BlockStepX + wrapZ * BlockStepZ + wrapY;
            return flattenIndex;
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
    }

    public class ChunkCacheException : Exception
    {
        public ChunkCacheException() : base("View range can not be larger than cache range!")
        { }
    }
}