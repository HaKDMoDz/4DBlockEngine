using System.Text;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Common.Extensions;
using _4DMonoEngine.Core.Debugging.Profiling;
using _4DMonoEngine.Core.Graphics.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Debugging
{

    internal class DebugBar : DrawableGameComponent
    {
        // resources.
        private PrimitiveBatch m_primitiveBatch;
        private SpriteBatch m_spriteBatch;
        private SpriteFont m_spriteFont;
        private Matrix m_localProjection;
        private Matrix m_localView;
        private Rectangle m_bounds;
        private readonly Vector2[] m_backgroundPolygon = new Vector2[4];
        private readonly StringBuilder m_stringBuilder = new StringBuilder(512, 512);
        private readonly ChunkCache m_chunkCache;
        private Statistics m_statistics;


        public DebugBar(Game game, Statistics statistics, ChunkCache chunkCache)
            : base(game)
        {
            m_chunkCache = chunkCache;
            m_statistics = statistics;
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // load resources.
            m_primitiveBatch = new PrimitiveBatch(GraphicsDevice, 1000);
            m_localProjection = Matrix.CreateOrthographicOffCenter(0f, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0f, 0f, 1f);
            m_localView = Matrix.Identity;
            m_spriteBatch = new SpriteBatch(GraphicsDevice);
            m_spriteFont = MainEngine.GetEngineInstance().GetAsset<SpriteFont>("Verdana");

            // init bounds.
            m_bounds = new Rectangle(10, 10, Game.GraphicsDevice.Viewport.Bounds.Width - 20, 20);
            m_backgroundPolygon[0] = new Vector2(m_bounds.X - 2, m_bounds.Y - 2); // top left
            m_backgroundPolygon[1] = new Vector2(m_bounds.X - 2, m_bounds.Y + m_bounds.Height + 14); // bottom left
            m_backgroundPolygon[2] = new Vector2(m_bounds.X + 2 + m_bounds.Width, m_bounds.Y + m_bounds.Height + 14); // bottom right
            m_backgroundPolygon[3] = new Vector2(m_bounds.X + 2 + m_bounds.Width, m_bounds.Y - 2); // top right
        }
        
        /// <summary>
        /// Draws the statistics.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            // backup  the raster and depth-stencil states.
            var previousRasterizerState = Game.GraphicsDevice.RasterizerState;
            var previousDepthStencilState = Game.GraphicsDevice.DepthStencilState;

            // set new states for drawing primitive shapes.
            Game.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            Game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            m_primitiveBatch.Begin(m_localProjection, m_localView); // initialize the primitive batch.

            BasicShapes.DrawSolidPolygon(m_primitiveBatch, m_backgroundPolygon, 4, Color.Black, true);

            m_primitiveBatch.End(); // end the batch.

            // restore old states.
            Game.GraphicsDevice.RasterizerState = previousRasterizerState;
            Game.GraphicsDevice.DepthStencilState = previousDepthStencilState;
            
            m_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Attention: DO NOT use string.format as it's slower than string concat.

            // FPS
            m_stringBuilder.Length = 0;
            m_stringBuilder.Append("fps:");
            m_stringBuilder.Append(m_statistics.Fps);
            m_spriteBatch.DrawString(m_spriteFont, m_stringBuilder, new Vector2(m_bounds.X + 5, m_bounds.Y + 5), Color.White);

            // mem used
            m_stringBuilder.Length = 0;
            m_stringBuilder.Append("mem:");
            m_stringBuilder.Append(m_statistics.MemoryUsed.GetKiloString());
            m_spriteBatch.DrawString(m_spriteFont, m_stringBuilder, new Vector2(m_bounds.X + 105, m_bounds.Y + 5), Color.White);

            // chunks
            m_stringBuilder.Length = 0;
            m_stringBuilder.Append("chunks:");
            m_stringBuilder.AppendNumber(m_chunkCache.ChunksDrawn);
            m_stringBuilder.Append('/');
            m_stringBuilder.AppendNumber(m_chunkCache.ChunksLoaded);
            m_spriteBatch.DrawString(m_spriteFont, m_stringBuilder, new Vector2(m_bounds.X + 205, m_bounds.Y + 5), Color.White);

            // chunk generation queue
            m_stringBuilder.Length = 0;
            m_stringBuilder.Append("GenerateQ:");
            m_stringBuilder.AppendNumber(m_statistics.GenerateQueue);
            m_spriteBatch.DrawString(m_spriteFont, m_stringBuilder, new Vector2(m_bounds.X + 450, m_bounds.Y + 5), Color.White);

            // chunk lighting queue
            m_stringBuilder.Length = 0;
            m_stringBuilder.Append("LightenQ:");
            m_stringBuilder.AppendNumber(m_statistics.LightenQueue);
            m_spriteBatch.DrawString(m_spriteFont, m_stringBuilder, new Vector2(m_bounds.X + 550, m_bounds.Y + 5), Color.White);

            // chunk build queue
            m_stringBuilder.Length = 0;
            m_stringBuilder.Append("BuildQ:");
            m_stringBuilder.AppendNumber(m_statistics.BuildQueue);
            m_spriteBatch.DrawString(m_spriteFont, m_stringBuilder, new Vector2(m_bounds.X + 650, m_bounds.Y + 5), Color.White);

            // ready chunks queue
            m_stringBuilder.Length = 0;
            m_stringBuilder.Append("Ready:");
            m_stringBuilder.AppendNumber(m_statistics.ReadyQueue);
            m_spriteBatch.DrawString(m_spriteFont, m_stringBuilder, new Vector2(m_bounds.X + 750, m_bounds.Y + 5), Color.White);

            // chunk removal queue
            m_stringBuilder.Length = 0;
            m_stringBuilder.Append("Removal:");
            m_stringBuilder.AppendNumber(m_statistics.RemovalQueue);
            m_spriteBatch.DrawString(m_spriteFont, m_stringBuilder, new Vector2(m_bounds.X + 850, m_bounds.Y + 5), Color.White);
            
            // player position
            m_stringBuilder.Length = 0;
            m_stringBuilder.Append("pos:");
            m_stringBuilder.Append(m_chunkCache.CachePosition);
            m_spriteBatch.DrawString(m_spriteFont, m_stringBuilder, new Vector2(m_bounds.X + 305, m_bounds.Y + 15), Color.White);

            m_spriteBatch.End();
        }
    }
}
