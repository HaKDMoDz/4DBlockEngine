using System;
using System.IO;

namespace _4DMonoEngine.Core.Logging
{
   public class FileTarget : LogTarget, IDisposable
    {
        private const string LogRoot = "logs";

        private FileStream m_fileStream; // filestream pointing to logfile.
        private StreamWriter m_logStream; // stream-writer for flushing logs to disk.

        public FileTarget(string fileName, Logger.Level minLevel, Logger.Level maxLevel, bool includeTimeStamps, bool reset = false)
        {
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;
            IncludeTimeStamps = includeTimeStamps;
            string filePath = string.Format("{0}/{1}", LogRoot, fileName); // construct the full path using LoggingRoot defined in config.ini

            if (!Directory.Exists(LogRoot)) // create logging directory if it does not exist yet.
                Directory.CreateDirectory(LogRoot);

            m_fileStream = new FileStream(filePath, reset ? FileMode.Create : FileMode.Append, FileAccess.Write, FileShare.Read); // init the file stream.
            m_logStream = new StreamWriter(m_fileStream) { AutoFlush = true }; // init the stream writer.
        }

        public override void Log(Logger.Level level, string logger, string message)
        {
            lock (this) 
            {
                var timeStamp = IncludeTimeStamps ? "[" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff") + "] " : "";
                if (!_disposed) // make sure we're not disposed.
                {
                    m_logStream.WriteLine("{0}[{1}] [{2}]: {3}", timeStamp, level.ToString().PadLeft(5), logger, message);
                }
            }
        }

       public override void Log(Logger.Level level, string logger, string message, Exception exception)
        {
            lock (this) 
            {
                var timeStamp = IncludeTimeStamps ? "[" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff") + "] " : "";
                if (!_disposed) // make sure we're not disposed.
                {
                    m_logStream.WriteLine("{0}[{1}] [{2}]: {3} - [Exception] {4}", timeStamp,
                        level.ToString().PadLeft(5), logger, message, exception);
                }
            }
        }

        #region de-ctor

        // IDisposable pattern: http://msdn.microsoft.com/en-us/library/fs2xkftw%28VS.80%29.aspx

        private bool _disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Take object out the finalization queue to prevent finalization code for it from executing a second time.
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return; // if already disposed, just return

            if (disposing) // only dispose managed resources if we're called from directly or in-directly from user code.
            {
                m_logStream.Close();
                m_logStream.Dispose();
                m_fileStream.Close();
                m_fileStream.Dispose();
            }

            m_logStream = null;
            m_fileStream = null;

            _disposed = true;
        }

        ~FileTarget() { Dispose(false); } // finalizer called by the runtime. we should only dispose unmanaged objects and should NOT reference managed ones.

        #endregion
    }
}