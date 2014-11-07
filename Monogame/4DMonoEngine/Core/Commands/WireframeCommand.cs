using _4DMonoEngine.Core.Debugging.Console;

namespace _4DMonoEngine.Core.Commands
{
    [Command("wireframe", "Sets rasterizer mode.\nusage: wireframe [on|off]")]
    public class WireframeCommand : Command
    {
        [DefaultCommand]
        public string Default(string[] @params)
        {
            return string.Format("Rasterizer is currently set to {0} mode.\nusage: wireframe [on|off].",
            MainEngine.GetEngineInstance().Rasterizer.Wireframe
                    ? "on"
                    : "off");
        }
        
        [Subcommand("on","Sets rasterizer mode to wireframed.")]
        public string Wireframed(string[] @params)
        {
            MainEngine.GetEngineInstance().Rasterizer.Wireframe = true;
            return "Rasterizer mode set to wireframed.";
        }

        [Subcommand("off", "Sets rasterizer mode to normal.")]
        public string Normal(string[] @params)
        {
            MainEngine.GetEngineInstance().Rasterizer.Wireframe = false;
            return "Rasterizer mode set to normal mode.";
        }
    }
}