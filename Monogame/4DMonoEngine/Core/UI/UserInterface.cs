using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.UI
{
    public class UserInterface : DrawableGameComponent
    {
        private readonly Dictionary<string, Texture2D> m_aimTextures;
        private Texture2D m_currentCrosshair;
        private SpriteBatch m_spriteBatch;

        public UserInterface(Game game)
            : base(game)
        {
            m_aimTextures = new Dictionary<string, Texture2D>();
        }

        public override void Initialize()
        {
            m_spriteBatch = new SpriteBatch(Game.GraphicsDevice);
            m_currentCrosshair = MainEngine.GetEngineInstance().GetAsset<Texture2D>("CrossHairNormal");
            m_aimTextures["CrossHairNormal"] = m_currentCrosshair;
        }

        public override void Draw(GameTime gameTime)
        {
            m_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            m_spriteBatch.Draw(m_currentCrosshair, new Vector2((Game.GraphicsDevice.Viewport.Width - m_currentCrosshair.Width) / 2.0f, (Game.GraphicsDevice.Viewport.Height - m_currentCrosshair.Height) / 2.0f), Color.White);
            m_spriteBatch.End();
        }
    }
}