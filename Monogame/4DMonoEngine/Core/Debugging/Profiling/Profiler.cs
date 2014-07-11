﻿

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace _4DMonoEngine.Core.Debugging.Profiling
{
    /// <summary>
    /// Provides methods for profiling.
    /// </summary>
    public static class Profiler
    {
        private static readonly Dictionary<string, Stopwatch> Timers;

        static Profiler()
        {
            Timers = new Dictionary<string, Stopwatch>();
        }

        public static void Start(string key)
        {
            if (!Timers.ContainsKey(key)) Timers.Add(key, new Stopwatch());
            else Timers[key].Restart();
            Timers[key].Start();
        }

        public static TimeSpan Stop(string key)
        {
            if (!Timers.ContainsKey(key)) return TimeSpan.Zero;
            Timers[key].Stop();
            return Timers[key].Elapsed;
        }
    }
}