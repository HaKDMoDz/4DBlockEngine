﻿using System.Linq;

namespace _4DMonoEngine.Core.Debugging.Console
{
    class CommandProcesser
    {
        public string Process(string buffer)
        {
            var output = CommandManager.Parse(buffer);
            return output;
        }

        static string GetCommandName(string buffer)
        {
            var firstSpace = buffer.IndexOf(' ');
            return buffer.Substring(0, firstSpace < 0 ? buffer.Length : firstSpace);
        }

        static string[] GetArguments(string buffer)
        {
            var firstSpace = buffer.IndexOf(' ');
            if (firstSpace < 0)
            {
                return new string[0];
            }
            
            var args = buffer.Substring(firstSpace, buffer.Length - firstSpace).Split(' ');
            return args.Where(a => a.Length != 0).ToArray();
        }
    }
}
