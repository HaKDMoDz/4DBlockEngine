

using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Assets;
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
        public bool GraphsEnabled { get; set; }
        private PrimitiveBatch _primitiveBatch;
        private SpriteBatch _spriteBatch;
        private SpriteFont _spriteFont;
        private Matrix _localProjection;
        private Matrix _localView;

        private IAssetManager _assetManager;

        private readonly List<DebugGraph> _graphs=new List<DebugGraph>(); // the current graphs list.

        public GraphManager(Game game, bool enabled)
            : base(game)
        {
            game.Services.AddService(typeof(IGraphManager), this); // export the service.
            GraphsEnabled = enabled;
        }

        public override void Initialize()
        {
            // create the graphs modules.
            _graphs.Add(new FPSGraph(Game, new Rectangle(Core.Core.Instance.Configuration.Graphics.Width - 280, 50, 270, 35)));
            _graphs.Add(new MemGraph(Game, new Rectangle(Core.Core.Instance.Configuration.Graphics.Width - 280, 105, 270, 35)));
            _graphs.Add(new GenerateQ(Game, new Rectangle(Core.Core.Instance.Configuration.Graphics.Width - 280, 160, 270, 35)));
            _graphs.Add(new LightenQ(Game, new Rectangle(Core.Core.Instance.Configuration.Graphics.Width - 280, 215, 270, 35)));
            _graphs.Add(new BuildQ(Game, new Rectangle(Core.Core.Instance.Configuration.Graphics.Width - 280, 270, 270, 35)));
            _graphs.Add(new ReadyQ(Game, new Rectangle(Core.Core.Instance.Configuration.Graphics.Width - 280, 325, 270, 35)));
            _graphs.Add(new RemoveQ(Game, new Rectangle(Core.Core.Instance.Configuration.Graphics.Width - 280, 380, 270, 35)));

            // import required services.
            _assetManager = (IAssetManager)Game.Services.GetService(typeof(IAssetManager));
            if (_assetManager == null)
                throw new NullReferenceException("Can not find asset manager component.");

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // init the drawing related objects.
            _primitiveBatch = new PrimitiveBatch(GraphicsDevice, 1000);
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _spriteFont = _assetManager.Verdana;
            _localProjection = Matrix.CreateOrthographicOffCenter(0f, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, 0f, 0f, 1f);
            _localView = Matrix.Identity;           
            
            // attach the drawing objects to the graph modules.
            foreach (var graph in _graphs)
            {
                graph.AttachGraphics(_primitiveBatch, _spriteBatch, _spriteFont, _localProjection, _localView);
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

                _primitiveBatch.Begin(_localProjection, _localView); // initialize the primitive batch.

                foreach (var graph in _graphs)
                {
                    graph.DrawGraph(gameTime); // let the graphs draw their primitives.
                }

                _primitiveBatch.End(); // end the batch.

                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend); // initialize the sprite batch.

                foreach (var graph in _graphs)
                {
                    graph.DrawStrings(gameTime); // let the graphs draw their sprites.
                }

                _spriteBatch.End(); // end the batch.

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

                foreach (var graph in _graphs)
                {
                    graph.Update(gameTime); // let the graphs update themself.
                }

            }
            base.Update(gameTime);
        }
    }
}
