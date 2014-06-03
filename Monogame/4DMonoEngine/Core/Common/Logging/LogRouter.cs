

using System;
using System.Linq;

namespace _4DMonoEngine.Core.Common.Logging
{
    internal static class LogRouter
    {

        public static void Route(Logger.Level level, string logger, string message, Exception exception)
        {
            if (LogManager.Enabled && LogManager.Targets.Count > 0)
            {
                foreach (var target in LogManager.Targets.Where(target => level >= target.MinimumLevel && level <= target.MaximumLevel))
                {
                    if (exception != null)
                    {
                        target.Log(level, logger, message, exception);
                    }
                    else
                    {
                        target.Log(level, logger, message);
                    }
                }
            }
        }
    }
}