using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Blocks;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Common.AbstractClasses;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Universe
{
    public class Simulation : DrawableGameComponent
    {
        private const float GameHourInRealSeconds = 120;

        internal Player Player
        {
            get { return m_player; }
        }

        internal ChunkCache ChunkCache
        {
            get { return m_chunkCache; }
        }

        private readonly ChunkCache m_chunkCache;
        private readonly Player m_player;
        private readonly List<Renderable> m_worldComponents;
        private Vector2 m_fogVector;

        public Simulation(Game game, uint seed)
            : base(game)
        {
            m_fogVector = new Vector2(Chunk.SizeInBlocks * (ChunkCache.ViewRange - 2), Chunk.SizeInBlocks * (ChunkCache.ViewRange));
            m_worldComponents = new List<Renderable>();
            m_chunkCache = new ChunkCache(game.GraphicsDevice, seed);
            m_worldComponents.Add(m_chunkCache);
            m_worldComponents.Add(new Sky(seed));
            m_player = new Player(ChunkCache.Blocks);
            m_worldComponents.Add(m_player);
        }

        public float GetTimeOfDay()
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

        public void SetFogVector(float x, float y)
        {
            m_fogVector.X = x;
            m_fogVector.Y = y;
        }

        public override void Initialize()
        {
            var camera = MainEngine.GetEngineInstance().Camera;
            m_player.SpawnPlayer(new Vector2Int(0, 0));
            foreach (var renderable in m_worldComponents)
            {
                if (renderable is WorldRenderable)
                {
                    (renderable as WorldRenderable).Initialize(GraphicsDevice, camera, GetTimeOfDay, GetFogVector);
                }
                renderable.LoadContent();
            }
            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            foreach (var renderable in m_worldComponents)
            {
                renderable.Update(gameTime);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            foreach (var renderable in m_worldComponents)
            {
                renderable.Draw(gameTime);
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
    }
}