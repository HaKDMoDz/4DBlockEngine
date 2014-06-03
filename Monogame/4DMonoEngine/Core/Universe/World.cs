using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Common.Vector;
using _4DMonoEngine.Core.Graphics;
using Microsoft.Xna.Framework;
using Block = _4DMonoEngine.Core.Common.Enums.Block;

namespace _4DMonoEngine.Core.Universe
{
    public class World : DrawableGameComponent
    {
        private const float GameHourInRealSeconds = 120;

        private readonly ChunkCache m_chunkCache;
        private readonly Player m_player;
        private readonly List<DrawableGameComponent> m_worldComponents;
        private ICameraControlService m_cameraController;
        private Vector2 m_fogVector;

        public static Vector4 NightColor = Color.Black.ToVector4();
        public static Vector4 SunColor = Color.White.ToVector4();
        public static Vector4 HorizonColor = Color.DarkGray.ToVector4();
        public static Vector4 EveningTint = Color.Red.ToVector4();
        public static Vector4 MorningTint = Color.Gold.ToVector4();

        public World(Game game, int seed)
            : base(game)
        {
            m_fogVector = new Vector2(Chunk.SizeInBlocks*(ChunkCache.ViewRange - 2),Chunk.SizeInBlocks*(ChunkCache.ViewRange));
            m_worldComponents = new List<DrawableGameComponent>();
            m_worldComponents.Add(new Sky(game));
            var blockDictionary = new BlockDictionary();
            m_chunkCache = new ChunkCache(game, TimeOfDay, blockDictionary, seed);
            m_worldComponents.Add(m_chunkCache);
            m_player = new Player(game, this);
            m_worldComponents.Add(m_player);
        }

        public float TimeOfDay()
        {
            return  ((int)(DateTime.Now.TimeOfDay.TotalSeconds / GameHourInRealSeconds) % 24) / 24.0f;
        }

        public Vector2 GetFogVector()
        {
            return m_fogVector;
        }

        public void SetFogVector(Vector2 fogVector)
        {
            m_fogVector = fogVector;
        }

        public Vector2 SetFogVector(float x, float y)
        {
            m_fogVector.X = x;
            m_fogVector.Y = y;
        }


        public override void Initialize()
        {
            // import required services.
            m_cameraController = (ICameraControlService) Game.Services.GetService(typeof (ICameraControlService));
            m_cameraController.LookAt(Vector3.Down);
            m_player.SpawnPlayer(new Vector2Int(0, 0));
            foreach (var drawableGameComponent in m_worldComponents)
            {
                drawableGameComponent.Initialize();
            }
            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            foreach (var drawableGameComponent in m_worldComponents)
            {
                drawableGameComponent.Update(gameTime);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            foreach (var drawableGameComponent in m_worldComponents)
            {
                drawableGameComponent.Draw(gameTime);
            }
        }

        public void AddBlockAt(int x, int y, int z, ref Block block)
        {
            m_chunkCache.AddBlock(x, y, z, ref block);
        }

        public void RemoveBlockAt(int x, int y, int z)
        {
            m_chunkCache.RemoveBlock(x, y, z);
        }

        public void SpawnPlayer(Vector2Int relativePosition)
        {
            
        }
    }
}