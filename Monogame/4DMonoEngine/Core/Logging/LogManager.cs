using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace _4DMonoEngine.Core.Logging
{
    public class LogManager
    {
        public bool Enabled { get; set; }
        private readonly List<LogTarget> m_targets = new List<LogTarget>();
        private readonly Dictionary<string, WeakReference<Logger>> m_loggers;

        public LogManager(List<LogTarget> logTargets)
        {
            m_targets = logTargets;
            m_loggers = new Dictionary<string, WeakReference<Logger>>();
        }

        public Logger GetOrCreateLogger(string name = null)
        {
            if (name == null)
            {
                var frame = new StackFrame(1, false); // read stack frame.
                var declaringType = frame.GetMethod().DeclaringType;
                name = declaringType != null ? declaringType.Name : "general"; // get declaring type's name.
            }
            Logger logger;
            if (!(m_loggers.ContainsKey(name) && m_loggers[name].TryGetTarget(out logger)))  // see if we already have instance for the given name.
            {
                logger = new Logger(name, Route);
                m_loggers.Add(name, new WeakReference<Logger>(logger)); // add it to dictionary of loggers.
            }
            return logger; // return the newly created logger.
        }

        private void Route(Logger.Level level, string logger, string message, Exception exception)
        {
            if (Enabled && m_targets.Count > 0)
            {
                foreach (var target in m_targets.Where(target => level >= target.MinimumLevel && level <= target.MaximumLevel))
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