using System.Collections.Generic;
using _4DMonoEngine.Core.Debugging.Graphs.Implementations;
using _4DMonoEngine.Core.Debugging.Graphs.Implementations.ChunkGraphs;
using _4DMonoEngine.Core.Graphics.Drawing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Debugging.Graphs
{
    /// <summary>
    /// GraphManager can render debug graphs.
    /// </summary>
    public interface IGraphManager
    { }

    /// <summary>
    /// GraphManager is DrawableGameComponent that can render debug graphs.
    /// </summary>
    public class GraphManager : DrawableGameComponent, IGraphManager
    {
        // stuff needed for drawing.
        private bool GraphsEnabled { get; set; }

        private PrimitiveBatch m_primitiveBatch;
        private SpriteBatch m_spriteBatch;
        private SpriteFont m_spriteFont;
        private Matrix m_localProjection;
        private Matrix m_localView;

        private readonly List<DebugGraph> m_graphs=new List<DebugGraph>(); // the current graphs list.

        public GraphManager(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IGraphManager), this); // export the service.
            GraphsEnabled = false;
        }

        public override void Initialize()
        {
            // create the graphs modules.
            var width = MainEngine.GetEngineInstance().Game.GraphicsDevice.Viewport.Width;
            m_graphs.Add(new FpsGraph(Game, new Rectangle(width - 280, 50, 270, 35)));
            m_graphs.Add(new MemGraph(Game, new Rectangle(width - 280, 105, 270, 35)));
            m_graphs.Add(new GenerateQ(Game, new Rectangle(width - 280, 160, 270, 35)));
            m_graphs.Add(new LightenQ(Game, new Rectangle(width - 280, 215, 270, 35)));
            m_graphs.Add(new BuildQ(Game, new Rectangle(width - 280, 270, 270, 35)));
            m_graphs.Add(new ReadyQ(Game, new Rectangle(width - 280, 325, 270, 35)));
            m_graphs.Add(new RemoveQ(Game, new Rectangle(width - 280, 380, 270, 35)));
            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // init the drawing related objects.
            m_primitiveBatch = new PrimitiveBatch(GraphicsDevice, 1000);
            m_spriteBatch = new SpriteBatch(GraphicsDevice);
            m_spriteFont = MainEngine.GetEngineInstance().GetAsset<SpriteFont>("Verdana");
            m_localProjection = Matrix.CreateOrthographicOffCenter(0f, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0f, 0f, 1f);
            m_localView = Matrix.Identity;           
            
            // attach the drawing objects to the graph modules.
            foreach (var graph in m_graphs)
            {
                graph.AttachGraphics(m_primitiveBatch, m_spriteBatch, m_spriteFont);
            }

            base.LoadContent();
        }

        public override void Draw(GameTime gameTime)
        {
            if (GraphsEnabled)
            {
                // backup  the raster and depth-stencil states.
                var previousRasterizerState = Game.GraphicsDevice.RasterizerState;
                var previousDepthStencilState = Game.GraphicsDevice.DepthStencilState;

                // set new states for drawing primitive shapes.
                Game.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
                Game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

                m_primitiveBatch.Begin(m_localProjection, m_localView); // initialize the primitive batch.

                foreach (var graph in m_graphs)
                {
                    graph.DrawGraph(); // let the graphs draw their primitives.
                }

                m_primitiveBatch.End(); // end the batch.

                m_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend); // initialize the sprite batch.

                foreach (var graph in m_graphs)
                {
                    graph.DrawStrings(); // let the graphs draw their sprites.
                }

                m_spriteBatch.End(); // end the batch.

                // restore old states.
                Game.GraphicsDevice.RasterizerState = previousRasterizerState;
                Game.GraphicsDevice.DepthStencilState = previousDepthStencilState;
            }
            base.Draw(gameTime);
        }

        public override void Update(GameTime gameTime)
        {
            if (GraphsEnabled)
            {
                foreach (var graph in m_graphs)
                {
                    graph.Update(); // let the graphs update themself.
                }
            }
            base.Update(gameTime);
        }
    }
}
