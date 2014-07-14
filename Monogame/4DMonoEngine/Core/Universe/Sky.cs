using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using _4DMonoEngine.Core.AbstractClasses;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Enums;
using _4DMonoEngine.Core.Interfaces;
using _4DMonoEngine.Core.Noise;
using _4DMonoEngine.Core.Structs.Vector;
using _4DMonoEngine.Core.Graphics;
using _4DMonoEngine.Core.Processors;

namespace _4DMonoEngine.Core.Universe
{
    public class CloudBlock : ILightable
    {
        private readonly ushort m_type;
        private CloudBlock(ushort type)
        {
            m_type = type;
        }
        public byte LightSun { get; set; }
        public byte LightRed { get; set; }
        public byte LightGreen { get; set; }
        public byte LightBlue { get; set; }
        public float Opacity { get; private set; }
        public HalfVector2[] GetTextureMapping(FaceDirection faceDir)
        {
            return BlockDictionary.GetInstance().GetTextureMapping(m_type, faceDir);
        }

        public bool Exists
        {
            get { return m_type != 0; }
        }

        public static readonly CloudBlock Cloud = new CloudBlock(BlockDictionary.GetInstance().GetBlockIdForName("Cloud"));
        public static readonly CloudBlock Empty = new CloudBlock(0);
    }

    public class Sky : WorldRenderable
    {
        private const int Size = 150;
        private readonly CloudBlock[] m_clouds;
        private readonly SimplexNoise m_noise;
        private  VertexBuilder<CloudBlock> m_vertexBuilder;
        private readonly CloudTarget m_cloudVertexTarget;
        public float CloudSpeed { get; set; }

        //private bool m_meshBuilt;

        private Effect m_blockEffect; // block effect.
        private Texture2D m_blockTextureAtlas; // block texture atlas

        public Sky(uint seed)
        {
            m_noise = new SimplexNoise(seed);
            m_clouds = new CloudBlock[Size * Size];
            m_cloudVertexTarget = new CloudTarget(new Vector3Int(), Size, 1, Size);

            CloudSpeed = 0.1f;
        }
        public override void LoadContent()
        {
            m_blockEffect = MainEngine.GetEngineInstance().GetAsset<Effect>("BlockEffect");
            m_blockTextureAtlas = MainEngine.GetEngineInstance().GetAsset<Texture2D>("BlockTextureAtlas");
        }

        public override void Initialize(GraphicsDevice graphicsDevice, Camera camera, GetTimeOfDay getTimeOfDay, GetFogVector getFogVector)
        {
            base.Initialize(graphicsDevice, camera, getTimeOfDay, getFogVector);
            m_vertexBuilder = new VertexBuilder<CloudBlock>(m_clouds, CloudIndexByWorldPosition, m_graphicsDevice);

        }

        private int CloudIndexByWorldPosition(int x, int y, int z)
        {
            return x * Size + z;
        }


        public override void Update(GameTime gameTime)
        {
            if (m_vertexBuilder == null)
            {
                return;
            }
            for (var x = 0; x < Size; x++)
            {
                for (var z = 0; z < Size; z++)
                {
                    m_clouds[x * Size + z] = m_noise.Perlin3Dfmb(x, m_getTimeOfDay() * CloudSpeed, z, Size, 0, 3) > 0 ? CloudBlock.Cloud : CloudBlock.Empty;
                }
            }
            m_vertexBuilder.Build(m_cloudVertexTarget);
        }

        public override void Draw(GameTime gameTime)
        {
            var viewFrustrum = new BoundingFrustum(m_camera.View * m_camera.Projection);
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

            foreach (var pass in m_blockEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                if (m_cloudVertexTarget.IndexBuffer == null || m_cloudVertexTarget.VertexBuffer == null)
                {
                    continue;
                }

                if (m_cloudVertexTarget.VertexBuffer.VertexCount == 0)
                {
                    continue;
                }

                if (m_cloudVertexTarget.IndexBuffer.IndexCount == 0)
                {
                    continue;
                }

                /*if (!IsInRange(m_vertexBuilder))
                {
                    continue;
                }*/

                if (!m_cloudVertexTarget.BoundingBox.Intersects(viewFrustrum))
                {
                    continue;
                }

                Game.GraphicsDevice.SetVertexBuffer(m_cloudVertexTarget.VertexBuffer);
                Game.GraphicsDevice.Indices = m_cloudVertexTarget.IndexBuffer;
                Game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, m_cloudVertexTarget.VertexBuffer.VertexCount, 0, m_cloudVertexTarget.IndexBuffer.IndexCount / 3);
            }
        }
    }
}
