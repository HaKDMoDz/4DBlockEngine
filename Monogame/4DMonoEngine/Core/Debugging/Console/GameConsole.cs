using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Debugging.Console
{
    public class GameConsole : DrawableGameComponent
    {
        private readonly GameConsoleOptions m_options;
        public bool Opened
        {
            get
            {
                return m_renderer.IsOpen;
            }
        }
        private readonly SpriteBatch m_spriteBatch;
        private readonly InputProcessor m_inputProcesser;
        private readonly Renderer m_renderer;
        
        public GameConsole(Game game, SpriteBatch spriteBatch, GameConsoleOptions options, Action toggleInGameDebugger) 
            : base(game)
        {
            m_options = options;
            Enabled = true;
            m_spriteBatch = spriteBatch;
            m_inputProcesser = new InputProcessor(new CommandProcesser(), m_options, toggleInGameDebugger);
            m_inputProcesser.Open += (s, e) => m_renderer.Open();
            m_inputProcesser.Close += (s, e) => m_renderer.Close();
            m_renderer = new Renderer(game, spriteBatch, m_inputProcesser, m_options);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Enabled)
            {
                return;
            }
            m_spriteBatch.Begin();
            m_renderer.Draw(gameTime);
            m_spriteBatch.End();
            base.Draw(gameTime);
        }

        public override void Update(GameTime gameTime)
        {
            if (!Enabled)
            {
                return;
            }
            m_renderer.Update(gameTime);
            base.Update(gameTime);
        }

        public void WriteLine(string text)
        {
            m_inputProcesser.AddToOutput(text);
        }
    }
}
