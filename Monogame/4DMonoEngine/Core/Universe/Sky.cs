using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using _4DMonoEngine.Core.Chunks.Processors;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Logging;
using _4DMonoEngine.Core.Common.Noise;
using _4DMonoEngine.Core.Graphics;

namespace _4DMonoEngine.Core.Universe
{
    public class Sky : WorldRenderable
    {
        private const int Size = 150;
        private readonly bool[] m_clouds;
        private readonly SimplexNoise m_noise;
        private readonly VertexBuilder m_vertexBuilder;


        public float CloudSpeed { get; set; }

        private bool m_meshBuilt;

        private Effect m_blockEffect; // block effect.
        private Texture2D m_blockTextureAtlas; // block texture atlas

        public Sky(ulong seed)
        {
            m_noise = new SimplexNoise(seed);
            m_clouds  = new bool[Size * Size];
            m_vertexBuilder = new VertexBuilder();

            CloudSpeed = 0.1f;
        }

        public override void LoadContent()
        {
            m_blockEffect = _assetManager.BlockEffect;
            m_blockTextureAtlas = _assetManager.BlockTextureAtlas;
        }

        public override void Update(GameTime gameTime)
        {
            for (var x = 0; x < Size; x++)
            {
                for (var z = 0; z < Size; z++)
                {
                    m_clouds[x * Size + z] = m_noise.Perlin3DFMB(x, m_getTimeOfDay() * CloudSpeed, z, Size, 0, 3) > 0;
                }
            }
            m_vertexBuilder.Build(this);
        }

        public override void Draw(GameTime gameTime)
        {
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

            foreach (var pass in m_blockEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                if (IndexBuffer == null || VertexBuffer == null)
                    continue;

                if (VertexBuffer.VertexCount == 0)
                    continue;

                if (IndexBuffer.IndexCount == 0)
                    continue;

                Game.GraphicsDevice.SetVertexBuffer(VertexBuffer);
                Game.GraphicsDevice.Indices = IndexBuffer;
                Game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, VertexBuffer.VertexCount, 0, IndexBuffer.IndexCount / 3);
            }
        }
    }
}
