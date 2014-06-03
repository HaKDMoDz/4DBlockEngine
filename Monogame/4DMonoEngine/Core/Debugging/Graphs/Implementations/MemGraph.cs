

using System.Linq;
using _4DMonoEngine.Core.Common.Extensions;
using _4DMonoEngine.Core.Graphics.Drawing;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Debugging.Graphs.Implementations
{
    public class MemGraph:DebugGraph
    {
        // required services
        private IStatistics _statistics;

        public MemGraph(Game game, Rectangle bounds)
            : base(game, bounds)
        {
        }

        protected override void Initialize()
        {
            // import required services.
            _statistics = (IStatistics)Game.Services.GetService(typeof(IStatistics));
        }

        public override void Update(GameTime gameTime)
        {

            GraphValues.Add(_statistics.MemoryUsed);

            if (GraphValues.Count > ValuesToGraph + 1)
                GraphValues.RemoveAt(0);

            // we must have at least 2 values to start rendering
            if (GraphValues.Count <= 2)
                return;

            MaxValue = (int)GraphValues.Max();
            AverageValue = (int)GraphValues.Average();
            MinimumValue = (int)GraphValues.Min();
            CurrentValue = (int)_statistics.MemoryUsed;

            if (!AdaptiveLimits)
                return;

            AdaptiveMaximum = MaxValue;
            AdaptiveMinimum = 0;
        }

        public override void DrawStrings(GameTime gameTime)
        {
            SpriteBatch.DrawString(SpriteFont, "mem:" + CurrentValue.GetKiloString(), new Vector2(Bounds.Left, Bounds.Bottom), Color.White);
            //SpriteBatch.DrawString(SpriteFont, "max:" + MaxValue.GetKiloString(), new Vector2(Bounds.Left + 90, Bounds.Bottom), Color.White);
            //SpriteBatch.DrawString(SpriteFont, "avg:" + AverageValue.GetKiloString(), new Vector2(Bounds.Left + 150, Bounds.Bottom), Color.White);
            //SpriteBatch.DrawString(SpriteFont, "min:" + MinimumValue.GetKiloString(), new Vector2(Bounds.Left + 210, Bounds.Bottom), Color.White);
        }

        public override void DrawGraph(GameTime gameTime)
        {
            BasicShapes.DrawSolidPolygon(PrimitiveBatch, BackgroundPolygon, 4, Color.Black, true);

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
                var y1 = Bounds.Bottom - ((GraphValues[i] / (AdaptiveMaximum - AdaptiveMinimum)) * yScale);
                var y2 = Bounds.Bottom - ((GraphValues[i - 1] / (AdaptiveMaximum - AdaptiveMinimum)) * yScale);

                var x1 = new Vector2(MathHelper.Clamp(x, Bounds.Left, Bounds.Right), MathHelper.Clamp(y1, Bounds.Top, Bounds.Bottom));

                var x2 = new Vector2(MathHelper.Clamp(x + deltaX, Bounds.Left, Bounds.Right), MathHelper.Clamp(y2, Bounds.Top, Bounds.Bottom));

                BasicShapes.DrawSegment(PrimitiveBatch,x1, x2, Color.DeepSkyBlue);

                x += deltaX;
            }
        }
    }
}
