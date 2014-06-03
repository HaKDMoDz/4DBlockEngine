using _4DMonoEngine.Core.Debugging.Console;

namespace _4DMonoEngine.Core.Graphics
{
    [Command("vsync", "Sets the vsync mode.\nusage: vsync [on|off]")]
    public class VSyncCommand:Command
    {
        private IGraphicsManager _graphicsManager;

        public VSyncCommand()
        {
            _graphicsManager = (IGraphicsManager)Core.Core.Instance.Game.Services.GetService(typeof(IGraphicsManager));
        }

        [DefaultCommand]
        public string Default(string[] @params)
        {
            return string.Format("Vsync is currently {0}.\nusage: vsync [on|off].",
                                 _graphicsManager.VerticalSyncEnabled
                                     ? "on"
                                     : "off");
        }

        [Subcommand("on", "Sets vsync on.")]
        public string On(string[] @params)
        {
            _graphicsManager.EnableVerticalSync(true);
            return "VSync on.";
        }

        [Subcommand("off", "Sets vsync off.")]
        public string Off(string[] @params)
        {
            _graphicsManager.EnableVerticalSync(false);
            return "VSync off.";
        }
    }

    [Command("fullscreen", "Sets the fullscreen mode.\nusage: fullscreen [on|off]")]
    public class FullScreenCommand : Command
    {
        private IGraphicsManager _graphicsManager;

        public FullScreenCommand()
        {
            _graphicsManager = (IGraphicsManager)Core.Core.Instance.Game.Services.GetService(typeof(IGraphicsManager));
        }

        [DefaultCommand]
        public string Default(string[] @params)
        {
            return string.Format("Fullscreen is currently {0}.\nusage: fullscreen [on|off].",
                                 _graphicsManager.FullScreenEnabled
                                     ? "on"
                                     : "off");
        }

        [Subcommand("on", "Sets fullscreen on.")]
        public string On(string[] @params)
        {
            _graphicsManager.EnableFullScreen(true);
            return "Fullscreen on.";
        }

        [Subcommand("off", "Sets fullscreen off.")]
        public string Off(string[] @params)
        {
            _graphicsManager.EnableFullScreen(false);
            return "Fullscreen off.";
        }
    }
}
