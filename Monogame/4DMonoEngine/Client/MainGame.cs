using _4DMonoEngine.Core;
using _4DMonoEngine.Core.Debugging.Timing;
using _4DMonoEngine.Core.Input;
using _4DMonoEngine.Core.UI;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Client
{
    public class MainGame : Game
    {

        private readonly TimeRuler m_timeRuler;

        public MainGame()
        {
            Content.RootDirectory = "Content"; // set content root directory.
            m_timeRuler = new TimeRuler(this)
            {
                Visible = true, 
                ShowLog = true
            };
        }

       protected override void Initialize()
        {
            IsMouseVisible = false;
            MainEngine.GetEngineInstance().Initialize(this, 0);
            Components.Add(m_timeRuler); 
            Components.Add(new InputManager(this));
            Components.Add(new UserInterface(this));
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
            GraphicsDevice.RasterizerState = MainEngine.GetEngineInstance().Rasterizer.State;
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
