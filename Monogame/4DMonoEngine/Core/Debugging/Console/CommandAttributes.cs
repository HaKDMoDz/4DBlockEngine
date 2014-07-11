using System;

namespace _4DMonoEngine.Core.Debugging.Console
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        /// Command group's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Help text for command group.
        /// </summary>
        public string Help { get; private set; }

        public CommandAttribute(string name, string help)
        {
            Name = name.ToLower();
            Help = help;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SubcommandAttribute : Attribute
    {
        /// <summary>
        /// Command's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Help text for command.
        /// </summary>
        public string Help { get; private set; }

        public SubcommandAttribute(string command, string help)
        {
            Name = command.ToLower();
            Help = help;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DefaultCommand : SubcommandAttribute
    {
        public DefaultCommand()
            : base("", "")
        {}
    }
}
