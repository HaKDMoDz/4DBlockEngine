using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Assets;
using _4DMonoEngine.Core.Assets.DataObjects;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Events;
using _4DMonoEngine.Core.Graphics;
using _4DMonoEngine.Core.Initialization;
using _4DMonoEngine.Core.Input;
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
                new FileTarget("EngineLog.txt", Logger.Level.Trace, Logger.Level.PacketDump, true)
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
        private InitializationController m_initializationController;
        private InputManager m_inputManager;

        public async void Initialize(Game game, InputManager inputManager, uint seed)
        {
            Game = game;
            m_inputManager = inputManager;

            Camera = new Camera(game.GraphicsDevice.Viewport.AspectRatio);
            CentralDispatch.Register(EventConstants.PlayerPositionUpdated,
                Camera.GetHandlerForEvent(EventConstants.PlayerPositionUpdated));
            CentralDispatch.Register(EventConstants.ViewUpdated,
                Camera.GetHandlerForEvent(EventConstants.ViewUpdated));
            CentralDispatch.Register(EventConstants.ScreenSizeUpdated,
                Camera.GetHandlerForEvent(EventConstants.ScreenSizeUpdated));
            // CentralDispatch.Register(EventConstants.ModalScreenPushed, Camera.GetHandlerForEvent(EventConstants.ModalScreenPushed));
            // CentralDispatch.Register(EventConstants.ModalScreenPopped, Camera.GetHandlerForEvent(EventConstants.ModalScreenPopped));

            m_assetProvider = new AssetManager(game.Content, game.GraphicsDevice);
            GeneralSettings = GetConfig<General>("GeneralSettings");
            m_initializationController = new InitializationController();
            m_initializationController.AddEntry(new BlockInitializer());
            m_initializationController.AddEntry(new SaveSystemInitializer());
            var simulationInitializer = new SimulationInitializer(game, seed);
            m_initializationController.AddEntry(simulationInitializer);

            if (await m_initializationController.Run())
            {
                Simulation = simulationInitializer.GetSimulation();
                game.Components.Add(Simulation);
                var player = Simulation.Player;
                CentralDispatch.Register(EventConstants.KeyDown, player.GetHandlerForEvent(EventConstants.KeyDown));
                CentralDispatch.Register(EventConstants.KeyUp, player.GetHandlerForEvent(EventConstants.KeyUp));
                CentralDispatch.Register(EventConstants.LeftMouseDown,
                    player.GetHandlerForEvent(EventConstants.LeftMouseDown));
                CentralDispatch.Register(EventConstants.LeftMouseUp,
                    player.GetHandlerForEvent(EventConstants.LeftMouseUp));
                CentralDispatch.Register(EventConstants.RightMouseDown,
                    player.GetHandlerForEvent(EventConstants.RightMouseDown));
                CentralDispatch.Register(EventConstants.MousePositionUpdated,
                    player.GetHandlerForEvent(EventConstants.MousePositionUpdated));
#if DEBUG
                DebugOnlyDebugManager = new DebugManager(game, Camera, Simulation.ChunkCache,
                    GetAsset<SpriteFont>("Verdana"));
                game.Components.Add(DebugOnlyDebugManager);
#endif
                Simulation.Start();
            }

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

        public void ToggleMouseMode()
        {
            Game.IsMouseVisible = !Game.IsMouseVisible;
            m_inputManager.CursorCentered = !Game.IsMouseVisible;
        }
    }
}
