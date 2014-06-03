using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace _4DMonoEngine.Core.Common.Logging
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
            var frame = new StackFrame(1, false); // read stack frame.
            var declaringType = frame.GetMethod().DeclaringType;
            if (declaringType != null)
            {
                if (name == null)
                {
                    name = declaringType.Name; // get declaring type's name.
                }
                Logger logger;
                if (!(m_loggers.ContainsKey(name) && m_loggers[name].TryGetTarget(out logger)))  // see if we already have instance for the given name.
                {
                    logger = new Logger(name);
                    m_loggers.Add(name, new WeakReference<Logger>(logger)); // add it to dictionary of loggers.
                }
                return logger; // return the newly created logger.
            }
        }

        public void AttachLogTarget(LogTarget target)
        {
            m_targets.Add(target);
        }
    }
}