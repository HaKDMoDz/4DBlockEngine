

using _4DMonoEngine.Core.Debugging.Console;

namespace _4DMonoEngine.Core.Debugging
{
    [Command("debug-graphs", "Sets the debug graphs mode.\nusage: debug-graphs [on|off]")]
    public class VSyncCommand:Command
    {
        [DefaultCommand]
        public string Default(string[] @params)
        {
            return string.Format("Debug-graphs are currently {0}.\nusage: debug-graphs [on|off].",
                                 Core.Core.Instance.Configuration.Debugging.GraphsEnabled
                                     ? "on"
                                     : "off");
        }

        [Subcommand("on", "Sets debug-graphs on.")]
        public string On(string[] @params)
        {
            Core.Core.Instance.Configuration.Debugging.GraphsEnabled = true;
            return "Debug-graphs on.";
        }

        [Subcommand("off", "Sets debug-graphs off.")]
        public string Off(string[] @params)
        {
            Core.Core.Instance.Configuration.Debugging.GraphsEnabled = false;
            return "Debug-graphs off.";
        }
    }

    /*[Command("fog", "Sets fog mode.\nusage: fog [off|near|far]")]
    public class FoggerCommand : Command
    {
        private readonly Fogger m_fogger;

        public FoggerCommand()
        {
            m_fogger = (Fogger)Core.Core.Instance.Game.Services.GetService(typeof(Fogger));
        }

        [DefaultCommand]
        public string Default(string[] @params)
        {
            return string.Format("Fog is currently set to {0} mode.\nusage: fog [off|near|far]",
                                 m_fogger.State.ToString().ToLower());
        }

        [Subcommand("off", "Sets fog to off.")]
        public string Off(string[] @params)
        {
            m_fogger.State = FogState.None;
            return "Fog is off.";
        }

        [Subcommand("near", "Sets fog to near.")]
        public string Near(string[] @params)
        {
            m_fogger.State = FogState.Near;
            return "Fog is near.";
        }

        [Subcommand("far", "Sets fog to far.")]
        public string Far(string[] @params)
        {
            m_fogger.State = FogState.Far;
            return "Fog is far.";
        }
    }*/
}
