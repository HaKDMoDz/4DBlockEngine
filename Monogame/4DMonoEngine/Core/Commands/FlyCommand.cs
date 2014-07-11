using _4DMonoEngine.Core.Debugging.Console;
using _4DMonoEngine.Core.Universe;

namespace _4DMonoEngine.Core.Commands
{
    [Command("fly", "Sets the flying mode.\nusage: fly [on|off]")]
    internal class FlyCommand : Command
    {
        private readonly Player m_player;

        public FlyCommand()
        {
            m_player = MainEngine.GetEngineInstance().Simulation.Player;
        }

        [DefaultCommand]
        public string Default(string[] @params)
        {
            return string.Format("Fly mode is currently {0}.\nusage: fly [on|off].",
                                 m_player.FlyingEnabled
                                     ? "on"
                                     : "off");
        }

        [Subcommand("on", "Sets flying mode on.")]
        public string On(string[] @params)
        {
            m_player.FlyingEnabled = true;
            return "Fly mode on.";
        }

        [Subcommand("off", "Sets flying off.")]
        public string Off(string[] @params)
        {
            m_player.FlyingEnabled = false;
            return "Fly mode off.";
        }
    }
}
