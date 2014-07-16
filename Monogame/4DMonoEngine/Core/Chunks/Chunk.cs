using System.Collections.Generic;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.AbstractClasses;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Common.Structs;
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
        AwaitingGenerate,
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
        public const byte MaxSunValue = 255;
        public const int SizeInBlocks = 16;
        private readonly Block[] m_blocks;
        public Vector3Int ChunkCachePosition;

        public SparseArray3D<Vector3Byte> LightSources;

        public ChunkState ChunkState;
        
        public Chunk(Vector3Int chunkCachePosition, Block[] blocks)
        {
            ChunkState = ChunkState.AwaitingGenerate; // set initial state to awaiting generation.
            ChunkCachePosition = chunkCachePosition; // set the relative position.
            m_blocks = blocks;

            // calculate the real world position.
            Position = new Vector3Int(ChunkCachePosition.X * SizeInBlocks,
                                           ChunkCachePosition.Y * SizeInBlocks,
                                           ChunkCachePosition.Z * SizeInBlocks);

            // calculate bounding-box.
            BoundingBox = new BoundingBox(new Vector3(Position.X, Position.Y, Position.Z),
                                          new Vector3(Position.X + SizeInBlocks, Position.Y + SizeInBlocks,
                                                      Position.Z + SizeInBlocks));

            // create vertex & index lists.
            VertexList = new List<BlockVertex>();
            IndexList = new List<short>();
            LightSources = new SparseArray3D<Vector3Byte>(SizeInBlocks, SizeInBlocks);
        }

        public bool IsInBounds(float x, float y, float z)
        {
            return !(x < BoundingBox.Min.X || y < BoundingBox.Min.Y || z < BoundingBox.Min.Z || x > BoundingBox.Max.X || y > BoundingBox.Max.Y || z > BoundingBox.Max.Z);
        }

        public static IEnumerable<FaceDirection> GetChunkEdgesBlockIsIn(int x, int y, int z)
        {
            var edges = new List<FaceDirection>();

            var localX = MathUtilities.Modulo(x, SizeInBlocks);
            var localY = MathUtilities.Modulo(y, SizeInBlocks);
            var localZ = MathUtilities.Modulo(z, SizeInBlocks);

            if (localX == 0)
            {
                edges.Add(FaceDirection.XDecreasing);
            }
            else if (localX == SizeInBlocks - 1)
            {
                edges.Add(FaceDirection.XIncreasing);
            }
            if (localY == 0)
            {
                edges.Add(FaceDirection.YDecreasing);
            }
            else if (localY == SizeInBlocks - 1)
            {
                edges.Add(FaceDirection.YIncreasing);
            }
            if (localZ == 0)
            {
                edges.Add(FaceDirection.ZDecreasing);
            }
            else if (localZ == SizeInBlocks - 1)
            {
                edges.Add(FaceDirection.ZIncreasing);
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

        private void UpdateBoundingBox()
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
                        var samplePosition = ChunkCache.BlockIndexByWorldPosition(worldPositionX, Position.Y + y, worldPositionZ);
                        if (!m_blocks[samplePosition].Exists)
                        {
                            continue;
                        }
                        foundBlockZ = true;
                        if (y > upperBoundY)
                        {
                            upperBoundY = y;
                        }
                        else if (y < lowerBoundY)
                        {
                            lowerBoundY = y;
                        }
                    }
                    if (foundBlockZ)
                    {
                        if (z > upperBoundZ)
                        {
                            upperBoundZ = z;
                        }
                        else if (z < lowerBoundZ)
                        {
                            lowerBoundZ = z;
                        }
                    }
                    foundBlockX |= foundBlockZ;
                }
                if (!foundBlockX)
                {
                    continue;
                }
                if (x > upperBoundX)
                {
                    upperBoundX = x;
                }
                else if (x < lowerBoundZ)
                {
                    lowerBoundX = x;
                }
            }
            BoundingBox.Min.X = lowerBoundX + Position.X;
            BoundingBox.Max.X = upperBoundX + Position.X;
            BoundingBox.Min.Y = lowerBoundY + Position.Y;
            BoundingBox.Max.Y = upperBoundY + Position.Y;
            BoundingBox.Min.Z = lowerBoundZ + Position.Z;
            BoundingBox.Max.Z = upperBoundZ + Position.Z;
        }


        public override string ToString()
        {
            return string.Format("{0} {1}", ChunkCachePosition, ChunkState);
        }

        
        public void DrawInGameDebugVisual(GraphicsDevice graphicsDevice, Camera camera, SpriteBatch spriteBatch, SpriteFont spriteFont)
        {
            var position = ChunkCachePosition + " " + ChunkState;
            var positionSize = spriteFont.MeasureString(position);

            var projected = graphicsDevice.Viewport.Project(Vector3.Zero, camera.Projection, camera.View,
                                                            Matrix.CreateTranslation(new Vector3(Position.X + SizeInBlocks / 2, Position.Y + SizeInBlocks / 2, Position.Z + SizeInBlocks / 2)));
            spriteBatch.DrawString(spriteFont, position, new Vector2(projected.X - positionSize.X/2, projected.Y - positionSize.Y/2), Color.Yellow);
            BoundingBoxRenderer.Render(BoundingBox, graphicsDevice, camera.View, camera.Projection, Color.DarkRed);
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
}