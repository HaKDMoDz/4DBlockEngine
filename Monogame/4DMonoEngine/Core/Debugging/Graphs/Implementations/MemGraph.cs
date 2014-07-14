using System.Linq;
using _4DMonoEngine.Core.Common.Extensions;
using _4DMonoEngine.Core.Debugging.Profiling;
using _4DMonoEngine.Core.Graphics.Drawing;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Debugging.Graphs.Implementations
{
    public class MemGraph:DebugGraph
    {
        // required services
        private Statistics m_statistics;

        public MemGraph(Game game, Rectangle bounds)
            : base(game, bounds)
        {
        }

        protected override void Initialize()
        {
            // import required services.
            m_statistics = (Statistics)Game.Services.GetService(typeof(Statistics));
        }

        public override void Update()
        {

            GraphValues.Add(m_statistics.MemoryUsed);

            if (GraphValues.Count > ValuesToGraph + 1)
                GraphValues.RemoveAt(0);

            // we must have at least 2 values to start rendering
            if (GraphValues.Count <= 2)
                return;

            m_maxValue = (int)GraphValues.Max();
            m_averageValue = (int)GraphValues.Average();
            m_minimumValue = (int)GraphValues.Min();
            m_currentValue = (int)m_statistics.MemoryUsed;

            if (!AdaptiveLimits)
                return;

            m_adaptiveMaximum = m_maxValue;
            m_adaptiveMinimum = 0;
        }

        public override void DrawStrings()
        {
            m_spriteBatch.DrawString(m_spriteFont, "mem:" + m_currentValue.GetKiloString(), new Vector2(Bounds.Left, Bounds.Bottom), Color.White);
            m_spriteBatch.DrawString(m_spriteFont, "max:" + m_maxValue.GetKiloString(), new Vector2(Bounds.Left + 90, Bounds.Bottom), Color.White);
            m_spriteBatch.DrawString(m_spriteFont, "avg:" + m_averageValue.GetKiloString(), new Vector2(Bounds.Left + 150, Bounds.Bottom), Color.White);
            m_spriteBatch.DrawString(m_spriteFont, "min:" + m_minimumValue.GetKiloString(), new Vector2(Bounds.Left + 210, Bounds.Bottom), Color.White);
        }

        public override void DrawGraph()
        {
            BasicShapes.DrawSolidPolygon(m_primitiveBatch, BackgroundPolygon, 4, Color.Black, true);

            float x = Bounds.X;
            float deltaX = Bounds.Width / (float)ValuesToGraph;
            float yScale = Bounds.Bottom - (float)Bounds.Top;

            // we must have at least 2 values to start rendering
            if (GraphValues.Count <= 2)
                return;

            // start at last value (newest value added)
            // continue until no values are left
            for (var i = GraphValues.Count - 1; i > 0; i--)
            {
                var y1 = Bounds.Bottom - ((GraphValues[i] / (m_adaptiveMaximum - m_adaptiveMinimum)) * yScale);
                var y2 = Bounds.Bottom - ((GraphValues[i - 1] / (m_adaptiveMaximum - m_adaptiveMinimum)) * yScale);

                var x1 = new Vector2(MathHelper.Clamp(x, Bounds.Left, Bounds.Right), MathHelper.Clamp(y1, Bounds.Top, Bounds.Bottom));

                var x2 = new Vector2(MathHelper.Clamp(x + deltaX, Bounds.Left, Bounds.Right), MathHelper.Clamp(y2, Bounds.Top, Bounds.Bottom));

                BasicShapes.DrawSegment(m_primitiveBatch,x1, x2, Color.DeepSkyBlue);

                x += deltaX;
            }
        }
    }
}
