using System;
using System.Diagnostics;
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
using _4DMonoEngine.Core.Utils.Noise;
using _4DMonoEngine.Core.Utils.Vector;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;

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

        public Color GetTint()
        {
            return Color.White;
        }

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
        public static readonly CloudBlock UndersideClear = new CloudBlock(0, 200);
        public static readonly CloudBlock UndersideOvercast = new CloudBlock(0, 140);
        public static readonly CloudBlock UndersideThreatening = new CloudBlock(0, 100);
        public static readonly CloudBlock UndersideStormy = new CloudBlock(0, 65);
    }

    public class Sky : WorldRenderable
    {
        private const int SizeXz = 20;
        private const int Scale = 1;
        private const int SizeY = 7;
        private const int ChunksXz = 10;
        private const int ArraySizeXz = ChunksXz * SizeXz;
        private const int OffsetXz = -ArraySizeXz * Scale / 2;
        private const int OffsetY = 120;

        private readonly CloudBlock[] m_clouds;
        private readonly SimplexNoise4D m_noise;
        private  VertexBuilder<CloudBlock> m_vertexBuilder;
        private readonly CloudTarget[] m_cloudVertexTargets;
        public float CloudSpeed { get; set; }
        
        private Effect m_blockEffect; // block effect.
        private Texture2D m_blockTextureAtlas; // block texture atlas
        private float m_cloudPosition;
        private float m_cloudDensity;

        public Sky(Game game, uint seed) : base(game)
        {
            m_noise = new SimplexNoise4D(seed);
            m_clouds = new CloudBlock[ArraySizeXz * SizeY * ArraySizeXz];
            m_cloudVertexTargets = new CloudTarget[ChunksXz * ChunksXz];
            for (var x = 0; x < ChunksXz; x++)
            {
                for (var z = 0; z < ChunksXz; z++)
                {
                    var offsetX = OffsetXz + x * SizeXz * Scale;
                    var offsetZ = OffsetXz + z * SizeXz * Scale;
                    m_cloudVertexTargets[x * ChunksXz + z] = new CloudTarget(new Vector3Int(offsetX, OffsetY, offsetZ), SizeXz, SizeY, SizeXz, Scale);
                }
            }

            InitializeClouds();
            CloudSpeed = 0.1f;
            m_cloudPosition = 0;
            m_cloudDensity = 0.25f;
        }
        public override void LoadContent()
        {
            m_blockEffect = MainEngine.GetEngineInstance().GetAsset<Effect>("BlockEffect");
            m_blockTextureAtlas = MainEngine.GetEngineInstance().GetAsset<Texture2D>("BlockTextureAtlas");
        }

        public override void Initialize(GraphicsDevice graphicsDevice, Camera camera, GetTimeOfDay getTimeOfDay, GetFogVector getFogVector)
        {
            base.Initialize(graphicsDevice, camera, getTimeOfDay, getFogVector);
            m_vertexBuilder = new VertexBuilder<CloudBlock>(m_clouds, CloudIndexByWorldPosition, m_graphicsDevice, Scale);
            //TODO: create general task pool (with priority) and put this in it.
            Task.Run(() =>
            {
                while (true)
                {
                    StepClouds();
                    foreach (var cloudVertexTarget in m_cloudVertexTargets)
                    {
                        m_vertexBuilder.Build(cloudVertexTarget);
                    }
                    Thread.Sleep(100);
                }
// ReSharper disable once FunctionNeverReturns
            });
        }

        public void UpdateCloudDensity(float density)
        {
            m_cloudDensity = MathHelper.Clamp(density, -0.5f, 0.5f);
        }

        private int CloudIndexByWorldPosition(int x, int y, int z)
        {
            return CloudIndexByRelativePosition((x - OffsetXz), y - OffsetY, (z - OffsetXz));
        }

        private static int CloudIndexByRelativePosition(int x, int y, int z)
        {
            var flattenIndex = x * ArraySizeXz * SizeY + z * SizeY + y;
            return flattenIndex;
        }

        public override void Update(GameTime gameTime)
        {
          
        }

        private void InitializeClouds()
        {
            for (var x = 0; x < ArraySizeXz; x++) 
            {
                for (var z = 0; z < ArraySizeXz; z++)
                {
                    for (var y = 0; y < SizeY; y++)
                    {
                        m_clouds[CloudIndexByRelativePosition(x, y, z)] = CloudBlock.Empty;
                    }
                }
            }
        }

        private float delta = 0.05f;
        private void StepClouds()
        {
            m_cloudPosition += CloudSpeed;
          //  m_cloudDensity += delta;
           // if (m_cloudDensity > 1 || m_cloudDensity < 0) delta = -delta;
            var undersideCell = CloudBlock.UndersideClear;
            if (m_cloudDensity > 0.75)
            {
                undersideCell = CloudBlock.UndersideStormy;
            }
            else if (m_cloudDensity > 0.5)
            {
                undersideCell = CloudBlock.UndersideThreatening;
            }
            else if (m_cloudDensity > 0.25)
            {
                undersideCell = CloudBlock.UndersideOvercast;
            }
           // if(m_lastCloudDensity !)
            for (var x = 1; x < ArraySizeXz - 1; x++)
            {
                for (var z = 1; z < ArraySizeXz - 1; z++)
                {
                    for (var y = SizeY - 2; y >= 1 ; y--)
                    {
                        var offset = -Math.Abs(3 - y) * 0.25f + (m_cloudDensity - .5f);
                        m_clouds[CloudIndexByRelativePosition(x, y, z)] =
                            m_noise.FractalBrownianMotion(x + OffsetXz / Scale + m_cloudPosition * 4,
                                y + OffsetY, z + OffsetXz / Scale + m_cloudPosition * 4, m_cloudPosition,
                                16, offset, 2) > 0
                                ? CloudBlock.Cloud
                                : CloudBlock.Empty;  
                    }
                }
            }
            for (var x = 1; x < ArraySizeXz - 1; x++)
            {
                for (var z = 1; z < ArraySizeXz - 1; z++)
                {
                    for (var y = 0; y < SizeY - 1; y++)
                    {
                        if (m_clouds[CloudIndexByRelativePosition(x, y, z)].Exists)
                        {
                            continue;
                        }
                        var underACloudCell = IsCellUnderCloud(x, y, z);
                        m_clouds[CloudIndexByRelativePosition(x, y, z)] = underACloudCell ? undersideCell
                            : CloudBlock.Empty;
                    }
                }
            }
        }

        private bool IsCellUnderCloud(int x, int y, int z)
        {
            return m_clouds[CloudIndexByRelativePosition(x, y + 1, z)].Exists ||
                    m_clouds[CloudIndexByRelativePosition(x + 1, y + 1, z)].Exists ||
                    m_clouds[CloudIndexByRelativePosition(x, y + 1, z + 1)].Exists ||
                    m_clouds[CloudIndexByRelativePosition(x - 1, y + 1, z)].Exists ||
                    m_clouds[CloudIndexByRelativePosition(x, y + 1, z - 1)].Exists || 
                    m_clouds[CloudIndexByRelativePosition(x + 1, y + 1, z + 1)].Exists ||
                    m_clouds[CloudIndexByRelativePosition(x - 1, y + 1, z + 1)].Exists ||
                    m_clouds[CloudIndexByRelativePosition(x - 1, y + 1, z - 1)].Exists ||
                    m_clouds[CloudIndexByRelativePosition(x + 1, y + 1, z - 1)].Exists;
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
            m_blockEffect.Parameters["SunColor"].SetValue(GetSunColor());
            m_blockEffect.Parameters["HorizonColor"].SetValue(RenderingConstants.DayHorizonColor);

            // fog parameters
            m_blockEffect.Parameters["FogNear"].SetValue(m_getFogVector().X);
            m_blockEffect.Parameters["FogFar"].SetValue(m_getFogVector().Y);

            foreach (var pass in m_blockEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                foreach (var cloudVertexTarget in m_cloudVertexTargets)
                {
                    if (cloudVertexTarget.IndexBuffer == null || cloudVertexTarget.VertexBuffer == null)
                    {
                        continue;
                    }

                    if (cloudVertexTarget.VertexBuffer.VertexCount == 0)
                    {
                        continue;
                    }

                    if (cloudVertexTarget.IndexBuffer.IndexCount == 0)
                    {
                        continue;
                    }
                    
                    if (!cloudVertexTarget.RenderingBoundingBox.Intersects(viewFrustrum))
                    {
                        continue;
                    }

                    Game.GraphicsDevice.SetVertexBuffer(cloudVertexTarget.VertexBuffer);
                    Game.GraphicsDevice.Indices = cloudVertexTarget.IndexBuffer;
                    Game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0,
                        cloudVertexTarget.VertexBuffer.VertexCount, 0, cloudVertexTarget.IndexBuffer.IndexCount/3);
                }
            }
        }
    }
}
