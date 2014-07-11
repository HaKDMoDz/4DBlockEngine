using _4DMonoEngine.Core.Debugging.Console;

namespace _4DMonoEngine.Core.Commands
{
    [Command("rasterizer", "Sets rasterizer mode.\nusage: rasterizer [wireframed|normal]")]
    public class RasterizerCommand : Command
    {
        [DefaultCommand]
        public string Default(string[] @params)
        {
            return string.Format("Rasterizer is currently set to {0} mode.\nusage: rasterizer [wireframed|normal].",
            MainEngine.GetEngineInstance().Rasterizer.Wireframe
                    ? "wireframe"
                    : "normal");
        }
        
        [Subcommand("wireframed","Sets rasterizer mode to wireframed.")]
        public string Wireframed(string[] @params)
        {
            MainEngine.GetEngineInstance().Rasterizer.Wireframe = true;
            return "Rasterizer mode set to wireframed.";
        }

        [Subcommand("normal", "Sets rasterizer mode to normal.")]
        public string Normal(string[] @params)
        {
            MainEngine.GetEngineInstance().Rasterizer.Wireframe = false;
            return "Rasterizer mode set to normal mode.";
        }
    }
}