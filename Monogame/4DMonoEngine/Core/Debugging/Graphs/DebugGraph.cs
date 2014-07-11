using System.Collections.Generic;
using _4DMonoEngine.Core.Graphics.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Debugging.Graphs
{
    public abstract class DebugGraph
    {
        protected Game Game { get; private set; }
        protected Rectangle Bounds { get; private set; }

        protected readonly bool AdaptiveLimits;
        protected readonly int ValuesToGraph;
        protected readonly List<float> GraphValues = new List<float>();
        protected readonly Vector2[] BackgroundPolygon = new Vector2[4];
        protected int m_maxValue;
        protected int m_averageValue;
        protected int m_minimumValue;
        protected int m_currentValue;
        protected int m_adaptiveMinimum;
        protected int m_adaptiveMaximum = 1000;
        protected PrimitiveBatch m_primitiveBatch;
        protected SpriteBatch m_spriteBatch;
        protected SpriteFont m_spriteFont;

        protected DebugGraph(Game game, Rectangle bounds)            
        {
            Game = game;
            Bounds = bounds;
            AdaptiveLimits = true;
            ValuesToGraph = 2500;
        }

        public void AttachGraphics(PrimitiveBatch primitiveBatch, SpriteBatch spriteBatch, SpriteFont spriteFont)
        {
            m_primitiveBatch = primitiveBatch;
            m_spriteBatch = spriteBatch;
            m_spriteFont = spriteFont;

            Initialize();
            LoadContent();
        }

        protected abstract void Initialize();

        private void LoadContent()
        {
            // calculate the coordinates for drawing the background.
            BackgroundPolygon[0] = new Vector2(Bounds.X - 2, Bounds.Y - 2); // top left
            BackgroundPolygon[3] = new Vector2(Bounds.X + 2 + Bounds.Width, Bounds.Y - 2); // top right
            BackgroundPolygon[1] = new Vector2(Bounds.X - 2, Bounds.Y + Bounds.Height + 14); // bottom left
            BackgroundPolygon[2] = new Vector2(Bounds.X + 2 + Bounds.Width, Bounds.Y + Bounds.Height + 14); // bottom right
        }

        public abstract void Update();
        public abstract void DrawStrings();
        public abstract void DrawGraph();

    }
}
