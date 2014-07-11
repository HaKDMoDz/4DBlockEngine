using System;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Chunks;

namespace _4DMonoEngine.Core.Debugging.Profiling
{
    //This class doesn't really draw, but it needs to get called in the draw loop to calculate the framerate
    class Statistics : DrawableGameComponent
    {
        public Statistics(Game game, ChunkCache chunkCache) : base(game)
        {
            m_chunkCache = chunkCache;
            game.Services.AddService(typeof(Statistics), this);
        }

        public int Fps { get; private set; }

        public long MemoryUsed
        {
            get
            {
                return GC.GetTotalMemory(false);
            }
        }
        public int GenerateQueue 
        {
            get
            {
                return m_chunkCache.StateStatistics[ChunkState.AwaitingGenerate] + m_chunkCache.StateStatistics[ChunkState.Generating];
            }
        }
        public int LightenQueue
        {
            get
            {
                return m_chunkCache.StateStatistics[ChunkState.AwaitingLighting] + m_chunkCache.StateStatistics[ChunkState.Lighting];
            }
        }
        public int BuildQueue
        {
            get
            {
                return m_chunkCache.StateStatistics[ChunkState.AwaitingBuild] + m_chunkCache.StateStatistics[ChunkState.Building];
            }
        }
        public int ReadyQueue
        {
            get
            {
                return m_chunkCache.StateStatistics[ChunkState.Ready];
            }
        }
        public int RemovalQueue
        {
            get
            {
                return m_chunkCache.StateStatistics[ChunkState.AwaitingRemoval];
            }
        }

        private readonly ChunkCache m_chunkCache;
        private int m_frameCounter = 0; // the frame count.
        private TimeSpan m_elapsedTime = TimeSpan.Zero;
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        public override void Update(GameTime gameTime)
        {
            
        }

        public override void Draw(GameTime gameTime)
        {
            ++m_frameCounter;
            m_elapsedTime += gameTime.ElapsedGameTime;
            if (m_elapsedTime > OneSecond)
            {
                m_elapsedTime = TimeSpan.Zero;
                Fps = m_frameCounter;
                m_frameCounter = 0;
            }
        }


    }
}
