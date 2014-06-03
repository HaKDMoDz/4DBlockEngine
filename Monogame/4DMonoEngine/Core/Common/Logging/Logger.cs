using System;
using System.Globalization;

namespace _4DMonoEngine.Core.Common.Logging
{
    public class Logger
    {
        public string Name { get; protected set; }
        public Logger(string name)
        {
            Name = name;
        }

        public enum Level
        {
            Trace,
            Debug,
            Info,
            Warn,
            Error,
            Fatal,
            PacketDump,
        }

        public void Trace(string message) 
        { 
            Log(Level.Trace, message, null);
        }
        public void Trace(string message, params object[] args) 
        { 
            Log(Level.Trace, message, args); 
        }
        public void Debug(string message) 
        { 
            Log(Level.Debug, message, null); 
        }

        public void Debug(string message, params object[] args) 
        { 
            Log(Level.Debug, message, args); 
        }

        public void Info(string message) 
        { 
            Log(Level.Info, message, null); 
        }

        public void Info(string message, params object[] args)
        { 
            Log(Level.Info, message, args);
        }

        public void Warn(string message)
        { 
            Log(Level.Warn, message, null);
        }

        public void Warn(string message, params object[] args) 
        { 
            Log(Level.Warn, message, args); 
        }

        public void Error(string message) 
        { 
            Log(Level.Error, message, null); 
        }

        public void Error(string message, params object[] args) 
        { 
            Log(Level.Error, message, args);
        }

        public void Fatal(string message) 
        { 
            Log(Level.Fatal, message, null); 
        }

        public void Fatal(string message, params object[] args) 
        {
            Log(Level.Fatal, message, args);
        }        
        
        public void Trace(Exception exception, string message) 
        { 
            Log(Level.Trace, message, null, exception); 
        }

        public void Trace(Exception exception, string message, params object[] args) 
        { 
            Log(Level.Trace, message, args, exception); 
        }

        public void Debug(Exception exception, string message)
        { 
            Log(Level.Debug, message, null, exception);
        }

        public void Debug(Exception exception, string message, params object[] args) 
        { 
            Log(Level.Debug, message, args, exception); 
        }

        public void Info(Exception exception, string message) 
        { 
            Log(Level.Info, message, null, exception);
        }

        public void Info(Exception exception, string message, params object[] args) 
        { 
            Log(Level.Info, message, args, exception); 
        }

        public void Warn(Exception exception, string message)
        {
            Log(Level.Warn, message, null, exception); 
        }
        
        public void Warn(Exception exception, string message, params object[] args) 
        {
            Log(Level.Warn, message, args, exception);
        }
        
        public void Error(Exception exception, string message) 
        { 
            Log(Level.Error, message, null, exception);
        }
        
        public void Error(Exception exception, string message, params object[] args)
        {
            Log(Level.Error, message, args, exception); 
        }

        public void Fatal(Exception exception, string message) 
        { 
            Log(Level.Fatal, message, null, exception); 
        }
        
        public void Fatal(Exception exception, string message, params object[] args) 
        { 
            Log(Level.Fatal, message, args, exception); 
        }

        private void Log(Level level, string message, object[] args, Exception exception = null) // sends logs to log-router.
        {
            LogRouter.Route(level, Name, args == null ? message : string.Format(CultureInfo.InvariantCulture, message, args), exception);
        }
    }
}