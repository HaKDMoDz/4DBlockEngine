

using _4DMonoEngine.Core.Common.Logging;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Graphics
{
    /// <summary>
    /// Screen service for controlling screen.
    /// </summary>
    public interface IGraphicsManager
    {
        /// <summary>
        /// Returns true if game is set to fixed time steps.
        /// </summary>
        bool FixedTimeStepsEnabled { get; }

        /// <summary>
        /// Returns true if vertical sync is enabled.
        /// </summary>
        bool VerticalSyncEnabled { get; }

        /// <summary>
        /// Returns true if full-screen is enabled.
        /// </summary>
        bool FullScreenEnabled { get; }

        /// <summary>
        /// Toggles fixed time steps.
        /// </summary>
        void ToggleFixedTimeSteps();

        /// <summary>
        /// Sets vertical sync on or off.
        /// </summary>
        /// <param name="enabled"></param>
        void EnableVerticalSync(bool enabled);

        /// <summary>
        /// Sets full screen on or off.
        /// </summary>
        /// <param name="enabled"></param>
        void EnableFullScreen(bool enabled);
    }

    /// <summary>
    /// The screen manager that controls various graphical aspects.
    /// </summary>
    public sealed class GraphicsManager : IGraphicsManager
    {
        // settings
        public bool FixedTimeStepsEnabled { get; private set; } // Returns true if game is set to fixed time steps.
        public bool VerticalSyncEnabled { get; private set; } // Returns true if vertical sync is enabled.
        public bool FullScreenEnabled { get; private set; } // Returns true if full-screen is enabled.

        // principal stuff
        private readonly Game _game; // the attached game.
        private readonly GraphicsDeviceManager _graphicsDeviceManager; // attached graphics device manager.

        // misc
        private static readonly Logger Logger = LogManager.GetOrCreateLogger(); // logging-facility.

        public GraphicsManager(GraphicsDeviceManager graphicsDeviceManager, Game game)
        {
            Logger.Trace("ctor()");

            _game = game;
            _graphicsDeviceManager = graphicsDeviceManager;
            _game.Services.AddService(typeof(IGraphicsManager), this); // export service.

            FullScreenEnabled = _graphicsDeviceManager.IsFullScreen = Core.Core.Instance.Configuration.Graphics.FullScreenEnabled;
            _graphicsDeviceManager.PreferredBackBufferWidth = Core.Core.Instance.Configuration.Graphics.Width;
            _graphicsDeviceManager.PreferredBackBufferHeight = Core.Core.Instance.Configuration.Graphics.Height;
            FixedTimeStepsEnabled = _game.IsFixedTimeStep = Core.Core.Instance.Configuration.Graphics.FixedTimeStepsEnabled;
            VerticalSyncEnabled = _graphicsDeviceManager.SynchronizeWithVerticalRetrace = Core.Core.Instance.Configuration.Graphics.VerticalSyncEnabled;
            _graphicsDeviceManager.ApplyChanges();
        }

        /// <summary>
        /// Toggles fixed time steps.
        /// </summary>
        public void ToggleFixedTimeSteps()
        {
            FixedTimeStepsEnabled = !FixedTimeStepsEnabled;
            _game.IsFixedTimeStep = FixedTimeStepsEnabled;
            _graphicsDeviceManager.ApplyChanges();
        }

        public void EnableVerticalSync(bool enabled)
        {
            VerticalSyncEnabled = enabled;
            _graphicsDeviceManager.SynchronizeWithVerticalRetrace = VerticalSyncEnabled;
            _graphicsDeviceManager.ApplyChanges();
        }

        /// <summary>
        /// Sets full screen on or off.
        /// </summary>
        /// <param name="enabled"></param>
        public void EnableFullScreen(bool enabled)
        {
            FullScreenEnabled = enabled;
            _graphicsDeviceManager.IsFullScreen = FullScreenEnabled;
            _graphicsDeviceManager.ApplyChanges();
        }
    }
}