using System;

namespace _4DMonoEngine.Core.Logging
{
    public class ConsoleTarget : LogTarget
    {
        public ConsoleTarget(Logger.Level minLevel, Logger.Level maxLevel, bool includeTimeStamps)
        {
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;
            IncludeTimeStamps = includeTimeStamps;
        }

        public override void Log(Logger.Level level, string logger, string message)
        {
            var timeStamp = IncludeTimeStamps ? "[" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff") + "] " : "";
            SetConsoleForegroundColor(level);
            Console.WriteLine(string.Format("{0}[{1}] [{2}]: {3}", timeStamp, level.ToString().PadLeft(5), logger, message));
        }

       public override void Log(Logger.Level level, string logger, string message, Exception exception)
        {
            var timeStamp = IncludeTimeStamps ? "[" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff") + "] " : "";
            SetConsoleForegroundColor(level);
            Console.WriteLine(string.Format("{0}[{1}] [{2}]: {3} - [Exception] {4}", timeStamp, level.ToString().PadLeft(5), logger, message, exception));
        }

        private static void SetConsoleForegroundColor(Logger.Level level)
        {
            switch (level)
            {
                case Logger.Level.Trace:
                case Logger.Level.PacketDump:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                case Logger.Level.Debug:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case Logger.Level.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case Logger.Level.Warn:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case Logger.Level.Error:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case Logger.Level.Fatal:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    break;
            }
        }
    }
}