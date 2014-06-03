

using System;
using _4DMonoEngine.Core.Assets;
using _4DMonoEngine.Core.Common.Logging;
using _4DMonoEngine.Core.Universe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Interface
{
    public class UserInterface : DrawableGameComponent
    {
        private Texture2D _crosshairNormalTexture;
        private Texture2D _crosshairShovelTexture;
        private SpriteBatch _spriteBatch;
        
        private IPlayer _player;
        private IAssetManager _assetManager;

        /// <summary>
        /// Logging facility.
        /// </summary>
        private static readonly Logger Logger = LogManager.GetOrCreateLogger();

        public UserInterface(Game game)
            : base(game)
        { }

        public override void Initialize()
        {
            Logger.Trace("init()");

            // import required services.
            _player = (IPlayer) Game.Services.GetService(typeof (IPlayer));
            if (_player == null)
                throw new NullReferenceException("Can not find player component.");

            _assetManager = (IAssetManager)Game.Services.GetService(typeof(IAssetManager));
            if (_assetManager == null)
                throw new NullReferenceException("Can not find asset manager component.");

            _spriteBatch = new SpriteBatch(Game.GraphicsDevice);
            LoadContent();
        }

        protected override void LoadContent()
        {
            _crosshairNormalTexture = _assetManager.CrossHairNormalTexture;
            _crosshairShovelTexture = _assetManager.CrossHairShovelTexture;
        }

        public override void Draw(GameTime gameTime)
        {
            // draw cross-hair.            
            var crosshairTexture = _player.AimedSolidBlock.HasValue
                                       ? _crosshairShovelTexture
                                       : _crosshairNormalTexture;

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            _spriteBatch.Draw(crosshairTexture,
                              new Vector2((Game.GraphicsDevice.Viewport.Width/2) - 10,
                                          (Game.GraphicsDevice.Viewport.Height/2) - 10), Color.White);
            _spriteBatch.End();
        }
    }
}