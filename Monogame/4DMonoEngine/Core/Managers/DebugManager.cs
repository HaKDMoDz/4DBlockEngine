using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Debugging;
using _4DMonoEngine.Core.Debugging.Console;
using _4DMonoEngine.Core.Debugging.Graphs;
using _4DMonoEngine.Core.Debugging.Ingame;
using _4DMonoEngine.Core.Debugging.Profiling;
using _4DMonoEngine.Core.Graphics;

namespace _4DMonoEngine.Core.Managers
{
    public class DebugManager : DrawableGameComponent
    {
        private readonly GameComponentCollection m_components;
        private readonly GraphManager m_graphs;
        private readonly InGameDebugger m_debugger;
        public DebugManager(Game game, Camera camera, ChunkCache chunkCache, SpriteFont debugFont) : base(game)
        {
            m_graphs = new GraphManager(game);
            m_debugger = new InGameDebugger(game, camera);
            var statistics = new Statistics(game, chunkCache);
            m_components = new GameComponentCollection
            {
                m_debugger,
                new DebugBar(game, statistics, chunkCache),
                statistics,
                m_graphs,
                new GameConsole(game,  new SpriteBatch(game.GraphicsDevice), new GameConsoleOptions
                {
                    Font = debugFont,
                    FontColor = Color.LawnGreen,
                    Prompt = ">",
                    PromptColor = Color.Crimson,
                    CursorColor = Color.OrangeRed,
                    BackgroundColor = Color.Black*0.8f,
                    PastCommandOutputColor = Color.Aqua,
                    BufferColor = Color.Gold
                }, m_debugger.ToggleInGameDebugger)
            };
        }

        public override void Initialize()
        {
            base.Initialize();
            foreach (var component in m_components)
            {
                component.Initialize();
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            foreach (var component in m_components.OfType<GameComponent>())
            {
                component.Update(gameTime);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            foreach (var component in m_components.OfType<DrawableGameComponent>())
            {
                component.Draw(gameTime);
            }
        }

        public bool GraphsEnabled
        {
            get { return  m_graphs.Enabled; }
            set { m_graphs.Enabled = value; }
        }

        public void RegisterInGameDebuggable(IInGameDebuggable debuggable)
        {
            m_debugger.RegisterInGameDebuggable(debuggable);
        }
    }
}
