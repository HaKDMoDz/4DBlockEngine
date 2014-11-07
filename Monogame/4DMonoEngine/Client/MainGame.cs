using _4DMonoEngine.Core;
using _4DMonoEngine.Core.Debugging.Timing;
using _4DMonoEngine.Core.Input;
using _4DMonoEngine.Core.UI;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Client
{
    public class MainGame : Game
    {
//TODO : consolidate DEBUG confitionals
#if DEBUG
        private readonly TimeRuler m_timeRuler;
#endif
        public MainGame()
        {
            Content.RootDirectory = "Content"; // set content root directory
            var graphicsDeviceManager = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferHeight = 720,
                PreferredBackBufferWidth = 1440
            };
#if DEBUG
            m_timeRuler = new TimeRuler(this)
            {
                Visible = true, 
                ShowLog = true
            };
#endif
        }

       protected override void Initialize()
        {
            IsMouseVisible = false;
            var inputManager = new InputManager(this);
            Components.Add(new UserInterface(this));
            Components.Add(inputManager);
            MainEngine.GetEngineInstance().Initialize(this, inputManager, 0);
#if DEBUG
            Components.Add(m_timeRuler); 
#endif
            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {

#if DEBUG
            m_timeRuler.StartFrame();
            m_timeRuler.BeginMark("Update", Color.Blue);
#endif
            base.Update(gameTime);
#if DEBUG
            m_timeRuler.EndMark("Update");
#endif
        }

        protected override void Draw(GameTime gameTime)
        {
#if DEBUG
            m_timeRuler.BeginMark("Draw", Color.Yellow); 
#endif
			var skyColor = new Color(128, 173, 254);
            GraphicsDevice.Clear(skyColor);
            base.Draw(gameTime);
#if DEBUG
            m_timeRuler.EndMark("Draw");
#endif
        }
            

        protected override void UnloadContent()
        {
        }
    }
}
