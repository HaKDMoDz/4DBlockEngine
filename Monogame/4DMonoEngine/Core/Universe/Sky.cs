using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.AbstractClasses;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Graphics;
using _4DMonoEngine.Core.Processors;
using _4DMonoEngine.Core.Utils;
using _4DMonoEngine.Core.Utils.Noise;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Universe
{
    public class CloudBlock : ILightable
    {
        private readonly ushort m_type;
        private CloudBlock(ushort type, byte light)
        {
            m_type = type;
            LightSun = light;
        }
        public byte LightSun { get; set; }
        public byte LightRed { get; set; }
        public byte LightGreen { get; set; }
        public byte LightBlue { get; set; }
        public float Opacity
        {
            get { return BlockDictionary.GetInstance().GetStaticData(m_type).Opacity; }
        }
        public HalfVector2[] GetTextureMapping(FaceDirection faceDir)
        {
            return BlockDictionary.GetInstance().GetTextureMapping(m_type, faceDir);
        }

        public bool Exists
        {
            get { return m_type != 0; }
        }

        public static readonly CloudBlock Cloud = new CloudBlock(BlockDictionary.GetInstance().GetBlockIdForName("Cloud"), 0);
        public static readonly CloudBlock Empty = new CloudBlock(0, 255);
    }

    public class Sky : WorldRenderable
    {
        private const int SizeXZ = 36; 
        private const int SizeY = 5;
        private readonly CloudBlock[] m_clouds;
        private readonly SimplexNoise m_noise;
        private  VertexBuilder<CloudBlock> m_vertexBuilder;
        private readonly CloudTarget m_cloudVertexTarget;
        public float CloudSpeed { get; set; }
        
        private Effect m_blockEffect; // block effect.
        private Texture2D m_blockTextureAtlas; // block texture atlas
        private float m_cloudPosition;

        public Sky(Game game, uint seed) : base(game)
        {
            m_noise = new SimplexNoise(seed);
            m_clouds = new CloudBlock[SizeXZ * SizeY * SizeXZ];
            m_cloudVertexTarget = new CloudTarget(new Vector3Int(0, 120, 0), SizeXZ, SizeY, SizeXZ, 8);
            InitializeClouds();
            CloudSpeed = 0.1f;
            m_cloudPosition = 0;
        }
        public override void LoadContent()
        {
            m_blockEffect = MainEngine.GetEngineInstance().GetAsset<Effect>("BlockEffect");
            m_blockTextureAtlas = MainEngine.GetEngineInstance().GetAsset<Texture2D>("BlockTextureAtlas");
        }

        public override void Initialize(GraphicsDevice graphicsDevice, Camera camera, GetTimeOfDay getTimeOfDay, GetFogVector getFogVector)
        {
            base.Initialize(graphicsDevice, camera, getTimeOfDay, getFogVector);
            m_vertexBuilder = new VertexBuilder<CloudBlock>(m_clouds, CloudIndexByWorldPosition, m_graphicsDevice, 8);
            Task.Run(() =>
            {
                while (true)
                {
                    StepClouds();
                    m_vertexBuilder.Build(m_cloudVertexTarget);
                    Thread.Sleep(1000);
                }
            });
        }

        private int CloudIndexByWorldPosition(int x, int y, int z)
        {
            return CloudIndexByRelativePosition(x - m_cloudVertexTarget.Position.X, y - m_cloudVertexTarget.Position.Y, z - m_cloudVertexTarget.Position.Z);
        }

        private static int CloudIndexByRelativePosition(int x, int y, int z)
        {
            var flattenIndex = x * SizeXZ * SizeY + z * SizeY + y;
            return flattenIndex;
        }

        private bool registered = false;
        public override void Update(GameTime gameTime)
        {
            if (MainEngine.GetEngineInstance().DebugOnlyDebugManager != null && !registered)
            {
                registered = true;
                MainEngine.GetEngineInstance().DebugOnlyDebugManager.RegisterInGameDebuggable(m_cloudVertexTarget);
            }
        }

        private void InitializeClouds()
        {
            for (var x = 0; x < SizeXZ; x++) 
            {
                for (var y = 0; y < SizeY; y++)
                {
                    for (var z = 0; z < SizeXZ; z++)
                    {
                        m_clouds[CloudIndexByRelativePosition(x, y, z)] = CloudBlock.Empty;
                    }
                }
            }
        }

        private void StepClouds()
        {
            m_cloudPosition += CloudSpeed;
            for (var x = 1; x < SizeXZ - 1; x++)
            {
                for (var y = 1; y < SizeY - 1; y++)
                {
                    var offset = -Math.Abs(2 - y) * 0.25f;
                    for (var z = 1; z < SizeXZ - 1; z++)
                    {
                        m_clouds[CloudIndexByRelativePosition(x, y, z)] = 
                            m_noise.Perlin4Dfbm(x + m_cloudVertexTarget.Position.X,
                                y + m_cloudVertexTarget.Position.Y, z + m_cloudVertexTarget.Position.Z, m_cloudPosition,
                                16, offset, 3) > 0
                                ? CloudBlock.Cloud
                                : CloudBlock.Empty;
                    }
                }
            }
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

                if (!m_cloudVertexTarget.RenderingBoundingBox.Intersects(viewFrustrum))
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
