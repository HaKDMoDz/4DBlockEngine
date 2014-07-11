using _4DMonoEngine.Core.Debugging.Console;

namespace _4DMonoEngine.Core.Commands
{
    [Command("debug-graphs", "Sets the debug graphs mode.\nusage: debug-graphs [on|off]")]
    public class GraphCommand:Command
    {
        [DefaultCommand]
        public string Default(string[] @params)
        {
            
#if DEBUG
            var str = string.Format("Debug-graphs are currently {0}.\nusage: debug-graphs [on|off].",
                MainEngine.GetEngineInstance().DebugOnlyDebugManager.GraphsEnabled ? "on" : "off");
#else
            var str = "Debug system offline. How are you reading this???";
#endif
            return str;

        }

        [Subcommand("on", "Sets debug-graphs on.")]
        public string On(string[] @params)
        {
#if DEBUG
            MainEngine.GetEngineInstance().DebugOnlyDebugManager.GraphsEnabled = true;
#endif
            return "Debug-graphs on.";
        }

        [Subcommand("off", "Sets debug-graphs off.")]
        public string Off(string[] @params)
        {
#if DEBUG
            MainEngine.GetEngineInstance().DebugOnlyDebugManager.GraphsEnabled = false;
#endif
            return "Debug-graphs off.";
        }
    }
}
