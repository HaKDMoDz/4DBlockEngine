using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using _4DMonoEngine.Core.Common.AbstractClasses;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Common.Structs;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Processors
{
    public class VertexBuilder<T> where T : ILightable
    {
        private readonly GraphicsDevice m_graphicsDevice;
        private readonly T[] m_blockSource;
        private readonly MappingFunction m_mappingFunction;
        private readonly int m_blockScale;
        private Vector3 m_origin;

        public VertexBuilder(T[] blockSource, MappingFunction mappingFunction, GraphicsDevice graphicsDevice, int blockScale = 1)
        {
            m_blockSource = blockSource;
            m_graphicsDevice = graphicsDevice;
            m_blockScale = blockScale;
            m_mappingFunction = mappingFunction;
        }

        public static void Clear(VertexBuilderTarget vertexBuilderTarget)
        {
            vertexBuilderTarget.VertexList.Clear();
            vertexBuilderTarget.IndexList.Clear();
            vertexBuilderTarget.Index = 0;
        }

        public void Build(VertexBuilderTarget vertexBuilderTarget)
        {
            Clear(vertexBuilderTarget);
            m_origin = vertexBuilderTarget.BoundingBox.Min;
            for (var x = (int)vertexBuilderTarget.BoundingBox.Min.X; x < vertexBuilderTarget.BoundingBox.Max.X; x++)
            {
                for (var z = (int)vertexBuilderTarget.BoundingBox.Min.Z; z < vertexBuilderTarget.BoundingBox.Max.Z; z++)
                {
                    for (var y = (int)vertexBuilderTarget.BoundingBox.Min.Y; y < vertexBuilderTarget.BoundingBox.Max.Y; y++)
                    {
                        var position = new Vector3Int(x, y, z);
                        var blockIndex = m_mappingFunction(x, y, z);
                        var block = m_blockSource[blockIndex];
                        if (block.Exists && m_blockSource[blockIndex].Opacity > 0)
                        {
                            BuildBlockVertices(vertexBuilderTarget, blockIndex, position);
                        }
                    }
                }
            }

            var vertices = vertexBuilderTarget.VertexList.ToArray();
            var indices = vertexBuilderTarget.IndexList.ToArray();

            if (vertices.Length > 0 && indices.Length > 0)
            {

                vertexBuilderTarget.VertexBuffer = new VertexBuffer(m_graphicsDevice, typeof(BlockVertex), vertices.Length, BufferUsage.WriteOnly);
                vertexBuilderTarget.VertexBuffer.SetData(vertices);

                vertexBuilderTarget.IndexBuffer = new IndexBuffer(m_graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
                vertexBuilderTarget.IndexBuffer.SetData(indices);
            }
        }

        private void BuildBlockVertices(VertexBuilderTarget vertexBuilderTarget, int blockIndex, Vector3Int worldPosition)
        {
            var tNw = m_mappingFunction(worldPosition.X - 1, worldPosition.Y + 1, worldPosition.Z + 1);
            var tN = m_mappingFunction(worldPosition.X, worldPosition.Y + 1, worldPosition.Z + 1);
            var tNe = m_mappingFunction(worldPosition.X + 1, worldPosition.Y + 1, worldPosition.Z + 1);
            var tW = m_mappingFunction(worldPosition.X - 1, worldPosition.Y + 1, worldPosition.Z);
            var tM = m_mappingFunction(worldPosition.X, worldPosition.Y + 1, worldPosition.Z);
            var tE = m_mappingFunction(worldPosition.X + 1, worldPosition.Y + 1, worldPosition.Z);
            var tSw = m_mappingFunction(worldPosition.X - 1, worldPosition.Y + 1, worldPosition.Z - 1);
            var tS = m_mappingFunction(worldPosition.X, worldPosition.Y + 1, worldPosition.Z - 1);
            var tSe = m_mappingFunction(worldPosition.X + 1, worldPosition.Y + 1, worldPosition.Z - 1);

            var mNw = m_mappingFunction(worldPosition.X - 1, worldPosition.Y, worldPosition.Z + 1);
            var mN = m_mappingFunction(worldPosition.X, worldPosition.Y, worldPosition.Z + 1);
            var mNe = m_mappingFunction(worldPosition.X + 1, worldPosition.Y, worldPosition.Z + 1);
            var mW = m_mappingFunction(worldPosition.X - 1, worldPosition.Y, worldPosition.Z);
            var mE = m_mappingFunction(worldPosition.X + 1, worldPosition.Y, worldPosition.Z);
            var mSw = m_mappingFunction(worldPosition.X - 1, worldPosition.Y, worldPosition.Z - 1);
            var mS = m_mappingFunction(worldPosition.X, worldPosition.Y, worldPosition.Z - 1);
            var mSe = m_mappingFunction(worldPosition.X + 1, worldPosition.Y, worldPosition.Z - 1);

            var bNw = m_mappingFunction(worldPosition.X - 1, worldPosition.Y - 1, worldPosition.Z + 1);
            var bN = m_mappingFunction(worldPosition.X, worldPosition.Y - 1, worldPosition.Z + 1);
            var bNe = m_mappingFunction(worldPosition.X + 1, worldPosition.Y - 1, worldPosition.Z + 1);
            var bW = m_mappingFunction(worldPosition.X - 1, worldPosition.Y - 1, worldPosition.Z);
            var bM = m_mappingFunction(worldPosition.X, worldPosition.Y - 1, worldPosition.Z);
            var bE = m_mappingFunction(worldPosition.X + 1, worldPosition.Y - 1, worldPosition.Z);
            var bSw = m_mappingFunction(worldPosition.X - 1, worldPosition.Y - 1, worldPosition.Z - 1);
            var bS = m_mappingFunction(worldPosition.X, worldPosition.Y - 1, worldPosition.Z - 1);
            var bSe = m_mappingFunction(worldPosition.X + 1, worldPosition.Y - 1, worldPosition.Z - 1);

            // -X face.
            if (!m_blockSource[mW].Exists || m_blockSource[mW].Opacity < 1)
            {
                CalculateVertexLight(vertexBuilderTarget, ref worldPosition, blockIndex, FaceDirection.XDecreasing, ref m_blockSource[mW], ref m_blockSource[mNw], ref m_blockSource[tNw], ref m_blockSource[tW], ref m_blockSource[tSw], ref m_blockSource[mSw], ref m_blockSource[bSw], ref m_blockSource[bW], ref m_blockSource[bNw]);
            }
            // +X face.
            if (!m_blockSource[mE].Exists || m_blockSource[mE].Opacity < 1)
            {
                CalculateVertexLight(vertexBuilderTarget, ref worldPosition, blockIndex, FaceDirection.XIncreasing, ref m_blockSource[mE], ref m_blockSource[mSe], ref m_blockSource[tSe], ref m_blockSource[tE], ref m_blockSource[tNe], ref m_blockSource[mNe], ref m_blockSource[bNe], ref m_blockSource[bE], ref m_blockSource[bSe]);
            }
            // -Y face.
            if (!m_blockSource[bM].Exists || m_blockSource[bM].Opacity < 1)
            {
                CalculateVertexLight(vertexBuilderTarget, ref worldPosition, blockIndex, FaceDirection.YDecreasing, ref m_blockSource[bM], ref m_blockSource[bE], ref m_blockSource[bNe], ref m_blockSource[bN], ref m_blockSource[bNw], ref m_blockSource[bW], ref m_blockSource[bSw], ref m_blockSource[bS], ref m_blockSource[bSe]);
            }
            // +Y face.
            if (!m_blockSource[tM].Exists || m_blockSource[tM].Opacity < 1)
            {
                CalculateVertexLight(vertexBuilderTarget, ref worldPosition, blockIndex, FaceDirection.YIncreasing, ref m_blockSource[tM], ref m_blockSource[tW], ref m_blockSource[tNw], ref m_blockSource[tN], ref m_blockSource[tNe], ref m_blockSource[tE], ref m_blockSource[tSe], ref m_blockSource[tS], ref m_blockSource[tSw]);
            }
            // -Z face.
            if (!m_blockSource[mS].Exists || m_blockSource[mS].Opacity < 1)
            {
                CalculateVertexLight(vertexBuilderTarget, ref worldPosition, blockIndex, FaceDirection.ZDecreasing, ref m_blockSource[mS], ref m_blockSource[mSw], ref m_blockSource[tSw], ref m_blockSource[tS], ref m_blockSource[tSe], ref m_blockSource[mSe], ref m_blockSource[bSe], ref m_blockSource[bS], ref m_blockSource[bSw]);
            }
            // +Z face.
            if (!m_blockSource[mN].Exists || m_blockSource[mN].Opacity < 1)
            {
                CalculateVertexLight(vertexBuilderTarget, ref worldPosition, blockIndex, FaceDirection.ZIncreasing, ref m_blockSource[mN], ref m_blockSource[mNe], ref m_blockSource[tNe], ref m_blockSource[tN], ref m_blockSource[tNw], ref m_blockSource[mNw], ref m_blockSource[bNw], ref m_blockSource[bN], ref m_blockSource[bNe]);
            }
        }


        private void CalculateVertexLight(VertexBuilderTarget vertexBuilderTarget, ref Vector3Int worldPosition, int blockIndex, FaceDirection faceDir, 
                                          ref T block0, ref T block1, ref T block2, ref T block3, ref T block4, ref T block5, ref T block6, ref T block7, ref T block8)
        {
            
            var sunTl = (block1.LightSun + block2.LightSun + block3.LightSun + block0.LightSun) >> 2;
            var sunTr = (block3.LightSun + block4.LightSun + block5.LightSun + block0.LightSun) >> 2;
            var sunBr = (block5.LightSun + block6.LightSun + block7.LightSun + block0.LightSun) >> 2;
            var sunBl = (block7.LightSun + block8.LightSun + block1.LightSun + block0.LightSun) >> 2;

            var redTl = (block1.LightRed + block2.LightRed + block3.LightRed + block0.LightRed) >> 2;
            var redTr = (block3.LightRed + block4.LightRed + block5.LightRed + block0.LightRed) >> 2;
            var redBr = (block5.LightRed + block6.LightRed + block7.LightRed + block0.LightRed) >> 2;
            var redBl = (block7.LightRed + block8.LightRed + block1.LightRed + block0.LightRed) >> 2;

            var greenTl = (block1.LightGreen + block2.LightGreen + block3.LightGreen + block0.LightGreen) >> 2;
            var greenTr = (block3.LightGreen + block4.LightGreen + block5.LightGreen + block0.LightGreen) >> 2;
            var greenBr = (block5.LightGreen + block6.LightGreen + block7.LightGreen + block0.LightGreen) >> 2;
            var greenBl = (block7.LightGreen + block8.LightGreen + block1.LightGreen + block0.LightGreen) >> 2;

            var blueTl = (block1.LightBlue + block2.LightBlue + block3.LightBlue + block0.LightBlue) >> 2;
            var blueTr = (block3.LightBlue + block4.LightBlue + block5.LightBlue + block0.LightBlue) >> 2;
            var blueBr = (block5.LightBlue + block6.LightBlue + block7.LightBlue + block0.LightBlue) >> 2;
            var blueBl = (block7.LightBlue + block8.LightBlue + block1.LightBlue + block0.LightBlue) >> 2;

            var flipped = (sunTl + redTl + greenTl + blueTl) + (sunBr + redBr + greenBr + blueBr) <
                           (sunTr + redTr + greenTr + blueTr) + (sunBl + redBl + greenBl + blueBl);

            var localTl = new HalfVector4(sunTl / 255.0f, redTl / 255.0f, greenTl / 255.0f, blueTl / 255.0f);
            var localTr = new HalfVector4(sunTr / 255.0f, redTr / 255.0f, greenTr / 255.0f, blueTr / 255.0f);
            var localBl = new HalfVector4(sunBl / 255.0f, redBl / 255.0f, greenBl / 255.0f, blueBl / 255.0f);
            var localBr = new HalfVector4(sunBr / 255.0f, redBr / 255.0f, greenBr / 255.0f, blueBr / 255.0f);

            BuildFaceVertices(vertexBuilderTarget, ref worldPosition, blockIndex, faceDir, flipped, ref localTl, ref localTr, ref localBl, ref localBr);
        }

        private void BuildFaceVertices(VertexBuilderTarget vertexBuilderTarget, ref Vector3Int position, int blockIndex, FaceDirection faceDir, bool flipped,
                                                ref HalfVector4 localLightTl, ref HalfVector4 localLightTr, ref HalfVector4 localLightBl, ref HalfVector4 localLightBr)
        {
            var textureUvMappings = m_blockSource[blockIndex].GetTextureMapping(faceDir);
            Vector3 vertexTl, vertexTr, vertexBr, vertexBl;

            switch (faceDir)
            {
                case FaceDirection.XIncreasing:
                    vertexTl = new Vector3(position.X + 1, position.Y + 1, position.Z);
                    vertexTr = new Vector3(position.X + 1, position.Y + 1, position.Z + 1);
                    vertexBr = new Vector3(position.X + 1, position.Y, position.Z + 1);
                    vertexBl = new Vector3(position.X + 1, position.Y, position.Z);
                    break;
                case FaceDirection.XDecreasing:
                    vertexTl = new Vector3(position.X, position.Y + 1, position.Z + 1);
                    vertexTr = new Vector3(position.X, position.Y + 1, position.Z);
                    vertexBr = new Vector3(position.X, position.Y, position.Z);
                    vertexBl = new Vector3(position.X, position.Y, position.Z + 1);
                    break;
                case FaceDirection.YIncreasing:
                    vertexTl = new Vector3(position.X, position.Y + 1, position.Z + 1);
                    vertexTr = new Vector3(position.X + 1, position.Y + 1, position.Z + 1);
                    vertexBr = new Vector3(position.X + 1, position.Y + 1, position.Z);
                    vertexBl = new Vector3(position.X, position.Y + 1, position.Z);
                    break;
                case FaceDirection.YDecreasing:
                    vertexTl = new Vector3(position.X + 1, position.Y, position.Z + 1);
                    vertexTr = new Vector3(position.X, position.Y, position.Z + 1);
                    vertexBr = new Vector3(position.X, position.Y, position.Z);
                    vertexBl = new Vector3(position.X + 1, position.Y, position.Z);
                    break;
                case FaceDirection.ZIncreasing:
                    vertexTl = new Vector3(position.X + 1, position.Y + 1, position.Z + 1);
                    vertexTr = new Vector3(position.X, position.Y + 1, position.Z + 1);
                    vertexBr = new Vector3(position.X, position.Y, position.Z + 1);
                    vertexBl = new Vector3(position.X + 1, position.Y, position.Z + 1);
                    break;
                default:
                    vertexTl = new Vector3(position.X, position.Y + 1, position.Z);
                    vertexTr = new Vector3(position.X + 1, position.Y + 1, position.Z);
                    vertexBr = new Vector3(position.X + 1, position.Y, position.Z);
                    vertexBl = new Vector3(position.X, position.Y, position.Z);
                    break;
            }
            if (m_blockScale != 1)
            {
                vertexTl = (vertexTl - m_origin) * m_blockScale + m_origin;
                vertexTr = (vertexTr - m_origin) * m_blockScale + m_origin;
                vertexBr = (vertexBr - m_origin) * m_blockScale + m_origin;
                vertexBl = (vertexBl - m_origin) * m_blockScale + m_origin;
            }

            vertexBuilderTarget.VertexList.Add(new BlockVertex(vertexTl, textureUvMappings[0], localLightTl));
            vertexBuilderTarget.VertexList.Add(new BlockVertex(vertexTr, textureUvMappings[1], localLightTr));
            vertexBuilderTarget.VertexList.Add(new BlockVertex(vertexBr, textureUvMappings[3], localLightBr));
            vertexBuilderTarget.VertexList.Add(new BlockVertex(vertexBl, textureUvMappings[2], localLightBl));
            AddIndex(vertexBuilderTarget, flipped);
        }

        private static void AddIndex(VertexBuilderTarget vertexBuilderTarget, bool flipped)
        {
            if (flipped)
            {
                vertexBuilderTarget.IndexList.Add((int)(vertexBuilderTarget.Index + 2));
                vertexBuilderTarget.IndexList.Add((int)(vertexBuilderTarget.Index + 1));
                vertexBuilderTarget.IndexList.Add(vertexBuilderTarget.Index);
                vertexBuilderTarget.IndexList.Add((int)(vertexBuilderTarget.Index + 3));
                vertexBuilderTarget.IndexList.Add((int)(vertexBuilderTarget.Index + 2));
                vertexBuilderTarget.IndexList.Add(vertexBuilderTarget.Index);
            }
            else
            {
                vertexBuilderTarget.IndexList.Add((int)(vertexBuilderTarget.Index + 3));
                vertexBuilderTarget.IndexList.Add((int)(vertexBuilderTarget.Index + 2));
                vertexBuilderTarget.IndexList.Add((int)(vertexBuilderTarget.Index + 1));
                vertexBuilderTarget.IndexList.Add((int)(vertexBuilderTarget.Index + 3));
                vertexBuilderTarget.IndexList.Add((int)(vertexBuilderTarget.Index + 1));
                vertexBuilderTarget.IndexList.Add(vertexBuilderTarget.Index);
            }
            vertexBuilderTarget.Index += 4;
        }
    }
}