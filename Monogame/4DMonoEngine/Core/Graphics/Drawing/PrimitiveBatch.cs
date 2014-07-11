using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Graphics.Drawing
{
    /* From FarSeer.DebugView - http://farseerphysics.codeplex.com/ */

    public class PrimitiveBatch : IDisposable
    {
        private const int DefaultBufferSize = 500;

        private readonly BasicEffect m_basicEffect; // a basic effect, which contains the shaders that we will use to draw our primitives.        

        private readonly GraphicsDevice m_device; // the device that we will issue draw calls to.        

        private bool m_hasBegun; // hasBegun is flipped to true once Begin is called, and is used to make sure users don't call End before Begin is called.

        private readonly VertexPositionColor[] m_lineVertices;
        private readonly VertexPositionColor[] m_triangleVertices;
        private int m_lineVertsCount;
        private int m_triangleVertsCount;

        private bool m_isDisposed;

        /// <summary>
        /// the constructor creates a new PrimitiveBatch and sets up all of the internals
        /// that PrimitiveBatch will need.
        /// </summary>
        public PrimitiveBatch(GraphicsDevice graphicsDevice, int bufferSize)
        {
            if (graphicsDevice == null)
                throw new ArgumentNullException("graphicsDevice");

            m_device = graphicsDevice;

            m_triangleVertices = new VertexPositionColor[bufferSize - bufferSize%3];
            m_lineVertices = new VertexPositionColor[bufferSize - bufferSize%2];

            // set up a new basic effect, and enable vertex colors.
            m_basicEffect = new BasicEffect(graphicsDevice) {VertexColorEnabled = true};
        }

        public void SetProjection(ref Matrix projection)
        {
            m_basicEffect.Projection = projection;
        }

        /// <summary>
        /// Begin is called to tell the PrimitiveBatch what kind of primitives will be
        /// drawn, and to prepare the graphics card to render those primitives.
        /// </summary>
        /// <param name="projection">The projection.</param>
        /// <param name="view">The view.</param>
        public void Begin(Matrix projection, Matrix view)
        {
            if (m_hasBegun)
                throw new InvalidOperationException("End must be called before Begin can be called again.");

            //tell our basic effect to begin.
            m_basicEffect.Projection = projection;
            m_basicEffect.View = view;
            m_basicEffect.CurrentTechnique.Passes[0].Apply();

            // flip the error checking boolean. It's now ok to call AddVertex, Flush and End.
            m_hasBegun = true;
        }

        public bool IsReady()
        {
            return m_hasBegun;
        }

        public void AddVertex(Vector2 vertex, Color color, PrimitiveType primitiveType)
        {
            if (!m_hasBegun)
                throw new InvalidOperationException("Begin() must be called before AddVertex can be called.");

            if (primitiveType == PrimitiveType.LineStrip || primitiveType == PrimitiveType.TriangleStrip)
                throw new NotSupportedException("The specified primitiveType is not supported by PrimitiveBatch.");

            if (primitiveType == PrimitiveType.TriangleList)
            {
                if (m_triangleVertsCount >= m_triangleVertices.Length)
                    FlushTriangles();

                m_triangleVertices[m_triangleVertsCount].Position = new Vector3(vertex, -0.1f);
                m_triangleVertices[m_triangleVertsCount].Color = color;
                m_triangleVertsCount++;
            }

            if (primitiveType == PrimitiveType.LineList)
            {
                if (m_lineVertsCount >= m_lineVertices.Length)
                    FlushLines();

                m_lineVertices[m_lineVertsCount].Position = new Vector3(vertex, 0f);
                m_lineVertices[m_lineVertsCount].Color = color;
                m_lineVertsCount++;
            }
        }

        /// <summary>
        /// End is called once all the primitives have been drawn using AddVertex.
        /// it will call Flush to actually submit the draw call to the graphics card, and
        /// then tell the basic effect to end.
        /// </summary>
        public void End()
        {
            if (!m_hasBegun)
                throw new InvalidOperationException("Begin must be called before End can be called.");

            // Draw whatever the user wanted us to draw
            FlushTriangles();
            FlushLines();

            m_hasBegun = false;
        }

        private void FlushTriangles()
        {
            if (!m_hasBegun)
                throw new InvalidOperationException("Begin must be called before Flush can be called.");

            if (m_triangleVertsCount >= 3)
            {
                var primitiveCount = m_triangleVertsCount/3;

                // submit the draw call to the graphics card
                m_device.SamplerStates[0] = SamplerState.AnisotropicClamp;
                m_device.DrawUserPrimitives(PrimitiveType.TriangleList, m_triangleVertices, 0, primitiveCount);
                m_triangleVertsCount -= primitiveCount*3;
            }
        }

        private void FlushLines()
        {
            if (!m_hasBegun)
                throw new InvalidOperationException("Begin must be called before Flush can be called.");

            if (m_lineVertsCount >= 2)
            {
                var primitiveCount = m_lineVertsCount/2;

                // submit the draw call to the graphics card
                m_device.SamplerStates[0] = SamplerState.AnisotropicClamp;
                m_device.DrawUserPrimitives(PrimitiveType.LineList, m_lineVertices, 0, primitiveCount);
                m_lineVertsCount -= primitiveCount*2;
            }
        }

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !m_isDisposed)
            {
                if (m_basicEffect != null)
                    m_basicEffect.Dispose();

                m_isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}