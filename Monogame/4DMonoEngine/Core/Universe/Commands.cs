using _4DMonoEngine.Core.Debugging.Console;

namespace _4DMonoEngine.Core.Universe
{
    [Command("fly", "Sets the flying mode.\nusage: fly [on|off]")]
    public class FlyCommand : Command
    {
        private readonly IPlayer _player;

        public FlyCommand()
        {
            _player = (IPlayer)Core.Core.Instance.Game.Services.GetService(typeof(IPlayer));
        }

        [DefaultCommand]
        public string Default(string[] @params)
        {
            return string.Format("Fly mode is currently {0}.\nusage: fly [on|off].",
                                 _player.FlyingEnabled
                                     ? "on"
                                     : "off");
        }

        [Subcommand("on", "Sets flying mode on.")]
        public string On(string[] @params)
        {
            _player.FlyingEnabled = true;
            return "Fly mode on.";
        }

        [Subcommand("off", "Sets flying off.")]
        public string Off(string[] @params)
        {
            _player.FlyingEnabled = false;
            return "Fly mode off.";
        }
    }
}
