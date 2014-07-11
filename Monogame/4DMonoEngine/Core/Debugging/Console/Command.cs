using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace _4DMonoEngine.Core.Debugging.Console
{
    public class Command
    {
        public CommandAttribute Attributes { get; private set; }

        private readonly Dictionary<SubcommandAttribute, MethodInfo> m_commands = new Dictionary<SubcommandAttribute, MethodInfo>();

        public void Register(CommandAttribute attributes)
        {
            Attributes = attributes;
            RegisterDefaultCommand();
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            foreach (var method in GetType().GetMethods())
            {
                var attributes = method.GetCustomAttributes(typeof(SubcommandAttribute), true);
                if (attributes.Length == 0) continue;

                var attribute = (SubcommandAttribute)attributes[0];
                if (attribute is DefaultCommand) continue;

                if (!m_commands.ContainsKey(attribute))
                    m_commands.Add(attribute, method);
                else
                    CommandManager.Logger.Warn("There exists an already registered command '{0}'.", attribute.Name);
            }
        }

        private void RegisterDefaultCommand()
        {
            foreach (var method in GetType().GetMethods())
            {
                var attributes = method.GetCustomAttributes(typeof(DefaultCommand), true);
                if (attributes.Length == 0) continue;
                if (method.Name.ToLower() == "fallback") continue;

                m_commands.Add(new DefaultCommand(), method);
                return;
            }

            // set the fallback command if we couldn't find a defined DefaultCommand.
            m_commands.Add(new DefaultCommand(), GetType().GetMethod("Fallback"));
        }

        public virtual string Handle(string parameters)
        {
            string[] @params = null;
            SubcommandAttribute target = null;

            if (parameters == string.Empty)
                target = GetDefaultSubcommand();
            else
            {
                @params = parameters.Split(' ');
                target = GetSubcommand(@params[0]) ?? GetDefaultSubcommand();

                if (target != GetDefaultSubcommand())
                    @params = @params.Skip(1).ToArray();
            }


            return (string)m_commands[target].Invoke(this, new object[] { @params });
        }

        public string GetHelp(string command)
        {
            foreach (var pair in m_commands)
            {
                if (command == pair.Key.Name)
                return pair.Key.Help;
            }

            return string.Empty;
        }

        [DefaultCommand]
        public virtual string Fallback(string[] @params = null)
        {
            var output = m_commands.Where(pair => pair.Key.Name.Trim() != string.Empty).Aggregate("Available subcommands: ", (current, pair) => current + (pair.Key.Name + ", "));

            return output.Substring(0, output.Length - 2) + ".";
        }

        private SubcommandAttribute GetDefaultSubcommand()
        {
            return m_commands.Keys.First();
        }

        private SubcommandAttribute GetSubcommand(string name)
        {
            return m_commands.Keys.FirstOrDefault(command => command.Name == name);
        }
    }
}
