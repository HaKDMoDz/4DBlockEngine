using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.AbstractClasses;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Debugging.Ingame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Graphics;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Chunks
{
    public enum ChunkState
    {
        AwaitingGenerate = 0,
        Generating,
        AwaitingLighting,
        Lighting,
        AwaitingBuild,
        Building,
        Ready,
        AwaitingRemoval
    }

    public sealed class Chunk : VertexBuilderTarget, IInGameDebuggable
    {
        public delegate Chunk GetNeighborChunk(Chunk origin, FaceDirection edge);
        public const int SizeInBlocks = 32;
        private readonly Block[] m_blocks;
        public Vector3Int ChunkCachePosition;

        public ChunkState ChunkState;
        private readonly MappingFunction m_mappingFunction;
        private readonly GetNeighborChunk m_getNeighborChunk;

        public Chunk(Vector3Int chunkCachePosition, Block[] blocks, MappingFunction mappingFunction, GetNeighborChunk getNeighborChunk)
        {
            ChunkState = ChunkState.AwaitingGenerate; // set initial state to awaiting generation.
            ChunkCachePosition = chunkCachePosition; // set the relative position.
            m_blocks = blocks;
            m_mappingFunction = mappingFunction;
            m_getNeighborChunk = getNeighborChunk;

            // calculate the real world position.
            Position = new Vector3Int(ChunkCachePosition.X * SizeInBlocks,
                                           ChunkCachePosition.Y * SizeInBlocks,
                                           ChunkCachePosition.Z * SizeInBlocks);

            // calculate bounding-box.
            BoundingBox = new BoundingBox(new Vector3(Position.X, Position.Y, Position.Z),
                                          new Vector3(Position.X + SizeInBlocks, Position.Y + SizeInBlocks,
                                                      Position.Z + SizeInBlocks));
#if DEBUG
            MainEngine.GetEngineInstance().DebugOnlyDebugManager.RegisterInGameDebuggable(this);
#endif

        }

        public static IEnumerable<FaceDirection> GetChunkEdgesBlockIsIn(int x, int y, int z)
        {
            var edges = new List<FaceDirection>();
            var localX = MathUtilities.Modulo(x, SizeInBlocks);
            var localY = MathUtilities.Modulo(y, SizeInBlocks);
            var localZ = MathUtilities.Modulo(z, SizeInBlocks);

            switch (localX)
            {
                case 0:
                    edges.Add(FaceDirection.XDecreasing);
                    break;
                case SizeInBlocks - 1:
                    edges.Add(FaceDirection.XIncreasing);
                    break;
            }
            switch (localY)
            {
                case 0:
                    edges.Add(FaceDirection.YDecreasing);
                    break;
                case SizeInBlocks - 1:
                    edges.Add(FaceDirection.YIncreasing);
                    break;
            }
            switch (localZ)
            {
                case 0:
                    edges.Add(FaceDirection.ZDecreasing);
                    break;
                case SizeInBlocks - 1:
                    edges.Add(FaceDirection.ZIncreasing);
                    break;
            }
            return edges;
        }

        public void AddBlock(int blockPositionX, int blockPositionZ, int blockPositionY)
        {
            if (blockPositionX > BoundingBox.Max.X)
            {
                BoundingBox.Max.X = blockPositionX;
            }
            else if (blockPositionX < BoundingBox.Min.X)
            {
                BoundingBox.Min.X = blockPositionX;
            }
            if (blockPositionZ > BoundingBox.Max.Z)
            {
                BoundingBox.Max.Z = blockPositionZ;
            }
            else if (blockPositionZ < BoundingBox.Min.Z)
            {
                BoundingBox.Min.Z = blockPositionZ;
            }
            if (blockPositionY > BoundingBox.Max.Y)
            {
                BoundingBox.Max.Y = blockPositionY;
            }
            else if (blockPositionY < BoundingBox.Min.Y)
            {
                BoundingBox.Min.Y = blockPositionY;
            }
        }

        public void RemoveBlock(int blockPositionX, int blockPositionZ, int blockPositionY)
        {
            if (blockPositionX == (int)BoundingBox.Max.X || blockPositionX == (int)BoundingBox.Min.X ||
               blockPositionZ == (int)BoundingBox.Max.Z || blockPositionZ == (int)BoundingBox.Min.Z ||
               blockPositionY == (int)BoundingBox.Max.Y || blockPositionY == (int)BoundingBox.Min.Y)
            {
                //When we do a remove we can't take a shortcut. Luckily only removals on the shell will change the bounding box
                UpdateBoundingBox();
            }
        }

        private Queue<VisibilityTraceNode> m_visibilityTraceQueue;

        public void RecalculateVisivility()
        {
            m_visibilityTraceQueue = new Queue<VisibilityTraceNode>();
            //We need to iterate over all blocks on each face of the chunk 
            foreach (FaceDirection direction in Enum.GetValues(typeof(FaceDirection)))
            {
                var workVector = new Vector3Int();
                for (var index = 0; index < SizeInBlocks * SizeInBlocks; ++index)
                {
                    int faceIndex = IterateOnFace(direction, ref workVector);
                    m_visibilityTraceQueue.Enqueue(new VisibilityTraceNode(workVector, faceIndex));
                }
                while(m_visibilityTraceQueue.Count > 0)
                {
                    var node = m_visibilityTraceQueue.Dequeue();
                    
                }
            }
        }

        private  struct VisibilityTraceNode
        {
            public Vector3Int Position;
            public int Index;
            public VisibilityTraceNode(Vector3Int position, int index)
            {
                Position = position;
                Index = index;
            }
        }

        private int IterateOnFace(FaceDirection direction, ref Vector3Int index)
        {
            int i1;
            switch(direction)
            {
                case FaceDirection.XIncreasing:                    
                case FaceDirection.XDecreasing:
                    i1 = index.Y;
                    index.Y = (i1 + 1) % SizeInBlocks;
                    if(index.Y < i1)
                    {
                        index.Z = (index.Z + 1) % SizeInBlocks;
                    }
                    break;
                case FaceDirection.YIncreasing:
                case FaceDirection.YDecreasing:
                    i1 = index.X;
                    index.X = (i1 + 1) % SizeInBlocks;
                    if(index.X < i1)
                    {
                        index.Z = (index.Z + 1) % SizeInBlocks;
                    }
                    break;
                case FaceDirection.ZIncreasing:
                case FaceDirection.ZDecreasing:
                    i1 = index.Y;
                    index.Y = (i1 + 1) % SizeInBlocks;
                    if(index.Y < i1)
                    {
                        index.X = (index.X + 1) % SizeInBlocks;
                    }
                    break;
            }
            return m_mappingFunction(index.X, index.Y, index.Z);
        }

        public void UpdateBoundingBox()
        {
            var upperBoundX = 0;
            var lowerBoundX = SizeInBlocks;
            var upperBoundY = 0;
            var lowerBoundY = SizeInBlocks;
            var upperBoundZ = 0;
            var lowerBoundZ = SizeInBlocks;
            for (var x = 0; x < SizeInBlocks; ++x)
            {
                var worldPositionX = Position.X + x;
                var foundBlockX = false;
                for (var z = 0; z < SizeInBlocks; ++z)
                {
                    var worldPositionZ = Position.Z + z;
                    var foundBlockZ = false;
                    for (var y = 0; y < SizeInBlocks; ++y)
                    {
                        var worldPositionY = Position.Y + y;
                        var samplePosition = m_mappingFunction(worldPositionX, worldPositionY, worldPositionZ);
                        if (!m_blocks[samplePosition].Exists)
                        {
                            continue;
                        }
                        foundBlockZ = true;
                        foundBlockX = true;
                        if (y > upperBoundY)
                        {
                            upperBoundY = y;
                        }
                        if (y < lowerBoundY)
                        {
                            lowerBoundY = y;
                        }
                    }
                    if (!foundBlockZ)
                    {
                        continue;
                    }
                    if (z > upperBoundZ)
                    {
                        upperBoundZ = z;
                    }
                    if (z < lowerBoundZ)
                    {
                        lowerBoundZ = z;
                    }
                }
                if (!foundBlockX)
                {
                    continue;
                }
                if (x > upperBoundX)
                {
                    upperBoundX = x;
                }
                if (x < lowerBoundX)
                {
                    lowerBoundX = x;
                }
            }
            if (lowerBoundX > upperBoundX || lowerBoundY > upperBoundY || lowerBoundZ > upperBoundZ)
            {
                BoundingBox.Min.X = Position.X;
                BoundingBox.Max.X = Position.X;
                BoundingBox.Min.Y = Position.Y;
                BoundingBox.Max.Y = Position.Y;
                BoundingBox.Min.Z = Position.Z;
                BoundingBox.Max.Z = Position.Z;
            }
            else
            {
                BoundingBox.Min.X = lowerBoundX + Position.X;
                BoundingBox.Max.X = upperBoundX + Position.X + 1;
                BoundingBox.Min.Y = lowerBoundY + Position.Y;
                BoundingBox.Max.Y = upperBoundY + Position.Y + 1;
                BoundingBox.Min.Z = lowerBoundZ + Position.Z;
                BoundingBox.Max.Z = upperBoundZ + Position.Z + 1;
            }
        }


        public override string ToString()
        {
            return string.Format("{0} {1}", ChunkCachePosition, ChunkState);
        }

        public override void SetMeshDirty(int x, int y, int z)
        {
            if (ChunkState == ChunkState.Ready)
            {
                ChunkState = ChunkState.AwaitingBuild;
            }
            var edgesBlockIsIn = GetChunkEdgesBlockIsIn(x, y, z);
            foreach (var edge in edgesBlockIsIn)
            {
                var neighborChunk = m_getNeighborChunk(this, edge);
                if (neighborChunk != null && neighborChunk.ChunkState == ChunkState.Ready)
                {
                    neighborChunk.ChunkState = ChunkState.AwaitingBuild;
                }
            }
        }


        public void DrawInGameDebugVisual(GraphicsDevice graphicsDevice, Camera camera, SpriteBatch spriteBatch, SpriteFont spriteFont)
        {
            var color = Color.Blue;
            if (ChunkState == ChunkState.Ready)
            {
                color = Color.DarkRed;
            }
            BoundingBoxRenderer.Render(BoundingBox, graphicsDevice, camera.View, camera.Projection, color);
        }

        public void PrepForRemoval()
        {
            ChunkState = ChunkState.AwaitingRemoval;
            if (VertexBuffer != null)
            {
                VertexBuffer.Dispose();
                VertexBuffer = null;
            }
            if (IndexBuffer != null)
            {
                IndexBuffer.Dispose();
                IndexBuffer = null;
            }
            //TODO : if dirty flag is true send this chunk to the persistance manager
        }
    }

    public struct Visibility
    {
        public const int TopBottom = 1 << 0;
        public const int TopLeft = 1 << 1;
        public const int TopRight = 1 << 2;
        public const int TopFront = 1 << 3;
        public const int TopBack = 1 << 4;
        public const int LeftBottom = 1 << 5;
        public const int LeftRight = 1 << 6;
        public const int LeftFront = 1 << 7;
        public const int LeftBack = 1 << 8;
        public const int RightBottom = 1 << 9;
        public const int RightFront = 1 << 10;
        public const int RightBack = 1 << 11;
        public const int FrontBottom = 1 << 12;
        public const int FrontBack = 1 << 13;
        public const int BackBottom = 1 << 14;

        public static readonly int[] Directions = new[] { TopBottom, TopLeft, TopRight, TopBack, LeftBottom,
                                                      LeftRight, LeftFront, LeftBack, RightBottom, RightFront, 
                                                      RightBack, FrontBottom ,FrontBack, BackBottom};
        private int m_bitVector;

        public Visibility(int visibilityVector)
        {
            m_bitVector = visibilityVector;
        }

        public void SetVisiblity(int direction, bool state)
        {
            m_bitVector = state ? (m_bitVector | direction) : (m_bitVector & ~direction);
        }

        public bool GetVisibility(int direction)
        {
            return (m_bitVector & direction) != 0;
        }
    }
}