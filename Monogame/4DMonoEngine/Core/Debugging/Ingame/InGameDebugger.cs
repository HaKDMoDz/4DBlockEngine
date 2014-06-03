using System;
using _4DMonoEngine.Core.Assets;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Common.Logging;
using _4DMonoEngine.Core.Graphics;
using _4DMonoEngine.Core.Universe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Debugging.Ingame
{
    public interface IInGameDebuggerService
    {
        void ToggleInGameDebugger();
    }
    
    

    public sealed class InGameDebugger : DrawableGameComponent, IInGameDebuggerService
    {
        private SpriteBatch _spriteBatch;
        private SpriteFont _spriteFont;
        private bool _active = false;

        // required services.
        private ICamera _camera;
        private IWorld _world;
        private IPlayer _player;
        private IChunkStorage _chunkStorage;
        private IAssetManager _assetManager;

        /// <summary>
        /// Logging facility.
        /// </summary>
        private static readonly Logger Logger = LogManager.GetOrCreateLogger();

        public InGameDebugger(Game game)
            : base(game)
        {
            game.Services.AddService(typeof (IInGameDebuggerService), this); // export service.
        }

        public override void Initialize()
        {
            Logger.Trace("init()");

            // import required service.
            _camera = (ICamera) Game.Services.GetService(typeof (ICamera));
            _world = (IWorld) Game.Services.GetService(typeof (IWorld));
            _player = (IPlayer) Game.Services.GetService(typeof (IPlayer));
            _chunkStorage = (IChunkStorage) Game.Services.GetService(typeof (IChunkStorage));
            _assetManager = (IAssetManager)Game.Services.GetService(typeof(IAssetManager));
            
            if (_assetManager == null)
                throw new NullReferenceException("Can not find asset manager component.");

            _spriteFont = _assetManager.Verdana;
            _spriteBatch = new SpriteBatch(Game.GraphicsDevice);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!_active) return;
            var viewFrustrum = new BoundingFrustum(_camera.View*_camera.Projection);

            _spriteBatch.Begin();
            
            //foreach (Chunk chunk in this._chunkStorage.Values)
            //{
            //    if (chunk != this._player.CurrentChunk)
            //        continue;

            //    if (!chunk.BoundingBox.Intersects(viewFrustrum)) 
            //        continue;

            //    chunk.DrawInGameDebugVisual(Game.GraphicsDevice, _camera, _spriteBatch, _spriteFont);
            //}

            _player.Weapon.DrawInGameDebugVisual(Game.GraphicsDevice, _camera, _spriteBatch, _spriteFont);

            _spriteBatch.End();
        }

        public void ToggleInGameDebugger()
        {
            _active = !_active;
        }
    }
}