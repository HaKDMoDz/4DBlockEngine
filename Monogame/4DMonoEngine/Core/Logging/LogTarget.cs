using System;

namespace _4DMonoEngine.Core.Logging
{
    public abstract class LogTarget
    {
        public Logger.Level MinimumLevel { get; protected set; }
        public Logger.Level MaximumLevel { get; protected set; }
        public bool IncludeTimeStamps { get; protected set; }

        public abstract void Log(Logger.Level level, string logger, string message);
        public abstract void Log(Logger.Level level, string logger, string message, Exception exception);
    }
}