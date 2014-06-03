using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _4DMonoEngine.Core.Common.Logging;

namespace _4DMonoEngine.Core.Debugging.Console
{
    public static class CommandManager
    {
        public static readonly Logger Logger;
        private static readonly Dictionary<CommandAttribute, Command> CommandGroups = new Dictionary<CommandAttribute, Command>();

        static CommandManager()
        {
            Logger = MainEngine.GetEngineInstance().GetLogger();
            RegisterCommandGroups();
        }

        private static void RegisterCommandGroups()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!type.IsSubclassOf(typeof(Command))) continue;

                var attributes = (CommandAttribute[])type.GetCustomAttributes(typeof(CommandAttribute), true);
                if (attributes.Length == 0) continue;

                var groupAttribute = attributes[0];
                if (CommandGroups.ContainsKey(groupAttribute))
                    Logger.Warn("There exists an already registered command group named '{0}'.", groupAttribute.Name);

                var commandGroup = (Command)Activator.CreateInstance(type);
                commandGroup.Register(groupAttribute);
                CommandGroups.Add(groupAttribute, commandGroup);
            }
        }

        /// <summary>
        /// Parses a given line from console as a command if any.
        /// </summary>
        /// <param name="line">The line to be parsed.</param>
        public static string Parse(string line)
        {
            var output = string.Empty;
            string command;
            string parameters;
            var found = false;

            if (line == null) 
                return output;

            if (line.Trim() == string.Empty) 
                return output;

            if (!ExtractCommandAndParameters(line, out command, out parameters))
            {
                output = "Unknown command: " + line;
                Logger.Info(output);
                return output;
            }

            foreach (var pair in CommandGroups)
            {
                if (pair.Key.Name != command) continue;
                output = pair.Value.Handle(parameters);
                found = true;
                break;
            }

            if (found == false)
                output = "ERROR: command not found.";

            if (output != string.Empty)
                Logger.Info(output);

            return output;
        }

        public static Command GetMatchingCommand(string command)
        {
            var matchingCommands = CommandGroups.Values.Where(c => c.Attributes.Name.StartsWith(command));
            return matchingCommands.FirstOrDefault();
        }

        public static bool ExtractCommandAndParameters(string line, out string command, out string parameters)
        {
            line = line.Trim();
            command = string.Empty;
            parameters = string.Empty;

            if (line == string.Empty)
                return false;

            command = line.Split(' ')[0].ToLower(); // get command
            parameters = String.Empty;
            if (line.Contains(' ')) parameters = line.Substring(line.IndexOf(' ') + 1).Trim(); // get parameters if any.

            return true;
        }

        [Command("commands", "Lists available commands for your user-level.")]
        public class CommandsList : Command
        {
            public override string Fallback(string[] parameters = null)
            {
                var output = CommandGroups.Aggregate("Available commands: ", (current, pair) => current + (pair.Key.Name + ", "));

                output = output.Substring(0, output.Length - 2) + ".";
                return output + "\nType 'help <command>' to get help.";
            }
        }

        [Command("help", "Oh no, we forgot to add a help to text to help command itself!")]
        public class HelpCommand : Command
        {
            public override string Fallback(string[] parameters = null)
            {
                return "usage: help <command>";
            }

            public override string Handle(string parameters)
            {
                if (parameters == string.Empty)
                    return Fallback();

                var output = string.Empty;
                var found = false;
                var @params = parameters.Split(' ');
                var group = @params[0];
                var command = @params.Count() > 1 ? @params[1] : string.Empty;

                foreach (var pair in CommandGroups)
                {
                    if (group != pair.Key.Name)
                        continue;

                    if (command == string.Empty)
                        return pair.Key.Help;

                    output = pair.Value.GetHelp(command);
                    found = true;
                }

                if (!found)
                    output = string.Format("Unknown command: {0} {1}", group, command);

                return output;
            }
        }

        [Command("clear", "Clears the console output.")]
        public class ClearCommand : Command
        {
            [DefaultCommand]
            public string Default(string[] @params)
            {
                return "not_implemented!";
                // processor.Out.Clear();
            }
        }

        [Command("exit", "Forcefully exists the game.")]
        public class ExitCommand : Command
        {
            [DefaultCommand]
            public string Default(string[] @params)
            {
                MainEngine.GetEngineInstance().Exit();
                return "Exiting the game..";
            }
        }
    }
}
