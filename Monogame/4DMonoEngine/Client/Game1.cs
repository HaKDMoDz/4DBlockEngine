using _4DMonoEngine.Core;
using _4DMonoEngine.Core.Assets;
using _4DMonoEngine.Core.Common.Logging;
using _4DMonoEngine.Core.Debugging;
using _4DMonoEngine.Core.Debugging.Console;
using _4DMonoEngine.Core.Debugging.Graphs;
using _4DMonoEngine.Core.Debugging.Ingame;
using _4DMonoEngine.Core.Debugging.Timing;
using _4DMonoEngine.Core.Graphics;
using _4DMonoEngine.Core.Input;
using _4DMonoEngine.Core.Interface;
using _4DMonoEngine.Core.Universe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Client
{
    public class MainGame : Game
    {
        private readonly GraphicsDeviceManager m_graphicsDeviceManager;

        public GraphicsManager ScreenManager { get; private set; }
        public GameConsole Console { get; private set; }
        public Rasterizer Rasterizer { get; private set; }

        private TimeRuler m_timeRuler;

        private readonly Logger m_logger;
        private const bool DebugMode = true;

        public MainGame()
        {
            Content.RootDirectory = "Content"; // set content root directory.
            m_graphicsDeviceManager = new GraphicsDeviceManager(this);
            m_logger = MainEngine.GetEngineInstance().GetLogger("Game");
        }

       protected override void Initialize()
        {
            IsMouseVisible = false;

            ScreenManager = new GraphicsManager(m_graphicsDeviceManager, this); // start the screen manager.

            m_timeRuler = new TimeRuler(this) { Visible = true, ShowLog = true };
            Components.Add(m_timeRuler); 
            Rasterizer = new Rasterizer();

            Components.Add(new InputManager(this));

            Components.Add(new AssetManager(this));

            Components.Add(new World(this, 0));

            Components.Add(new Camera(this));
            Components.Add(new UserInterface(this));

            Components.Add(new InGameDebugger(this));
            Components.Add(new DebugBar(this));
            Components.Add(new GraphManager(this, DebugMode));

            var spriteBatch = new SpriteBatch(GraphicsDevice);
            Console = new GameConsole(this, spriteBatch, new GameConsoleOptions
            {
                Font = Content.Load<SpriteFont>(@"Fonts/Verdana"),
                FontColor = Color.LawnGreen,
                Prompt = ">",
                PromptColor = Color.Crimson,
                CursorColor = Color.OrangeRed,
                BackgroundColor = Color.Black * 0.8f,
                PastCommandOutputColor = Color.Aqua,
                BufferColor = Color.Gold
            });
            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            m_timeRuler.StartFrame();
            m_timeRuler.BeginMark("Update", Color.Blue);
            base.Update(gameTime);
            m_timeRuler.EndMark("Update");
        }

        protected override void Draw(GameTime gameTime)
        {
            m_timeRuler.BeginMark("Draw", Color.Yellow); 
			var skyColor = new Color(128, 173, 254);
            GraphicsDevice.Clear(skyColor);
            GraphicsDevice.RasterizerState = Rasterizer.State;
            base.Draw(gameTime);
            m_timeRuler.EndMark("Draw");
        }
            

        protected override void UnloadContent()
        {
        }
    }
}
