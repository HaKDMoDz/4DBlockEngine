using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Assets;
using _4DMonoEngine.Core.Interfaces;
using _4DMonoEngine.Core.Config;
using _4DMonoEngine.Core.Events;
using _4DMonoEngine.Core.Graphics;
using _4DMonoEngine.Core.Logging;
using _4DMonoEngine.Core.Managers;
using _4DMonoEngine.Core.Universe;

namespace _4DMonoEngine.Core
{
    public sealed class MainEngine
    {
        private static MainEngine s_engineInstance;
        private readonly LogManager m_logManager;
#if DEBUG
        public DebugManager DebugOnlyDebugManager { get; private set; }
#endif
        private AssetManager m_assetProvider;

        public static MainEngine GetEngineInstance()
        {
            return s_engineInstance ?? (s_engineInstance = new MainEngine());
        }

        private MainEngine()
        {
            m_logManager = new LogManager(new List<LogTarget>
            {
                new ConsoleTarget(Logger.Level.Info, Logger.Level.Fatal, false),
                new FileTarget("EngineLog", Logger.Level.Trace, Logger.Level.PacketDump, true)
            });
            Rasterizer = new Rasterizer();
            CentralDispatch = new EventDispatcher();
        }

        public Game Game { get; private set; }
        public Simulation Simulation { get; private set; }
        public Rasterizer Rasterizer { get; private set; }
        public Camera Camera { get; private set; }
        public EventDispatcher CentralDispatch { get; private set; }

        public Task<General> GeneralSettings { get; private set; }

        public void Initialize(Game game, uint seed)
        {
            Game = game;
            m_assetProvider = new AssetManager(game.Content, game.GraphicsDevice);
            GeneralSettings = GetConfig<General>("GeneralSettings");
            Simulation = new Simulation(game, seed);
            game.Components.Add(Simulation);
            Camera = new Camera(game.GraphicsDevice.Viewport.AspectRatio);
#if DEBUG
            DebugOnlyDebugManager = new DebugManager(game, Camera, Simulation.ChunkCache, GetAsset<SpriteFont>("Verdana"));
            game.Components.Add(DebugOnlyDebugManager);
#endif
            var player = Simulation.Player;
            CentralDispatch.Register(EventConstants.KeyDown, player.GetHandlerForEvent(EventConstants.KeyDown));
            CentralDispatch.Register(EventConstants.KeyUp, player.GetHandlerForEvent(EventConstants.KeyUp));
            CentralDispatch.Register(EventConstants.LeftMouseDown, player.GetHandlerForEvent(EventConstants.LeftMouseDown));
            CentralDispatch.Register(EventConstants.LeftMouseUp, player.GetHandlerForEvent(EventConstants.LeftMouseUp));
            CentralDispatch.Register(EventConstants.RightMouseDown, player.GetHandlerForEvent(EventConstants.RightMouseDown));
            CentralDispatch.Register(EventConstants.MousePositionUpdated, player.GetHandlerForEvent(EventConstants.MousePositionUpdated));

            CentralDispatch.Register(EventConstants.PlayerPositionUpdated, Camera.GetHandlerForEvent(EventConstants.PlayerPositionUpdated));
            CentralDispatch.Register(EventConstants.ViewUpdated, Camera.GetHandlerForEvent(EventConstants.ViewUpdated));
            CentralDispatch.Register(EventConstants.ScreenSizeUpdated, Camera.GetHandlerForEvent(EventConstants.ScreenSizeUpdated));
            CentralDispatch.Register(EventConstants.ModalScreenPushed, Camera.GetHandlerForEvent(EventConstants.ModalScreenPushed));
            CentralDispatch.Register(EventConstants.ModalScreenPopped, Camera.GetHandlerForEvent(EventConstants.ModalScreenPopped));
        }

        public Logger GetLogger(string name = null)
        {
            return m_logManager.GetOrCreateLogger(name);
        }

        public T GetAsset<T>(string assetId)
        {
            return m_assetProvider.GetAsset<T>(assetId);
        }

        public async Task<Dictionary<string, T>> GetTable<T>(string tableName) where T : IDataContainer
        {
            return await m_assetProvider.GetTable<T>(tableName);
        }

        public async Task<T> GetConfig<T>(string recordId) where T : IDataContainer
        {
            return await GetConfig<T>(recordId, recordId);
        }

        public async Task<T> GetConfig<T>(string fileName, string recordId) where T : IDataContainer
        {
           return await m_assetProvider.GetConfig<T>(fileName, recordId);
        }

        public void Exit()
        {
            Game.Exit();
        }
    }
}
